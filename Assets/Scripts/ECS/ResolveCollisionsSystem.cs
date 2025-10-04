using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct ResolveCollisionsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em  = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (xf, agent, tgt, e) in SystemAPI
                 .Query<RefRO<LocalTransform>, RefRO<Agent>, RefRO<Target>>()
                 .WithAll<InCollisionRange>()
                 .WithEntityAccess())
        {
            var other = tgt.ValueRO.Value;
            if (!em.Exists(other)) continue;
            if (!em.HasComponent<LocalTransform>(other) || !em.HasComponent<Agent>(other)) continue;

            var oxf = em.GetComponentData<LocalTransform>(other);
            var oag = em.GetComponentData<Agent>(other);

            float thresh = agent.ValueRO.Radius + oag.Radius;
            if (math.lengthsq(oxf.Position - xf.ValueRO.Position) <= thresh * thresh)
            {
                bool otherSeesUs =
                    em.HasComponent<Target>(other) &&
                    em.HasComponent<InCollisionRange>(other) &&
                    em.GetComponentData<Target>(other).Value == e;

                if (otherSeesUs)
                {
                    ecb.DestroyEntity(e);
                    ecb.DestroyEntity(other);
                }
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
