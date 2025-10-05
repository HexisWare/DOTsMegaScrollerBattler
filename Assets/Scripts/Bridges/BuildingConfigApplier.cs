using UnityEngine;

[DisallowMultipleComponent]
public class BuildingConfigApplier : MonoBehaviour
{
    [Header("JSON")]
    public TextAsset playerBuildingsJson;
    public TextAsset enemyBuildingsJson;

    [Header("Scene References")]
    public GameObject playerTop;
    public GameObject playerMiddle;
    public GameObject playerBottom;
    public GameObject enemyTop;
    public GameObject enemyMiddle;
    public GameObject enemyBottom;

    [Header("Sprites / Materials")]
    public BuildingSpriteMaterialRegistry buildingSpriteRegistry; // assign asset of this type
    public Material buildingFallbackMaterial;                     // optional fallback (URP/Unlit Transparent)

    [Header("Extras")]
    public bool autoAddShooter = true;
    public bool autoAddECSProxy = true;
    public float defaultBuildingHitRadius = 0.65f;

    void Awake()
    {
        if (playerBuildingsJson != null)
            ApplySet(playerBuildingsJson.text, Faction.Player, playerTop, playerMiddle, playerBottom);
        else
            Debug.LogWarning("[BuildingConfigApplier] No playerBuildingsJson assigned.", this);

        if (enemyBuildingsJson != null)
            ApplySet(enemyBuildingsJson.text, Faction.Enemy, enemyTop, enemyMiddle, enemyBottom);
        else
            Debug.LogWarning("[BuildingConfigApplier] No enemyBuildingsJson assigned.", this);
    }

    private void ApplySet(string json, Faction faction, GameObject top, GameObject mid, GameObject bot)
    {
        var set = JsonUtility.FromJson<BuildingSetConfig>(json);
        if (set == null || set.boxes == null || set.boxes.Length == 0)
        {
            Debug.LogError("[BuildingConfigApplier] Parsed building config is empty/invalid.", this);
            return;
        }

        var topBox = FindBox(set, "Top");
        var midBox = FindBox(set, "Middle");
        var botBox = FindBox(set, "Bottom");

        Debug.Log($"[BuildingConfigApplier] {faction} Apply: Top={(topBox!=null)} Mid={(midBox!=null)} Bot={(botBox!=null)}");

        ApplyBoxTo(topBox, top, faction, MovementKind.Free);
        ApplyBoxTo(midBox,  mid, faction, MovementKind.Free);
        ApplyBoxTo(botBox,  bot, faction, MovementKind.HorizontalOnly);
    }

    private static BuildingConfigBox FindBox(BuildingSetConfig set, string id)
    {
        foreach (var b in set.boxes)
            if (string.Equals(b.id, id, System.StringComparison.OrdinalIgnoreCase))
                return b;
        Debug.LogError($"[BuildingConfigApplier] Box '{id}' not found in building config.");
        return null;
    }

    private static int MaskFromStrings(string[] arr)
    {
        if (arr == null || arr.Length == 0) return 0;
        int m = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            var g = SpawnConfigLoader.ParseGroup(arr[i]);
            m |= 1 << (int)g;
        }
        return m;
    }

    private void ApplyBoxTo(BuildingConfigBox cfg, GameObject go, Faction faction, MovementKind movementKind)
    {
        if (cfg == null || go == null) return;

        // ----- Gameplay hookup -----
        var stats = go.GetComponent<BuildingStatsMono>();
        if (stats == null) stats = go.AddComponent<BuildingStatsMono>();

        var group = SpawnConfigLoader.ParseGroup(cfg.group);
        int mask  = MaskFromStrings(cfg.canAttack);

        stats.ApplyFromConfig(cfg.hp, cfg.cooldown, group, faction,
                              shootRangeOpt: cfg.shootRange,
                              damageOpt:     cfg.damage,
                              targetMaskOpt: mask,
                              moveSpeedOpt:  cfg.moveSpeed);

        stats.movementKind = movementKind;

        if (autoAddShooter)
        {
            var turret = go.GetComponent<BuildingShooterBridge>();
            if (turret == null) turret = go.AddComponent<BuildingShooterBridge>();
            turret.stats = stats;
        }

        if (autoAddECSProxy)
        {
            var proxy = go.GetComponent<BuildingECSProxy>();
            if (proxy == null) proxy = go.AddComponent<BuildingECSProxy>();
            proxy.stats = stats;
            proxy.proxyRadius = defaultBuildingHitRadius;
        }

        if (go.GetComponent<BuildingAutoMover>() == null)
            go.AddComponent<BuildingAutoMover>();

        // ----- Visual hookup (always via child mesh) -----
        var mat = ResolveMaterial(cfg.sprite);
        EnsureChildQuadWithMaterial(go, mat);

        Debug.Log($"[BuildingConfigApplier] {faction} {cfg.id} -> HP={cfg.hp}, CD={cfg.cooldown}, Move={stats.moveSpeed}, Group={cfg.group}, Sprite='{cfg.sprite}'", go);
    }

    private Material ResolveMaterial(string spriteId)
    {
        if (buildingSpriteRegistry != null)
        {
            buildingSpriteRegistry.InitIfNeeded();
            var m = buildingSpriteRegistry.TryGet(spriteId);
            if (m != null)
            {
                Debug.Log($"[BuildingConfigApplier] Resolved sprite '{spriteId}' -> material '{m.name}'");
                return m;
            }
            Debug.LogWarning($"[BuildingConfigApplier] Sprite id '{spriteId}' not found in registry; using fallback.");
        }
        else
        {
            Debug.LogWarning("[BuildingConfigApplier] No buildingSpriteRegistry assigned; using fallback.");
        }

        if (buildingFallbackMaterial == null)
        {
            // Create a safe default so we never NRE
            buildingFallbackMaterial = CreateDefaultFallbackMaterial();
            Debug.LogWarning("[BuildingConfigApplier] No fallback material assigned; created a default URP/Unlit Transparent material.");
        }

        return buildingFallbackMaterial;
    }

    private static Material CreateDefaultFallbackMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var m = new Material(shader) { name = "Auto_Building_Fallback" };
        // Transparent, Both sides, instancing — these properties are shader-variant dependent,
        // but these are reasonable defaults. You can also set these in the Inspector.
        m.SetFloat("_Surface", 1);     // 0=Opaque, 1=Transparent (URP Unlit)
        m.SetFloat("_Cull", 0);        // 0=None (Both)
        m.enableInstancing = true;
        m.color = Color.white;
        return m;
    }

    // ---------- Child-mesh visual path (avoids root component conflicts) ----------
    private const string kVisualChildName = "__BuildingVisual";

    private static Mesh _cachedQuad;
    private static Mesh GetQuad()
    {
        if (_cachedQuad) return _cachedQuad;
        var m = new Mesh { name = "Quad_Building" };
        m.vertices  = new Vector3[] { new(-0.5f,-0.5f,0), new(0.5f,-0.5f,0), new(0.5f,0.5f,0), new(-0.5f,0.5f,0) };
        m.uv        = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
        m.triangles = new int[] { 0,2,1, 0,3,2 }; // faces +Z
        m.RecalculateBounds();
        _cachedQuad = m;
        return _cachedQuad;
    }

    private static GameObject EnsureVisualChild(GameObject root)
    {
        var t = root.transform.Find(kVisualChildName);
        if (t != null) return t.gameObject;

        var child = new GameObject(kVisualChildName);
        var ct = child.transform;
        ct.SetParent(root.transform, worldPositionStays: false);
        ct.localPosition = Vector3.zero;
        ct.localRotation = Quaternion.identity;
        ct.localScale    = Vector3.one;
        return child;
    }

    private void EnsureChildQuadWithMaterial(GameObject root, Material mat)
    {
        if (root == null)
        {
            Debug.LogError("[BuildingConfigApplier] EnsureChildQuadWithMaterial called with NULL root.");
            return;
        }
        if (mat == null)
        {
            Debug.LogWarning($"[BuildingConfigApplier] No material resolved for '{root.name}'. Skipping visual hookup.");
            return;
        }

        // If root has a SpriteRenderer, disable it to avoid visual conflicts
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        var child = EnsureVisualChild(root);

        if (!child.TryGetComponent<MeshFilter>(out var mf) || mf == null)
            mf = child.AddComponent<MeshFilter>();
        var quad = GetQuad();
        mf.sharedMesh = quad;

        if (!child.TryGetComponent<MeshRenderer>(out var mr) || mr == null)
            mr = child.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // Keep child at z=0 relative to parent
        var ct = child.transform;
        ct.localPosition = Vector3.zero;

        Debug.Log($"[BuildingConfigApplier] Visual ready on '{root.name}' → child '{kVisualChildName}' with material '{mat.name}'");
    }

    void OnValidate()
    {
        if (playerTop == null || playerMiddle == null || playerBottom == null ||
            enemyTop == null  || enemyMiddle == null  || enemyBottom == null)
            Debug.LogWarning("[BuildingConfigApplier] One or more building GameObject references are unassigned.", this);

        if (buildingSpriteRegistry == null)
            Debug.LogWarning("[BuildingConfigApplier] buildingSpriteRegistry not assigned.", this);

        if (buildingFallbackMaterial == null)
            Debug.LogWarning("[BuildingConfigApplier] buildingFallbackMaterial not assigned (we'll auto-create one at runtime if needed).", this);
    }
}
