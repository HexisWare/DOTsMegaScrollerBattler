// Assets/Scripts/ECS/ProjectileHitSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileMoveSystem))]
[UpdateBefore(typeof(ResolveCollisionsSystem))]
public partial struct ProjectileHitSystem : ISystem
{
    private ComponentLookup<UnitHealth> _hpLookup; // instance member (not touched by local funcs)

    public void OnCreate(ref SystemState s)
    {
        _hpLookup = s.GetComponentLookup<UnitHealth>(false); // read/write
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        _hpLookup.Update(ref s);

        // Make a local copy so our static helper can mutate via ref without touching 'this'
        var hpLookup = _hpLookup;

        var em  = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Spatial grid
        var gridHandle = s.WorldUnmanaged.GetExistingUnmanagedSystem<BuildSpatialGridSystem>();
        ref var grid   = ref s.WorldUnmanaged.GetUnsafeSystemRef<BuildSpatialGridSystem>(gridHandle);
        var map  = grid.Map;

        float cell = SystemAPI.GetSingleton<GridParams>().CellSize;

        foreach (var (pxf, proj, e) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<Projectile>>()
                     .WithEntityAccess())
        {
            float3 ppos3   = pxf.ValueRO.Position;
            float2 ppos    = ppos3.xy;
            int2 baseCell  = (int2)math.floor(ppos / cell);
            bool hit       = false;
            Projectile pd  = proj.ValueRO;

            // Scan the 9 neighboring cells
            ScanCell(map, baseCell + new int2( 0,  0), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2( 1,  0), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2(-1,  0), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2( 0,  1), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2( 0, -1), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2( 1,  1), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2(-1,  1), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2( 1, -1), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
            ScanCell(map, baseCell + new int2(-1, -1), ppos, pd, ref hit, e, ref ecb, ref hpLookup);
        }

        ecb.Playback(em);
        ecb.Dispose();

        // (No need to assign back to _hpLookup; it’s a handle into ECS data.)
    }

    // Static helper: no capture of 'this'
    private static void ScanCell(
        NativeParallelMultiHashMap<int, BuildSpatialGridSystem.Item> map,
        int2 cell,
        float2 projPos,
        Projectile projData,
        ref bool hit,
        Entity projectileEntity,
        ref EntityCommandBuffer ecb,
        ref ComponentLookup<UnitHealth> hpLookup)
    {
        if (hit) return;

        int key = (cell.x * 73856093) ^ (cell.y * 19349663);

        NativeParallelMultiHashMapIterator<int> it;
        BuildSpatialGridSystem.Item item;
        if (!map.TryGetFirstValue(key, out item, out it)) return;

        do
        {
            // Only hit opposite faction
            if (projData.Faction == item.F) continue;

            float2 d = projPos - item.Pos.xy;
            float  r = projData.Radius + item.Radius;

            if (math.lengthsq(d) <= r * r)
            {
                // Apply damage if UnitHealth exists, otherwise kill
                if (hpLookup.HasComponent(item.E))
                {
                    var hp = hpLookup[item.E];
                    hp.Value -= projData.Damage;
                    if (hp.Value <= 0f) ecb.DestroyEntity(item.E);
                    else hpLookup[item.E] = hp;
                }
                else
                {
                    ecb.DestroyEntity(item.E);
                }

                // Destroy projectile on hit
                ecb.DestroyEntity(projectileEntity);
                hit = true;
                return;
            }
        }
        while (map.TryGetNextValue(out item, ref it));
    }
}
