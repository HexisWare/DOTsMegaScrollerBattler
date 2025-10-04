using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine.Rendering; // ShadowCastingMode

[WorldSystemFilter(WorldSystemFilterFlags.Default)]
public partial class ProjectileRenderSetupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Need the spawner for mesh/material
        var sp = MiniSquareSpawner.Instance;
        if (sp == null) return;

        var em = EntityManager;

        // Get defaults (for colors)
        var defQ = GetEntityQuery(ComponentType.ReadOnly<ProjectileDefaults>());
        if (defQ.CalculateEntityCount() == 0) return;
        var defs = em.GetComponentData<ProjectileDefaults>(defQ.GetSingletonEntity());

        // Query "naked" projectiles (no MaterialMeshInfo yet)
        var q = SystemAPI.QueryBuilder()
            .WithAll<Projectile, LocalTransform>()
            .WithNone<MaterialMeshInfo>()
            .Build();

        using var ents = q.ToEntityArray(Allocator.Temp);
        if (ents.Length == 0) return;

        var desc = new RenderMeshDescription(ShadowCastingMode.Off, receiveShadows: false);

        foreach (var e in ents)
        {
            // Attach render components
            RenderMeshUtility.AddComponents(e, em, desc, sp.RMA, sp.MMI);

            // Tint by faction
            var proj = em.GetComponentData<Projectile>(e);
            var col = (proj.Faction == Faction.Player) ? defs.PlayerColor : defs.EnemyColor;
            em.AddComponentData(e, new URPMaterialPropertyBaseColor { Value = col });
        }
    }
}
