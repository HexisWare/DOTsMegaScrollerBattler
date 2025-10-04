using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShootingSystem))]
public partial struct ProjectileMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (xf, vel) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
            xf.ValueRW.Position += vel.ValueRO.Value * dt;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (life, e) in SystemAPI.Query<RefRW<Lifetime>>().WithEntityAccess())
        {
            var L = life.ValueRO; L.Seconds -= dt;
            if (L.Seconds <= 0f) ecb.DestroyEntity(e);
            else life.ValueRW = L;
        }
        ecb.Playback(s.EntityManager);
        ecb.Dispose();
    }
}
