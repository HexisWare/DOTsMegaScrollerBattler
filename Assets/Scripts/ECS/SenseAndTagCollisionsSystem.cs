// Assets/Scripts/ECS/SenseAndTagCollisionsSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SenseAndTagCollisionsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        // Access the spatial grid built earlier
        var gridHandle = s.WorldUnmanaged.GetExistingUnmanagedSystem<BuildSpatialGridSystem>();
        ref var gridSys = ref s.WorldUnmanaged.GetUnsafeSystemRef<BuildSpatialGridSystem>(gridHandle);
        var map = gridSys.Map;

        float cell = SystemAPI.GetSingleton<GridParams>().CellSize;

        // Record structural changes here, play them back after the loop
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (xf, agent, e) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<Agent>>()
                     .WithEntityAccess())
        {
            int2 baseCell = (int2)math.floor(xf.ValueRO.Position.xy / cell);

            Entity best = Entity.Null;
            float bestD2 = float.MaxValue;

            // local helper = no managed alloc
            void ScanCell(int2 c)
            {
                int key = (c.x * 73856093) ^ (c.y * 19349663);
                NativeParallelMultiHashMapIterator<int> it;
                BuildSpatialGridSystem.Item item;
                if (!map.TryGetFirstValue(key, out item, out it)) return;

                do
                {
                    if (item.E == e) continue; // skip self quickly
                    if (item.F == agent.ValueRO.Faction) continue;

                    float d2 = math.lengthsq(item.Pos - xf.ValueRO.Position);
                    float thresh = math.max(agent.ValueRO.DetectRange, agent.ValueRO.Radius + item.Radius);
                    if (d2 <= thresh * thresh && d2 < bestD2)
                    {
                        bestD2 = d2;
                        best   = item.E;
                    }
                }
                while (map.TryGetNextValue(out item, ref it));
            }

            // 9-cell scan: self + 8 neighbors
            ScanCell(baseCell + new int2( 0,  0));
            ScanCell(baseCell + new int2( 1,  0));
            ScanCell(baseCell + new int2(-1,  0));
            ScanCell(baseCell + new int2( 0,  1));
            ScanCell(baseCell + new int2( 0, -1));
            ScanCell(baseCell + new int2( 1,  1));
            ScanCell(baseCell + new int2(-1,  1));
            ScanCell(baseCell + new int2( 1, -1));
            ScanCell(baseCell + new int2(-1, -1));

            // ——— STRUCTURAL CHANGES VIA ECB ———
            if (best != Entity.Null)
            {
                // Target: add or update
                if (s.EntityManager.HasComponent<Target>(e))
                    ecb.SetComponent(e, new Target { Value = best });
                else
                    ecb.AddComponent(e, new Target { Value = best });

                // InCollisionRange: ensure present
                if (!s.EntityManager.HasComponent<InCollisionRange>(e))
                    ecb.AddComponent<InCollisionRange>(e);
            }
            else
            {
                // Remove markers if present
                if (s.EntityManager.HasComponent<InCollisionRange>(e))
                    ecb.RemoveComponent<InCollisionRange>(e);
                if (s.EntityManager.HasComponent<Target>(e))
                    ecb.RemoveComponent<Target>(e);
            }
        }

        ecb.Playback(s.EntityManager);
        ecb.Dispose();
    }
}
