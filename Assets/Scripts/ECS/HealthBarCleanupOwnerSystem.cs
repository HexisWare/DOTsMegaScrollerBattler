using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct HealthBarCleanupOwnerSystem : ISystem
{
    [BurstCompile] public void OnCreate(ref SystemState s) {}

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em  = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (hp, link, owner) in SystemAPI
                     .Query<RefRO<UnitHealth>, RefRO<HealthBarChild>>()
                     .WithEntityAccess())
        {
            if (hp.ValueRO.Value > 0) continue;

            var bg   = link.ValueRO.Bg;
            var fill = link.ValueRO.Fill;

            if (bg   != Entity.Null && em.Exists(bg))   ecb.DestroyEntity(bg);
            if (fill != Entity.Null && em.Exists(fill)) ecb.DestroyEntity(fill);

            // Remove the link so no further systems try to update missing bars
            ecb.RemoveComponent<HealthBarChild>(owner);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
