using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Assumptions in your project:
// - Team.Value is a Faction enum
// - Minis have: Agent (Faction), UnitHealth
// - Buildings have: Team (Faction), AttackGroup, UnitHealth (via proxy)
// - Movers pause when entity has Attacking tag
// - Shooter has fields: float Range, float FireCooldown, int TargetMask (0=all), int Damage
// - ProjectileDefaults defines Speed, Radius, Life

[BurstCompile]
public partial struct ShootingSystem : ISystem
{
    private EntityQuery _buildingQuery;
    private EntityQuery _miniQuery;
    private EntityQuery _defaultsQuery;

    public void OnCreate(ref SystemState s)
    {
        // ✅ Buildings considered as targets: DO NOT require BuildingTarget here.
        _buildingQuery = s.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<AttackGroup>(),
            ComponentType.ReadOnly<Team>(),
            ComponentType.ReadOnly<UnitHealth>());

        _miniQuery = s.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<Agent>(),
            ComponentType.ReadOnly<UnitHealth>());

        _defaultsQuery = s.GetEntityQuery(ComponentType.ReadOnly<ProjectileDefaults>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        if (_defaultsQuery.CalculateEntityCount() == 0)
            return;

        var defs = em.GetComponentData<ProjectileDefaults>(_defaultsQuery.GetSingletonEntity());

        // --- snapshot buildings (targets)
        var bEnts   = _buildingQuery.ToEntityArray(Allocator.Temp);
        var bXfs    = _buildingQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var bGroups = _buildingQuery.ToComponentDataArray<AttackGroup>(Allocator.Temp);
        var bTeams  = _buildingQuery.ToComponentDataArray<Team>(Allocator.Temp);
        var bHPs    = _buildingQuery.ToComponentDataArray<UnitHealth>(Allocator.Temp);

        // --- snapshot minis (targets)
        var mEnts = _miniQuery.ToEntityArray(Allocator.Temp);
        var mXfs  = _miniQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var mAgs  = _miniQuery.ToComponentDataArray<Agent>(Allocator.Temp);
        var mHPs  = _miniQuery.ToComponentDataArray<UnitHealth>(Allocator.Temp);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Shooters can be minis OR buildings (Agent optional)
        foreach (var (xf, shooter, cd, e) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<Shooter>, RefRW<ShooterCooldown>>()
                     .WithEntityAccess())
        {
            // Determine shooter's team as Faction
            Faction myTeam;
            if (em.HasComponent<Agent>(e))
                myTeam = em.GetComponentData<Agent>(e).Faction;
            else if (em.HasComponent<Team>(e))
                myTeam = em.GetComponentData<Team>(e).Value;
            else
                continue; // no team info → skip

            float3 myPos  = xf.ValueRO.Position;
            float  range  = shooter.ValueRO.Range;
            float  range2 = range * range;

            // Target mask: if 0, allow all groups (Ground/Air/Orbital)
            int allowed = shooter.ValueRO.TargetMask;
            if (allowed == 0)
                allowed = (1 << (int)GroupKind.Ground) | (1 << (int)GroupKind.Air) | (1 << (int)GroupKind.Orbital);

            Entity best   = Entity.Null;
            float  bestD2 = float.MaxValue;

            // ---------- scan minis (skip dead, same team, honor mask)
            for (int i = 0; i < mEnts.Length; i++)
            {
                if (mHPs[i].Value <= 0)                continue;
                if (mAgs[i].Faction == myTeam)         continue;

                GroupKind mgKind = GroupKind.Ground;
                if (em.HasComponent<AttackGroup>(mEnts[i]))
                    mgKind = em.GetComponentData<AttackGroup>(mEnts[i]).Value;

                int bit = 1 << (int)mgKind;
                if ((allowed & bit) == 0)              continue;

                float2 d  = myPos.xy - mXfs[i].Position.xy;
                float  d2 = math.lengthsq(d);
                if (d2 <= range2 && d2 < bestD2)
                {
                    bestD2 = d2;
                    best   = mEnts[i];
                }
            }

            // ---------- scan buildings (skip dead, same team, honor mask)
            for (int i = 0; i < bEnts.Length; i++)
            {
                if (bHPs[i].Value <= 0)                continue;
                if (bTeams[i].Value == myTeam)         continue;

                int bit = 1 << (int)bGroups[i].Value;
                if ((allowed & bit) == 0)              continue;

                float2 d  = myPos.xy - bXfs[i].Position.xy;
                float  d2 = math.lengthsq(d);
                if (d2 <= range2 && d2 < bestD2)
                {
                    bestD2 = d2;
                    best   = bEnts[i];
                }
            }

            // --- If no target in range: clear Attacking so movers resume
            if (best == Entity.Null)
            {
                if (em.HasComponent<Attacking>(e))
                    ecb.RemoveComponent<Attacking>(e);

                // tick cooldown if any
                if (cd.ValueRO.TimeLeft > 0f)
                    cd.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime;

                continue;
            }

            // --- We have a target in range: hold Attacking to pause movement while aiming/firing
            if (!em.HasComponent<Attacking>(e))
                ecb.AddComponent<Attacking>(e);

            // Fire only if cooldown elapsed
            if (cd.ValueRO.TimeLeft > 0f)
            {
                cd.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime;
                continue;
            }

            // ---------- spawn projectile ----------
            var tgtXf = em.GetComponentData<LocalTransform>(best);
            float3 dir = tgtXf.Position - myPos;
            float  len = math.length(dir);
            if (len > 1e-5f)
            {
                dir /= len;

                var p = ecb.CreateEntity();
                ecb.AddComponent(p, LocalTransform.FromPositionRotationScale(
                    new float3(myPos.x, myPos.y, -0.02f), quaternion.identity, 0.12f));
                ecb.AddComponent(p, new Projectile
                {
                    Faction = myTeam,
                    Radius  = defs.Radius,
                    Damage  = shooter.ValueRO.Damage
                });
                ecb.AddComponent(p, new Velocity { Value = dir * defs.Speed });
                ecb.AddComponent(p, new Lifetime { Seconds = defs.Life });

                // reset shooter cooldown
                cd.ValueRW.TimeLeft = shooter.ValueRO.FireCooldown;
            }
        }

        ecb.Playback(em);
        ecb.Dispose();

        bEnts.Dispose(); bXfs.Dispose(); bGroups.Dispose(); bTeams.Dispose(); bHPs.Dispose();
        mEnts.Dispose(); mXfs.Dispose(); mAgs.Dispose(); mHPs.Dispose();
    }
}
