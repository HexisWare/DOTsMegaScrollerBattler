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
            None = new ComponentType[] { typeof(Disabled) }   // only enabled ones
        });
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 1) Destroy dead minis
        using (var ents = _deadMinis.ToEntityArray(Allocator.Temp))
        using (var hps  = _deadMinis.ToComponentDataArray<UnitHealth>(Allocator.Temp))
        {
            for (int i = 0; i < ents.Length; i++)
            {
                if (hps[i].Value <= 0)
                    ecb.DestroyEntity(ents[i]);
            }
        }

        // 2) Disable dead buildings (so they stop being targeted & can’t shoot)
        using (var entsB = _deadBuildings.ToEntityArray(Allocator.Temp))
        using (var hpsB  = _deadBuildings.ToComponentDataArray<UnitHealth>(Allocator.Temp))
        {
            for (int i = 0; i < entsB.Length; i++)
            {
                if (hpsB[i].Value <= 0)
                    ecb.AddComponent<Disabled>(entsB[i]); // remove from queries automatically
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
