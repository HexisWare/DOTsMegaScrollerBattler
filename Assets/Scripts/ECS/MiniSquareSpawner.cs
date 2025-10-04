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
    public Material urpUnlitMaterial;
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
    private MaterialMeshInfo _mmi;
    private EntityArchetype _archetype;

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

        // URP/Unlit material
        if (urpUnlitMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) { Debug.LogError("URP/Unlit shader not found."); enabled = false; return; }
            urpUnlitMaterial = new Material(shader) { name = "AutoMiniSquareMat" };
            urpUnlitMaterial.color = Color.white;
        }

        // Mesh + render descriptors
        _mesh = CreateUnitQuad();
        _rma  = new RenderMeshArray(new[] { urpUnlitMaterial }, new[] { _mesh });
        _mmi  = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

        // Archetype (include AttackGroup & UnitHealth from the start)
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
    // Public spawns (fixed signatures)
    // -------------------------

    // Basic convenience: defaults to Ground group & HP=1
    public void SpawnMini(float3 pos, Faction faction, Color color)
    {
        float speed = (faction == Faction.Player) ? playerSpeed : enemySpeed;
        SpawnMiniEntityCustom(pos, faction, color, speed, detectRange, radius, 1, GroupKind.Ground, null);
    }

    // Convenience with overrides (kept your old call pattern):
    // NOTE: no 'group' arg here; default to Ground for compatibility
    public void SpawnMiniCustom(float3 pos, Faction faction, Color color,
                                float speed, float detectRange, float radius,
                                int hp, float? scaleOverride = null)
    {
        SpawnMiniEntityCustom(pos, faction, color, speed, detectRange, radius, hp, GroupKind.Ground, scaleOverride);
    }

    // Core spawner that RETURNS the created entity (now requires GroupKind)
    public Entity SpawnMiniEntityCustom(float3 pos, Faction faction, Color color,
                                        float speed, float detectRange, float radius,
                                        int hp, GroupKind group, float? scaleOverride = null)
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

        var desc = new RenderMeshDescription(ShadowCastingMode.Off, receiveShadows: false);
        RenderMeshUtility.AddComponents(e, _em, desc, _rma, _mmi);
        return e;
    }

    // PLAYER shooter: now requires group + targetMask
    public Entity SpawnPlayerShooter(float3 pos, Color tint,
                                     float rangeFromConfig, float cooldownFromConfig,
                                     float speedFromConfig, float detectRangeFromConfig, float radiusFromConfig,
                                     int hpFromConfig, int damageFromConfig,
                                     GroupKind group, int targetMask,
                                     float? scaleOverride = null)
    {
        var e = SpawnMiniEntityCustom(pos, Faction.Player, tint,
            speedFromConfig, detectRangeFromConfig, radiusFromConfig, hpFromConfig, group, scaleOverride);

        _em.AddComponentData(e, new Shooter {
            Range        = rangeFromConfig,
            FireCooldown = cooldownFromConfig,
            Damage       = damageFromConfig,
            TargetMask   = targetMask
        });
        _em.AddComponentData(e, new ShooterCooldown { TimeLeft = 0f });
        return e;
    }

    // ENEMY shooter: requires group + targetMask
    public Entity SpawnEnemyShooter(float3 pos, Color tint,
                                    float range, float cooldown,
                                    float speed, float detectRange, float radius,
                                    int hp, int damage,
                                    GroupKind group, int targetMask,
                                    float? scaleOverride = null)
    {
        var e = SpawnMiniEntityCustom(pos, Faction.Enemy, tint,
            speed, detectRange, radius, hp, group, scaleOverride);

        _em.AddComponentData(e, new Shooter {
            Range        = range,
            FireCooldown = cooldown,
            Damage       = damage,
            TargetMask   = targetMask
        });
        _em.AddComponentData(e, new ShooterCooldown { TimeLeft = 0f });
        return e;
    }

    // -------------------------
    // Helpers
    // -------------------------
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
