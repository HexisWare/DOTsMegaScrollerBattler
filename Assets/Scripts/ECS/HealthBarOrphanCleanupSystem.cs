using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct HealthBarOrphanCleanupSystem : ISystem
{
    private EntityQuery _barsQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState s)
    {
        // All bar entities carry HealthBarElement (bg/fill)
        _barsQuery = s.GetEntityQuery(
            ComponentType.ReadOnly<HealthBarElement>()
        );
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em  = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        using var bars = _barsQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];

            bool destroy = false;

            if (em.HasComponent<HealthBarOwner>(bar))
            {
                var owner = em.GetComponentData<HealthBarOwner>(bar).Owner;

                // Owner doesn't exist → orphan
                if (!em.Exists(owner))
                {
                    destroy = true;
                }
                else
                {
                    // If owner has health and is dead → remove the bar
                    if (em.HasComponent<UnitHealth>(owner))
                    {
                        var hp = em.GetComponentData<UnitHealth>(owner).Value;
                        if (hp <= 0)
                            destroy = true;
                    }
                }
            }
            else
            {
                // Bars that never got an owner assigned are orphans
                destroy = true;
            }

            if (destroy)
                ecb.DestroyEntity(bar);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
