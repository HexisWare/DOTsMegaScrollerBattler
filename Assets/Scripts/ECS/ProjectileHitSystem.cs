using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct ProjectileHitSystem : ISystem
{
    private EntityQuery _projQ;
    private EntityQuery _miniQ;
    private EntityQuery _bldQ;

    public void OnCreate(ref SystemState s)
    {
        _projQ = s.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<Projectile>()
        );

        _miniQ = s.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<Agent>(),
            ComponentType.ReadWrite<UnitHealth>() // minis have HP
        );

        _bldQ = s.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<BuildingTarget>(),
            ComponentType.ReadOnly<BuildingHitbox>(),
            ComponentType.ReadOnly<Team>(),
            ComponentType.ReadWrite<UnitHealth>() // buildings have HP too
        );
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;

        var projEnts = _projQ.ToEntityArray(Allocator.Temp);
        var projXfs  = _projQ.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var projs    = _projQ.ToComponentDataArray<Projectile>(Allocator.Temp);

        var miniEnts = _miniQ.ToEntityArray(Allocator.Temp);
        var miniXfs  = _miniQ.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var miniAgts = _miniQ.ToComponentDataArray<Agent>(Allocator.Temp);
        var miniHPs  = _miniQ.ToComponentDataArray<UnitHealth>(Allocator.Temp);

        var bldEnts  = _bldQ.ToEntityArray(Allocator.Temp);
        var bldXfs   = _bldQ.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var bldHit   = _bldQ.ToComponentDataArray<BuildingHitbox>(Allocator.Temp);
        var bldTeams = _bldQ.ToComponentDataArray<Team>(Allocator.Temp);
        var bldHPs   = _bldQ.ToComponentDataArray<UnitHealth>(Allocator.Temp);

        var toDestroy = new NativeList<Entity>(Allocator.Temp);

        for (int i = 0; i < projEnts.Length; i++)
        {
            var pEnt = projEnts[i];
            var pXf  = projXfs[i];
            var p    = projs[i];

            bool killed = false;

            // --- Hit minis first (same as before) ---
            for (int j = 0; j < miniEnts.Length; j++)
            {
                if (miniAgts[j].Faction == p.Faction) continue; // only hit enemies

                float r = p.Radius + miniAgts[j].Radius;
                float r2 = r * r;
                float2 d = pXf.Position.xy - miniXfs[j].Position.xy;
                if (math.lengthsq(d) <= r2)
                {
                    // damage mini
                    var hp = miniHPs[j];
                    hp.Value -= (int)math.round(p.Damage);
                    em.SetComponentData(miniEnts[j], hp);
                    toDestroy.Add(pEnt);
                    killed = true;
                    break;
                }
            }
            if (killed) continue;

            // --- Hit BUILDINGS ---
            for (int j = 0; j < bldEnts.Length; j++)
            {
                if (bldTeams[j].Value == p.Faction) continue; // only hit enemy buildings

                float r = p.Radius + bldHit[j].Radius;
                float r2 = r * r;
                float2 d = pXf.Position.xy - bldXfs[j].Position.xy;
                if (math.lengthsq(d) <= r2)
                {
                    var hp = bldHPs[j];
                    hp.Value -= (int)math.round(p.Damage);
                    em.SetComponentData(bldEnts[j], hp);
                    toDestroy.Add(pEnt);
                    break;
                }
            }
        }

        // Destroy all projectiles that hit something
        for (int k = 0; k < toDestroy.Length; k++)
            if (em.Exists(toDestroy[k])) em.DestroyEntity(toDestroy[k]);

        projEnts.Dispose(); projXfs.Dispose(); projs.Dispose();
        miniEnts.Dispose(); miniXfs.Dispose(); miniAgts.Dispose(); miniHPs.Dispose();
        bldEnts.Dispose();  bldXfs.Dispose();  bldHit.Dispose();  bldTeams.Dispose(); bldHPs.Dispose();
        toDestroy.Dispose();
    }
}
