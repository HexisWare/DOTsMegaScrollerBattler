using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShootingSystem))]  // make sure this runs after your shooting
public partial struct AttackingReleaseSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState s) {}

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // If you ever add Attacking only when firing, this makes it last just a frame or two.
        foreach (var (cd, e) in SystemAPI
                     .Query<RefRO<ShooterCooldown>>()
                     .WithAll<Attacking>()
                     .WithEntityAccess())
        {
            if (cd.ValueRO.TimeLeft > 0.05f)   // hold pose for ~1-2 frames
                ecb.RemoveComponent<Attacking>(e);
        }

        ecb.Playback(s.EntityManager);
        ecb.Dispose();
    }
}
