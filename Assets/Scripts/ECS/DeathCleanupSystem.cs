using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public partial struct DeathCleanupSystem : ISystem
{
    private EntityQuery _deadMinis;
    private EntityQuery _deadBuildings;

    public void OnCreate(ref SystemState s)
    {
        _deadMinis = s.GetEntityQuery(new EntityQueryDesc {
            All  = new ComponentType[] { typeof(UnitHealth), typeof(Agent) },
        });

        _deadBuildings = s.GetEntityQuery(new EntityQueryDesc {
            All  = new ComponentType[] { typeof(UnitHealth), typeof(BuildingTarget) },
            None = new ComponentType[] { typeof(Disabled) }
        });
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Minis → destroy + clean bars
        using (var ents = _deadMinis.ToEntityArray(Allocator.Temp))
        using (var hps  = _deadMinis.ToComponentDataArray<UnitHealth>(Allocator.Temp))
        {
            for (int i = 0; i < ents.Length; i++)
            {
                if (hps[i].Value <= 0)
                {
                    if (em.HasComponent<HealthBarChild>(ents[i]))
                    {
                        var hb = em.GetComponentData<HealthBarChild>(ents[i]);
                        if (hb.Bg   != Entity.Null && em.Exists(hb.Bg))   ecb.DestroyEntity(hb.Bg);
                        if (hb.Fill != Entity.Null && em.Exists(hb.Fill)) ecb.DestroyEntity(hb.Fill);
                    }
                    ecb.DestroyEntity(ents[i]);
                }
            }
        }

        // Buildings → disable + clean bars
        using (var entsB = _deadBuildings.ToEntityArray(Allocator.Temp))
        using (var hpsB  = _deadBuildings.ToComponentDataArray<UnitHealth>(Allocator.Temp))
        {
            for (int i = 0; i < entsB.Length; i++)
            {
                if (hpsB[i].Value <= 0)
                {
                    if (em.HasComponent<HealthBarChild>(entsB[i]))
                    {
                        var hb = em.GetComponentData<HealthBarChild>(entsB[i]);
                        if (hb.Bg   != Entity.Null && em.Exists(hb.Bg))   ecb.DestroyEntity(hb.Bg);
                        if (hb.Fill != Entity.Null && em.Exists(hb.Fill)) ecb.DestroyEntity(hb.Fill);
                        ecb.RemoveComponent<HealthBarChild>(entsB[i]);
                    }
                    if (!em.HasComponent<Disabled>(entsB[i]))
                        ecb.AddComponent<Disabled>(entsB[i]);
                }
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
