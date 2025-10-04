using UnityEngine;

[DisallowMultipleComponent]
public class BuildingConfigApplier : MonoBehaviour
{
    [Header("JSON")]
    public TextAsset playerBuildingsJson; // PlayerBuildingsConfig.json
    public TextAsset enemyBuildingsJson;  // EnemyBuildingsConfig.json

    [Header("Scene References")]
    public GameObject playerTop;
    public GameObject playerMiddle;
    public GameObject playerBottom;
    public GameObject enemyTop;
    public GameObject enemyMiddle;
    public GameObject enemyBottom;

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

        ApplyBoxTo(FindBox(set, "Top"),    top, faction);
        ApplyBoxTo(FindBox(set, "Middle"), mid, faction);
        ApplyBoxTo(FindBox(set, "Bottom"), bot, faction);
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

    private void ApplyBoxTo(BuildingConfigBox cfg, GameObject go, Faction faction)
    {
        if (cfg == null || go == null) return;

        var stats = go.GetComponent<BuildingStatsMono>();
        if (stats == null) stats = go.AddComponent<BuildingStatsMono>();

        var group = SpawnConfigLoader.ParseGroup(cfg.group);
        int mask  = MaskFromStrings(cfg.canAttack);

        stats.ApplyFromConfig(cfg.hp, cfg.cooldown, group, faction,
                              shootRangeOpt: cfg.shootRange,
                              damageOpt:     cfg.damage,
                              targetMaskOpt: mask);

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

        Debug.Log($"[BuildingConfigApplier] {faction} {cfg.id} -> HP={cfg.hp}, CD={cfg.cooldown}, Group={cfg.group}", go);
    }
}
