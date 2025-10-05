using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine.Rendering; // ShadowCastingMode

public class MiniSquareSpawner : MonoBehaviour
{
    public static MiniSquareSpawner Instance;

    [Header("Visuals")]
    public Material urpUnlitMaterial;            // Fallback (colored quad)
    public SpriteMaterialRegistry unitRegistry;  // ← assign UnitSpriteMaterialRegistry asset in Inspector
    public float miniScale = 0.20f;

    [Header("Agent Defaults")]
    public float playerSpeed = 6f;
    public float enemySpeed  = 6f;
    public float detectRange = 0.6f;
    public float radius      = 0.15f;

    [Header("Projectile Defaults (ECS singleton seed)")]
    public float projectileSpeed  = 14f;
    public float projectileRadius = 0.12f;
    public float projectileLife   = 2.0f;
    public Color playerProjectileColor = new Color(0.80f, 1.00f, 1.00f, 1.00f);
    public Color enemyProjectileColor  = new Color(1.00f, 0.55f, 0.55f, 1.00f);

    // internals
    private EntityManager _em;
    private Mesh _mesh;
    private RenderMeshArray _rma;
    private MaterialMeshInfo _mmiFallback;
    private EntityArchetype _archetype;

    // registry map: spriteId -> material index (in _rma)
    private Dictionary<string, int> _matIndexById;

    // expose for other systems that render bullets/bars
    public RenderMeshArray RMA => _rma;
    public MaterialMeshInfo MMI => _mmiFallback;

    void Awake()
    {
        Instance = this;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        Debug.Log("[MiniSquareSpawner] Awake: DOTS rendering + singletons");

        // GridParams singleton
        if (_em.CreateEntityQuery(ComponentType.ReadOnly<GridParams>()).CalculateEntityCount() == 0)
        {
            var cfg = _em.CreateEntity(typeof(GridParams));
            _em.SetComponentData(cfg, new GridParams { CellSize = 0.5f });
        }

        // Fallback URP/Unlit
        if (urpUnlitMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) { Debug.LogError("URP/Unlit shader not found."); enabled = false; return; }
            urpUnlitMaterial = new Material(shader) { name = "AutoMiniSquareMat" };
            urpUnlitMaterial.color = Color.white;
        }

        // Mesh (unit quad)
        _mesh = CreateUnitQuad();

        // Build materials list: [0]=fallback, then registry entries (if any)
        var mats = new List<Material>(16) { urpUnlitMaterial };
        _matIndexById = new Dictionary<string, int>(16, System.StringComparer.Ordinal);

        if (unitRegistry != null)
        {
            unitRegistry.InitIfNeeded();
            if (unitRegistry.entries != null)
            {
                for (int i = 0; i < unitRegistry.entries.Length; i++)
                {
                    var id  = unitRegistry.entries[i].id ?? "";
                    var mat = unitRegistry.entries[i].material;
                    if (string.IsNullOrWhiteSpace(id) || mat == null) continue;

                    // add to array and map the id to this index
                    int idx = mats.Count;
                    mats.Add(mat);
                    _matIndexById[id] = idx;
                }
            }
        }

        // Create RMA with all materials; single shared mesh at index 0
        _rma = new RenderMeshArray(mats.ToArray(), new[] { _mesh });
        _mmiFallback = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

        // Debug mapping so you can see what's available
        Debug.Log($"[MiniSquareSpawner] RMA built. Fallback idx=0. Registry count={_matIndexById.Count}");
        foreach (var kv in _matIndexById)
            Debug.Log($"[MiniSquareSpawner] Registry id '{kv.Key}' -> matIndex {kv.Value}");

        // Archetype (include health & group)
        _archetype = _em.CreateArchetype(
            typeof(LocalTransform),
            typeof(LocalToWorld),
            typeof(Agent),
            typeof(UnitHealth),
            typeof(AttackGroup),
            typeof(URPMaterialPropertyBaseColor)
        );

        // ProjectileDefaults singleton
        var q = _em.CreateEntityQuery(ComponentType.ReadOnly<ProjectileDefaults>());
        var pc = playerProjectileColor.linear;
        var ec = enemyProjectileColor.linear;
        var data = new ProjectileDefaults {
            Speed       = projectileSpeed,
            Radius      = projectileRadius,
            Life        = projectileLife,
            PlayerColor = new float4(pc.r, pc.g, pc.b, pc.a),
            EnemyColor  = new float4(ec.r, ec.g, ec.b, ec.a)
        };
        if (q.CalculateEntityCount() == 0)
        {
            var e = _em.CreateEntity(typeof(ProjectileDefaults));
            _em.SetComponentData(e, data);
        }
        else
        {
            var e = q.GetSingletonEntity();
            _em.SetComponentData(e, data);
        }
    }

    // -------------------------
    // Public spawns
    // -------------------------

    // Basic convenience: Ground + HP=1
    public void SpawnMini(float3 pos, Faction faction, Color color)
    {
        float speed = (faction == Faction.Player) ? playerSpeed : enemySpeed;
        SpawnMiniEntityCustom(pos, faction, color, speed, detectRange, radius, 1, GroupKind.Ground, null, null);
    }

    // Convenience with overrides (defaults to Ground)
    public void SpawnMiniCustom(float3 pos, Faction faction, Color color,
                                float speed, float detectRange, float radius,
                                int hp, float? scaleOverride = null, string spriteId = null)
    {
        SpawnMiniEntityCustom(pos, faction, color, speed, detectRange, radius, hp, GroupKind.Ground, scaleOverride, spriteId);
    }

    // Core spawner (returns the created entity)
    public Entity SpawnMiniEntityCustom(float3 pos, Faction faction, Color color,
                                        float speed, float detectRange, float radius,
                                        int hp, GroupKind group,
                                        float? scaleOverride, string spriteId)
    {
        var e = _em.CreateEntity(_archetype);

        pos.z = -0.02f;
        float scale = scaleOverride ?? miniScale;

        _em.SetComponentData(e, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale));
        _em.SetComponentData(e, new Agent { Faction = faction, MoveSpeed = speed, DetectRange = detectRange, Radius = radius });
        _em.SetComponentData(e, new UnitHealth { Value = hp, Max = hp });
        _em.SetComponentData(e, new AttackGroup { Value = group });

        var lin = color.linear;
        _em.SetComponentData(e, new URPMaterialPropertyBaseColor { Value = new float4(lin.r, lin.g, lin.b, lin.a) });

        // choose material index based on spriteId (fallback = 0)
        var mmi = _mmiFallback;
        if (!string.IsNullOrEmpty(spriteId))
        {
            if (_matIndexById != null && _matIndexById.TryGetValue(spriteId, out int matIndex))
            {
                mmi = MaterialMeshInfo.FromRenderMeshArrayIndices(matIndex, 0);
            }
            else
            {
                Debug.LogWarning($"[MiniSquareSpawner] spriteId '{spriteId}' not found in registry; using fallback material.");
            }
            //mmi = MaterialMeshInfo.FromRenderMeshArrayIndices(1, 0);
        }

        var desc = new RenderMeshDescription(ShadowCastingMode.Off, receiveShadows: false);
        Debug.Log($"[MiniSquareSpawner] Spawning spriteId='{spriteId ?? "<null>"}' → matIndex={(string.IsNullOrEmpty(spriteId) || _matIndexById == null || !_matIndexById.TryGetValue(spriteId, out var idx) ? 0 : idx)}");
        RenderMeshUtility.AddComponents(e, _em, desc, _rma, mmi);
        return e;
    }

    // PLAYER shooter
    public Entity SpawnPlayerShooter(float3 pos, Color tint,
                                     float rangeFromConfig, float cooldownFromConfig,
                                     float speedFromConfig, float detectRangeFromConfig, float radiusFromConfig,
                                     int hpFromConfig, int damageFromConfig,
                                     GroupKind group, int targetMask,
                                     float? scaleOverride = null, string spriteId = null)
    {
        var e = SpawnMiniEntityCustom(pos, Faction.Player, tint,
            speedFromConfig, detectRangeFromConfig, radiusFromConfig, hpFromConfig, group, scaleOverride, spriteId);

        _em.AddComponentData(e, new Shooter {
            Range        = rangeFromConfig,
            FireCooldown = cooldownFromConfig,
            Damage       = damageFromConfig,
            TargetMask   = targetMask
        });
        _em.AddComponentData(e, new ShooterCooldown { TimeLeft = 0f });
        return e;
    }

    // ENEMY shooter
    public Entity SpawnEnemyShooter(float3 pos, Color tint,
                                    float range, float cooldown,
                                    float speed, float detectRange, float radius,
                                    int hp, int damage,
                                    GroupKind group, int targetMask,
                                    float? scaleOverride = null, string spriteId = null)
    {
        var e = SpawnMiniEntityCustom(pos, Faction.Enemy, tint,
            speed, detectRange, radius, hp, group, scaleOverride, spriteId);

        _em.AddComponentData(e, new Shooter {
            Range        = range,
            FireCooldown = cooldown,
            Damage       = damage,
            TargetMask   = targetMask
        });
        _em.AddComponentData(e, new ShooterCooldown { TimeLeft = 0f });
        return e;
    }

    // Spawns a projectile entity (used by building shooters etc.)
    public Entity SpawnProjectileFromBuilding(float3 pos, float3 dir, Faction faction, float damage, float projectileScale = 0.12f)
    {
        var em = _em;

        var q = em.CreateEntityQuery(ComponentType.ReadOnly<ProjectileDefaults>());
        var defs = em.GetComponentData<ProjectileDefaults>(q.GetSingletonEntity());

        var p = em.CreateEntity();
        em.AddComponentData(p, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, projectileScale));
        em.AddComponentData(p, new Projectile { Faction = faction, Radius = defs.Radius, Damage = damage });
        em.AddComponentData(p, new Velocity   { Value   = dir * defs.Speed });
        em.AddComponentData(p, new Lifetime   { Seconds = defs.Life });

        var col = (faction == Faction.Player) ? defs.PlayerColor : defs.EnemyColor;
        em.AddComponentData(p, new URPMaterialPropertyBaseColor { Value = col });

        var desc = new RenderMeshDescription(ShadowCastingMode.Off, receiveShadows: false);
        RenderMeshUtility.AddComponents(p, em, desc, _rma, _mmiFallback);
        return p;
    }

    // Helpers
    private static Mesh CreateUnitQuad()
    {
        var m = new Mesh { name = "DOTS_Quad" };
        m.vertices  = new Vector3[] { new(-0.5f,-0.5f,0), new(0.5f,-0.5f,0), new(0.5f,0.5f,0), new(-0.5f,0.5f,0) };
        m.uv        = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
        m.triangles = new int[] { 0,1,2, 0,2,3 };
        m.RecalculateBounds();
        return m;
    }
}
