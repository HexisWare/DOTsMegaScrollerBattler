using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;


[WorldSystemFilter(WorldSystemFilterFlags.Default)]
public partial class HealthBarUpdateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var em = EntityManager;

        foreach (var (hp, hb, owner) in SystemAPI.Query<RefRO<UnitHealth>, RefRO<HealthBarChild>>().WithEntityAccess())
        {
            if (hb.ValueRO.Fill == Entity.Null || !em.Exists(hb.ValueRO.Fill))
                continue;

            float max = math.max(1, hp.ValueRO.Max);
            float ratio = math.clamp(hp.ValueRO.Value / max, 0f, 1f);

            // Scale X by ratio using PostTransformMatrix
            if (em.HasComponent<PostTransformMatrix>(hb.ValueRO.Fill))
            {
                var post = em.GetComponentData<PostTransformMatrix>(hb.ValueRO.Fill);
                post.Value = float4x4.Scale(new float3(hb.ValueRO.Width * ratio, hb.ValueRO.Height, 1f));
                em.SetComponentData(hb.ValueRO.Fill, post);
            }

            // Left-anchor the fill by shifting its local X
            if (em.HasComponent<LocalTransform>(hb.ValueRO.Fill))
            {
                var xf = em.GetComponentData<LocalTransform>(hb.ValueRO.Fill);
                float xLeftAnchored = (ratio - 1f) * hb.ValueRO.Width * 0.5f; // -w/2 .. +w/2
                xf.Position = new float3(xLeftAnchored, hb.ValueRO.Offset.y, hb.ValueRO.Offset.z);
                em.SetComponentData(hb.ValueRO.Fill, xf);
            }

            // Optional: hide BG/Fill when dead
            if (hp.ValueRO.Value <= 0)
            {
                if (em.HasComponent<URPMaterialPropertyBaseColor>(hb.ValueRO.Fill))
                {
                    var c = em.GetComponentData<URPMaterialPropertyBaseColor>(hb.ValueRO.Fill);
                    c.Value.w = 0f;
                    em.SetComponentData(hb.ValueRO.Fill, c);
                }
            }
        }
    }
}
