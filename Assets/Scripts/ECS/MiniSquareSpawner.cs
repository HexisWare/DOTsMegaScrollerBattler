using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;
using UnityEngine.Rendering;  // for ShadowCastingMode, MotionVectorGenerationMode

public class MiniSquareSpawner : MonoBehaviour
{
    public static MiniSquareSpawner Instance;

    [Header("Visuals")]
    public Material urpUnlitMaterial;  // assign MiniSquareMat in Inspector
    public float miniScale = 0.2f;

    [Header("Agent Defaults")]
    public float playerSpeed = 6f;
    public float enemySpeed  = 6f;
    public float detectRange = 0.5f;
    public float radius      = 0.15f;

    private EntityManager _em;
    private Mesh _mesh;
    private RenderMeshArray _rma;
    private MaterialMeshInfo _mmi;
    private EntityArchetype _archetype;

    void Awake()
    {
        Instance = this;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        Debug.Log("[MiniSquareSpawner] Awake: creating DOTS rendering setup");

        // Ensure GridParams singleton (safer check)
        if (_em.CreateEntityQuery(ComponentType.ReadOnly<GridParams>()).CalculateEntityCount() == 0)
        {
            var cfg = _em.CreateEntity(typeof(GridParams));
            _em.SetComponentData(cfg, new GridParams { CellSize = 0.5f });
        }

        // Ensure we have a valid URP Unlit material
        if (urpUnlitMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("URP Unlit shader not found. Assign a URP material on MiniSquareSpawner.");
                enabled = false; // bail so Instance != null doesn't hide the problem
                return;
            }
            urpUnlitMaterial = new Material(shader) { name = "AutoMiniSquareMat" };
            urpUnlitMaterial.color = Color.white;
            Debug.LogWarning("No material assigned; created a default URP Unlit material.");
        }

        _mesh = CreateUnitQuad();
        _rma  = new RenderMeshArray(new[] { urpUnlitMaterial }, new[] { _mesh });
        _mmi  = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

        _archetype = _em.CreateArchetype(
            typeof(LocalTransform),
            typeof(LocalToWorld),                  // <- add this so transforms update immediately
            typeof(Agent),
            typeof(URPMaterialPropertyBaseColor)   // color override per entity
        );
    }


    public void SpawnMini(float3 pos, Faction faction, Color color)
    {
        var e = _em.CreateEntity(_archetype);

        // Transform + gameplay
        pos.z = -0.02f;
        _em.SetComponentData(e, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, miniScale));
        var speed = (faction == Faction.Player) ? playerSpeed : enemySpeed;
        _em.SetComponentData(e, new Agent {
            Faction = faction,
            MoveSpeed = speed,
            DetectRange = detectRange,
            Radius = radius
        });

        // Color (set .Value, no ctor)
        var lin = color.linear;
        _em.SetComponentData(e, new URPMaterialPropertyBaseColor {
            Value = new float4(lin.r, lin.g, lin.b, lin.a)
        });

        // ✅ Let Entities Graphics add all needed render components (bounds, indices, etc.)
        var desc = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.Off,
            receiveShadows: false
        );
        RenderMeshUtility.AddComponents(e, _em, desc, _rma, _mmi);
    }

    private static Mesh CreateUnitQuad()
    {
        var m = new Mesh { name = "DOTS_Quad" };
        m.vertices = new Vector3[] {
            new(-0.5f,-0.5f,0), new(0.5f,-0.5f,0), new(0.5f,0.5f,0), new(-0.5f,0.5f,0)
        };
        m.uv = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
        m.triangles = new int[] { 0,1,2, 0,2,3 };
        m.RecalculateBounds();
        return m;
    }
}
