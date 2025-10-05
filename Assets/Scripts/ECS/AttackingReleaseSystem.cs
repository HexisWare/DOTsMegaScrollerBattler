// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;

// [BurstCompile]
// [UpdateInGroup(typeof(SimulationSystemGroup))]
// [UpdateAfter(typeof(ShootingSystem))]
// public partial struct AttackingReleaseSystem : ISystem
// {
//     [BurstCompile] public void OnCreate(ref SystemState s) {}

//     [BurstCompile]
//     public void OnUpdate(ref SystemState s)
//     {
//         var ecb = new EntityCommandBuffer(Allocator.Temp);

//         // Keep Attacking for a tiny window after a shot so movement visibly pauses.
//         foreach (var (cd, shooter, e) in SystemAPI
//                      .Query<RefRO<ShooterCooldown>, RefRO<Shooter>>()
//                      .WithAll<Attacking>()
//                      .WithEntityAccess())
//         {
//             // Right after firing: cd == FireCooldown. Keep tag for ~0.05s:
//             // Remove when the remaining cooldown falls below (FireCooldown - 0.05).
//             if (cd.ValueRO.TimeLeft <= shooter.ValueRO.FireCooldown - 0.05f)
//                 ecb.RemoveComponent<Attacking>(e);
//         }

//         ecb.Playback(s.EntityManager);
//         ecb.Dispose();
//     }
// }
