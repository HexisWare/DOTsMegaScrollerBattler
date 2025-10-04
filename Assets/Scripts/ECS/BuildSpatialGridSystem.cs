using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BuildSpatialGridSystem : ISystem
{
    public struct Item { public Entity E; public float3 Pos; public Faction F; public float Radius; }

    private NativeParallelMultiHashMap<int, Item> _map;

    public void OnCreate(ref SystemState s)
    {
        // Seed default GridParams if none exists (baker/authoring can also set it)
        if (SystemAPI.TryGetSingleton<GridParams>(out _) == false)
        {
            var cfg = s.EntityManager.CreateEntity(typeof(GridParams));
            s.EntityManager.SetComponentData(cfg, new GridParams { CellSize = 0.5f });
        }

        _map = new NativeParallelMultiHashMap<int, Item>(1024, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState s)
    {
        if (_map.IsCreated) _map.Dispose();
    }

    // Expose to other systems (access via GetUnsafeSystemRef)
    public NativeParallelMultiHashMap<int, Item> Map => _map;

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        float cell = SystemAPI.GetSingleton<GridParams>().CellSize; // <-- no static!

        _map.Clear();

        // Optional capacity nudge to reduce reallocs
        int est = SystemAPI.QueryBuilder().WithAll<Agent, LocalTransform>().Build().CalculateEntityCount();
        if (_map.Capacity < est * 2)
            _map.Capacity = math.max(_map.Capacity, est * 2);

        foreach (var (xf, agent, e) in SystemAPI
                 .Query<RefRO<LocalTransform>, RefRO<Agent>>()
                 .WithEntityAccess())
        {
            int2 c = (int2)math.floor(xf.ValueRO.Position.xy / cell);
            int key = (c.x * 73856093) ^ (c.y * 19349663);
            _map.Add(key, new Item {
                E = e,
                Pos = xf.ValueRO.Position,
                F = agent.ValueRO.Faction,
                Radius = agent.ValueRO.Radius
            });
        }
    }
}
