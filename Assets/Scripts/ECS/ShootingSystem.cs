using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct ShootingSystem : ISystem
{
    private EntityQuery _buildingQuery;
    private EntityQuery _miniQuery;
    private EntityQuery _defaultsQuery;

    public void OnCreate(ref SystemState s)
    {
        _buildingQuery = s.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<AttackGroup>(),
            ComponentType.ReadOnly<Team>(),
            ComponentType.ReadOnly<BuildingTarget>());

        _miniQuery = s.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<Agent>(),
            ComponentType.ReadOnly<UnitHealth>()); // minis must have HP

        _defaultsQuery = s.GetEntityQuery(ComponentType.ReadOnly<ProjectileDefaults>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        if (_defaultsQuery.CalculateEntityCount() == 0)
            return;

        var defs = em.GetComponentData<ProjectileDefaults>(_defaultsQuery.GetSingletonEntity());

        // Snapshot buildings
        var bEnts   = _buildingQuery.ToEntityArray(Allocator.Temp);
        var bXfs    = _buildingQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var bGroups = _buildingQuery.ToComponentDataArray<AttackGroup>(Allocator.Temp);
        var bTeams  = _buildingQuery.ToComponentDataArray<Team>(Allocator.Temp);

        // Snapshot minis (simple scan; fine for hundreds — your grid can stay for movement)
        var mEnts = _miniQuery.ToEntityArray(Allocator.Temp);
        var mXfs  = _miniQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var mAgs  = _miniQuery.ToComponentDataArray<Agent>(Allocator.Temp);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (xf, ag, shooter, cd, e) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<Agent>, RefRO<Shooter>, RefRW<ShooterCooldown>>()
                     .WithEntityAccess())
        {
            // cooldown tick
            if (cd.ValueRO.TimeLeft > 0f)
            {
                cd.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime;
                continue;
            }

            float3 myPos   = xf.ValueRO.Position;
            float  range   = shooter.ValueRO.Range;
            float  range2  = range * range;
            int    myTeam  = (int)ag.ValueRO.Faction;
            int    allowed = shooter.ValueRO.TargetMask;
            if (allowed == 0) allowed = (1 << (int)GroupKind.Ground) | (1 << (int)GroupKind.Air) | (1 << (int)GroupKind.Orbital);

            Entity best = Entity.Null;
            float  bestD2 = float.MaxValue;

            // 1) minis
            for (int i = 0; i < mEnts.Length; i++)
            {
                if ((int)mAgs[i].Faction == myTeam) continue;

                // group: prefer AttackGroup if present; default to Ground
                GroupKind mgKind = GroupKind.Ground;
                if (em.HasComponent<AttackGroup>(mEnts[i]))
                    mgKind = em.GetComponentData<AttackGroup>(mEnts[i]).Value;

                int bit = 1 << (int)mgKind;
                if ((allowed & bit) == 0) continue;

                float2 d = myPos.xy - mXfs[i].Position.xy;
                float d2 = math.lengthsq(d);
                if (d2 <= range2 && d2 < bestD2)
                {
                    bestD2 = d2;
                    best   = mEnts[i];
                }
            }

            // 2) buildings
            for (int i = 0; i < bEnts.Length; i++)
            {
                if ((int)bTeams[i].Value == myTeam) continue;

                int bit = 1 << (int)bGroups[i].Value;
                if ((allowed & bit) == 0) continue;

                float2 d = myPos.xy - bXfs[i].Position.xy;
                float d2 = math.lengthsq(d);
                if (d2 <= range2 && d2 < bestD2)
                {
                    bestD2 = d2;
                    best   = bEnts[i];
                }
            }

            if (best == Entity.Null) continue;

            // fire
            var tgtXf = em.GetComponentData<LocalTransform>(best);
            float3 dir = tgtXf.Position - myPos;
            float len = math.length(dir);
            if (len <= 1e-5f) continue;
            dir /= len;

            var p = ecb.CreateEntity();
            ecb.AddComponent(p, LocalTransform.FromPositionRotationScale(new float3(myPos.x, myPos.y, -0.02f), quaternion.identity, 0.12f));
            ecb.AddComponent(p, new Projectile
            {
                Faction = ag.ValueRO.Faction,
                Radius  = defs.Radius,
                Damage  = shooter.ValueRO.Damage
            });
            ecb.AddComponent(p, new Velocity  { Value = dir * defs.Speed });
            ecb.AddComponent(p, new Lifetime  { Seconds = defs.Life });

            cd.ValueRW.TimeLeft = shooter.ValueRO.FireCooldown;

            // Optional: mark Attacking so your movement pause-on-attack works
            if (!em.HasComponent<Attacking>(e))
                ecb.AddComponent<Attacking>(e);
        }

        ecb.Playback(em);
        ecb.Dispose();

        bEnts.Dispose(); bXfs.Dispose(); bGroups.Dispose(); bTeams.Dispose();
        mEnts.Dispose(); mXfs.Dispose();  mAgs.Dispose();
    }
}
