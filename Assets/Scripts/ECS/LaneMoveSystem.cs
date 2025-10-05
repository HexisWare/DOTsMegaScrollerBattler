// Assets/Scripts/ECS/LaneMoveSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShootingSystem))]               // <— make sure we see Attacking this frame
public partial struct LaneMoveSystem : ISystem
{
    [BurstCompile] public void OnCreate(ref SystemState s) {}

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // ❗ Exclude entities that are currently Attacking (they’ll “pause”)
        foreach (var (xf, agent) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRO<Agent>>()
                     .WithNone<Attacking>())
        {
            // simple lane move: left team moves +X, right team moves -X
            float dir = agent.ValueRO.Faction == Faction.Player ? 1f : -1f;

            var t = xf.ValueRO;
            t.Position += new float3(dir * agent.ValueRO.MoveSpeed * dt, 0f, 0f);
            xf.ValueRW = t;
        }
    }
}
