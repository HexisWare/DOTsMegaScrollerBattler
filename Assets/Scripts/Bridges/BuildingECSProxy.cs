using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildingECSProxy : MonoBehaviour
{
    public BuildingStatsMono stats;     // set by applier or manually
    public float proxyRadius = 0.65f;   // collision radius for projectiles

    private EntityManager _em;
    private Entity _e;
    private static bool s_quitting;

    void OnApplicationQuit() => s_quitting = true;

    void Awake()
    {
        if (stats == null) stats = GetComponent<BuildingStatsMono>();
        if (stats == null)
        {
            Debug.LogError("[BuildingECSProxy] Missing BuildingStatsMono.", this);
            enabled = false; return;
        }

        _em = World.DefaultGameObjectInjectionWorld.EntityManager;

        _e = _em.CreateEntity(
            typeof(LocalTransform),
            typeof(LocalToWorld),
            typeof(UnitHealth),
            typeof(AttackGroup),
            typeof(Team),
            typeof(BuildingTarget),
            typeof(BuildingHitbox)
        );

        var p = transform.position;
        _em.SetComponentData(_e, LocalTransform.FromPositionRotationScale(new float3(p.x, p.y, -0.02f), quaternion.identity, 1f));
        _em.SetComponentData(_e, new UnitHealth { Value = stats.currentHP, Max = stats.maxHP });
        _em.SetComponentData(_e, new AttackGroup { Value = stats.group });
        _em.SetComponentData(_e, new Team { Value = stats.faction });
        _em.SetComponentData(_e, new BuildingHitbox { Radius = proxyRadius });
    }

    void OnDestroy()
    {
        if (s_quitting) return;               // Unity is tearing down; EntityManager is gone
        if (_e == Entity.Null) return;
        try
        {
            if (_em.Exists(_e)) _em.DestroyEntity(_e);
        }
        catch (System.ObjectDisposedException) { /* world already gone; ignore */ }
    }

    void LateUpdate()
    {
        if (_e == Entity.Null) return;

        try
        {
            if (!_em.Exists(_e)) return;

            // keep ECS position synced with GameObject
            var xf = _em.GetComponentData<LocalTransform>(_e);
            var p = transform.position;
            xf.Position = new float3(p.x, p.y, -0.02f);
            _em.SetComponentData(_e, xf);

            // pull HP from ECS to Mono (so UI/logic can read it)
            var hp = _em.GetComponentData<UnitHealth>(_e);
            stats.currentHP = Mathf.RoundToInt(hp.Value);
        }
        catch (System.ObjectDisposedException)
        {
            // Happens on playmode exit/domain reload — safe to ignore
        }
    }
}
