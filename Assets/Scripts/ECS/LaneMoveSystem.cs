using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct LaneMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (xf, agent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Agent>>())
        {
            float dir = (agent.ValueRO.Faction == Faction.Player) ? 1f : -1f;
            xf.ValueRW.Position += new float3(dir * agent.ValueRO.MoveSpeed * dt, 0, 0);
        }
    }
}
