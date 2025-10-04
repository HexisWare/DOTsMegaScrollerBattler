using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;            // RenderMeshArray, RenderMeshDescription, RenderMeshUtility
using Unity.Transforms;
using UnityEngine.Rendering;      // ShadowCastingMode

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShootingSystem))]
public partial struct ProjectileRenderSetupSystem : ISystem
{
    private EntityQuery _needsRenderQ;
    private EntityQuery _sampleRenderQ;

    public void OnCreate(ref SystemState s)
    {
        // Any projectiles waiting for render hookup
        _needsRenderQ = s.GetEntityQuery(
            ComponentType.ReadOnly<NeedsRenderSetup>(),
            ComponentType.ReadOnly<LocalTransform>());

        // Any entity that already has Entities Graphics render bits we can copy (your minis)
        _sampleRenderQ = s.GetEntityQuery(
            ComponentType.ReadOnly<MaterialMeshInfo>(),
            ComponentType.ReadOnly<RenderMeshArray>());
    }

    public void OnUpdate(ref SystemState s)
    {
        if (_needsRenderQ.IsEmptyIgnoreFilter) return;
        if (_sampleRenderQ.IsEmptyIgnoreFilter) return;

        var em = s.EntityManager;

        using var samples = _sampleRenderQ.ToEntityArray(Allocator.Temp);
        var sample = samples[0]; // just grab one mini as the prototype

        var mmi = em.GetComponentData<MaterialMeshInfo>(sample);
        var rma = em.GetSharedComponentManaged<RenderMeshArray>(sample);

        using var pending = _needsRenderQ.ToEntityArray(Allocator.Temp);
        var desc = new RenderMeshDescription(ShadowCastingMode.Off, false);

        for (int i = 0; i < pending.Length; i++)
        {
            var e = pending[i];
            RenderMeshUtility.AddComponents(e, em, desc, rma, mmi);
            em.RemoveComponent<NeedsRenderSetup>(e);
        }
    }
}
