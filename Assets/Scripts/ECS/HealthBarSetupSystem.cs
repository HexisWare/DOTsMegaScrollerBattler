using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

[WorldSystemFilter(WorldSystemFilterFlags.Default)]
public partial class HealthBarSetupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var sp = MiniSquareSpawner.Instance;
        if (sp == null) return;

        var em = EntityManager;

        var q = SystemAPI.QueryBuilder()
            .WithAll<UnitHealth, LocalTransform>()
            .WithNone<HealthBarChild>()
            .Build();

        using var ents = q.ToEntityArray(Allocator.Temp);
        if (ents.Length == 0) return;

        foreach (var e in ents)
        {
            bool isBuilding = em.HasComponent<BuildingTarget>(e);
            bool isMini     = em.HasComponent<Agent>(e);
            if (!isBuilding && !isMini) continue;

            float radius = 0.25f;
            if (isMini && em.HasComponent<Agent>(e))
                radius = math.max(0.12f, em.GetComponentData<Agent>(e).Radius);
            else if (isBuilding && em.HasComponent<BuildingHitbox>(e))
                radius = em.GetComponentData<BuildingHitbox>(e).Radius;

            float width  = isBuilding ? math.max(1.2f, radius * 1.8f) : math.max(0.6f, radius * 1.6f);
            float height = isBuilding ? 0.12f : 0.08f;

            // Offsets: buildings a little closer; minis a little higher
            float yoff   = isBuilding ? (radius + 0.30f) : (radius + 0.40f);
            var   offset = new float3(0f, yoff, -0.03f);

            var bgCol = new float4(0, 0, 0, 0.50f);
            var fgCol = new float4(0.2f, 0.95f, 0.2f, 1f);
            if (em.HasComponent<Agent>(e) && em.GetComponentData<Agent>(e).Faction == Faction.Enemy)
                fgCol = new float4(0.95f, 0.25f, 0.25f, 1f);
            if (em.HasComponent<Team>(e) && em.GetComponentData<Team>(e).Value == Faction.Enemy)
                fgCol = new float4(0.95f, 0.45f, 0.25f, 1f);

            var bg   = em.CreateEntity();
            var fill = em.CreateEntity();

            var desc = new RenderMeshDescription(ShadowCastingMode.Off, receiveShadows: false);

            // ---- BG ----
            em.AddComponentData(bg, new Parent { Value = e });
            em.AddComponentData(bg, LocalTransform.FromPositionRotationScale(offset, quaternion.identity, 1f));
            em.AddComponentData(bg, new LocalToWorld());
            em.AddComponentData(bg, new PostTransformMatrix { Value = float4x4.Scale(new float3(width, height, 1f)) });
            RenderMeshUtility.AddComponents(bg, em, desc, sp.RMA, sp.MMI);
            em.AddComponentData(bg, new URPMaterialPropertyBaseColor { Value = bgCol });

            // Hookup: tag the bar and point it at its owner
            em.AddComponentData(bg, new HealthBarOwner { Owner = e });
            em.AddComponent<HealthBarElement>(bg);

            // ---- FILL ----
            em.AddComponentData(fill, new Parent { Value = e });
            em.AddComponentData(fill, LocalTransform.FromPositionRotationScale(offset, quaternion.identity, 1f));
            em.AddComponentData(fill, new LocalToWorld());
            em.AddComponentData(fill, new PostTransformMatrix { Value = float4x4.Scale(new float3(width, height, 1f)) });
            RenderMeshUtility.AddComponents(fill, em, desc, sp.RMA, sp.MMI);
            em.AddComponentData(fill, new URPMaterialPropertyBaseColor { Value = fgCol });

            // Hookup: tag the bar and point it at its owner
            em.AddComponentData(fill, new HealthBarOwner { Owner = e });
            em.AddComponent<HealthBarElement>(fill);

            // Link stored on the OWNER so we can delete both bars on death quickly
            em.AddComponentData(e, new HealthBarChild {
                Bg     = bg,
                Fill   = fill,
                Width  = width,
                Height = height,
                Offset = offset
            });
        }
    }
}
