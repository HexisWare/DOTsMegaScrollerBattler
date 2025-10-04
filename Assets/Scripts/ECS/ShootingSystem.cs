using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering; // URPMaterialPropertyBaseColor

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BuildSpatialGridSystem))]
public partial struct ShootingSystem : ISystem
{
    private ComponentLookup<LocalTransform> _xfLookup;
    private ComponentLookup<AttackGroup> _groupLookup;

    public void OnCreate(ref SystemState s)
    {
        _xfLookup = s.GetComponentLookup<LocalTransform>(true);
        _groupLookup = s.GetComponentLookup<AttackGroup>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        _xfLookup.Update(ref s);
        _groupLookup.Update(ref s);

        float dt = SystemAPI.Time.DeltaTime;
        var defs = SystemAPI.GetSingleton<ProjectileDefaults>();

        var em  = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Spatial grid
        var gridHandle = s.WorldUnmanaged.GetExistingUnmanagedSystem<BuildSpatialGridSystem>();
        ref var grid   = ref s.WorldUnmanaged.GetUnsafeSystemRef<BuildSpatialGridSystem>(gridHandle);
        var map  = grid.Map;
        float cell = SystemAPI.GetSingleton<GridParams>().CellSize;

        foreach (var (xf, agent, shooter, cd, e) in SystemAPI
                 .Query<RefRO<LocalTransform>, RefRO<Agent>, RefRO<Shooter>, RefRW<ShooterCooldown>>()
                 .WithEntityAccess())
        {
            float3 myPos = xf.ValueRO.Position;

            // Expand scan radius by Shooter.Range
            int maxCells = math.clamp((int)math.ceil(shooter.ValueRO.Range / cell), 1, 64);
            int2 baseCell = (int2)math.floor(myPos.xy / cell);

            Entity best = Entity.Null;
            float bestD2 = float.MaxValue;
            float range2 = shooter.ValueRO.Range * shooter.ValueRO.Range;

            for (int dy = -maxCells; dy <= maxCells; dy++)
            {
                for (int dx = -maxCells; dx <= maxCells; dx++)
                {
                    int2 cc = baseCell + new int2(dx, dy);
                    int key = (cc.x * 73856093) ^ (cc.y * 19349663);

                    NativeParallelMultiHashMapIterator<int> it;
                    BuildSpatialGridSystem.Item item;
                    if (!map.TryGetFirstValue(key, out item, out it)) continue;

                    do
                    {
                        if (item.F == agent.ValueRO.Faction) continue;

                        // 🔑 Check target's AttackGroup vs shooter.TargetMask
                        if (!_groupLookup.HasComponent(item.E)) continue;
                        var g = _groupLookup[item.E].Value;
                        int bit = 1 << (int)g;
                        if ( (shooter.ValueRO.TargetMask & bit) == 0 ) continue;

                        float2 d = myPos.xy - item.Pos.xy;
                        float d2 = math.lengthsq(d);
                        if (d2 <= range2 && d2 < bestD2) { bestD2 = d2; best = item.E; }
                    }
                    while (map.TryGetNextValue(out item, ref it));
                }
            }

            bool inRange = (best != Entity.Null) && _xfLookup.HasComponent(best);

            // --- Set/clear Attacking tag so the mover can hold position ---
            bool hasAttackTag = SystemAPI.HasComponent<Attacking>(e);
            if (inRange && !hasAttackTag) ecb.AddComponent<Attacking>(e);
            else if (!inRange && hasAttackTag) ecb.RemoveComponent<Attacking>(e);

            // --- Handle cooldown & fire only if ready ---
            var c = cd.ValueRO;
            c.TimeLeft -= dt;

            if (!inRange || c.TimeLeft > 0f)
            {
                cd.ValueRW = c;
                continue;
            }

            // Fire toward snapshot pos
            float3 tpos = _xfLookup[best].Position;
            float3 to   = tpos - myPos;
            float len   = math.length(to);
            float3 dir  = (len > 1e-5f) ? (to / len) : new float3(1,0,0);

            var p = ecb.CreateEntity();
            ecb.AddComponent(p, LocalTransform.FromPositionRotationScale(myPos, quaternion.identity, 0.12f));
            ecb.AddComponent(p, new Projectile { Faction = agent.ValueRO.Faction, Radius = defs.Radius, Damage = shooter.ValueRO.Damage });
            ecb.AddComponent(p, new Velocity   { Value   = dir * defs.Speed });
            ecb.AddComponent(p, new Lifetime   { Seconds = defs.Life });

            var col = (agent.ValueRO.Faction == Faction.Player) ? defs.PlayerColor : defs.EnemyColor;
            ecb.AddComponent(p, new URPMaterialPropertyBaseColor { Value = col });
            ecb.AddComponent<NeedsRenderSetup>(p);

            c.TimeLeft = math.max(0.01f, shooter.ValueRO.FireCooldown);
            cd.ValueRW = c;
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
