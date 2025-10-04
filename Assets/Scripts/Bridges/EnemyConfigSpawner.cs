// Assets/Scripts/Bridges/EnemyConfigSpawner.cs
using UnityEngine;
using Unity.Mathematics;

public enum Lane { Top, Middle, Bottom }

[DisallowMultipleComponent]
public class EnemyConfigSpawner : MonoBehaviour
{
    [Header("Which lane is this spawner for?")]
    public Lane lane = Lane.Top;

    [Header("Config JSON")]
    [Tooltip("Assign EnemySpawnConfig.json here (ENEMY spawns).")]
    public TextAsset enemyConfigJson;

    [Header("Timing")]
    [Tooltip("If true, uses unscaled time for spawn timing (ignores Time.timeScale).")]
    public bool useUnscaledTime = false;

    [Tooltip("Fallback interval if spawnCooldown is 0/missing in JSON.")]
    public float fallbackInterval = 1.2f;

    // --- internal state ---
    private SpawnSetConfig _cfg;
    private BoxConfig _box;
    private SpawnDef _spawn;       // first enabled spawn in this box
    private float _spawnInterval;  // seconds, from spawn.spawnCooldown or fallback
    private float _timer;

    void Awake()
    {
        if (enemyConfigJson == null)
        {
            Debug.LogError("[EnemyConfigSpawner] No enemyConfigJson assigned.", this);
            enabled = false; return;
        }

        _cfg = SpawnConfigLoader.LoadFromText(enemyConfigJson.text);
        if (_cfg == null || _cfg.boxes == null || _cfg.boxes.Length == 0)
        {
            Debug.LogError("[EnemyConfigSpawner] Parsed config is empty/invalid.", this);
            enabled = false; return;
        }

        string wantedId = lane.ToString(); // "Top", "Middle", "Bottom"
        _box = System.Array.Find(_cfg.boxes,
            b => string.Equals(b.id, wantedId, System.StringComparison.OrdinalIgnoreCase));

        if (_box == null)
        {
            Debug.LogError($"[EnemyConfigSpawner] Box '{wantedId}' not found in JSON.", this);
            enabled = false; return;
        }

        if (_box.spawns == null || _box.spawns.Length == 0)
        {
            Debug.LogError($"[EnemyConfigSpawner] Box '{wantedId}' has no spawns.", this);
            enabled = false; return;
        }

        _spawn = System.Array.Find(_box.spawns, s => s.enabled);
        if (_spawn == null)
        {
            Debug.LogWarning($"[EnemyConfigSpawner] Box '{wantedId}' has no ENABLED spawns.", this);
            enabled = false; return;
        }

        _spawnInterval = (_spawn.spawnCooldown > 0f) ? _spawn.spawnCooldown : fallbackInterval;
        _timer = 0f;

        Debug.Log(
            $"[EnemyConfigSpawner] Lane={lane} Box='{_box.id}' Spawn='{_spawn.id}'  spawnCooldown={_spawn.spawnCooldown:F2}s  intervalUsed={_spawnInterval:F2}s  attackCooldown={_spawn.cooldown:F2}s",
            this
        );
    }

    void Update()
    {
        if (!enabled || MiniSquareSpawner.Instance == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _timer += dt;

        if (_timer < _spawnInterval) return;
        _timer -= _spawnInterval; // keep remainder so long frames don't drift

        // Spawn one enemy unit according to JSON
        var pos   = transform.position;
        var color = SpawnConfigLoader.ColorFromHtml(_spawn.color, Color.red);
        var group = SpawnConfigLoader.ParseGroup(_box.group);
        int mask  = SpawnConfigLoader.MaskFromStrings(_spawn.canAttack);

        MiniSquareSpawner.Instance.SpawnEnemyShooter(
            new float3(pos.x, pos.y, -0.02f),
            color,
            range:        _spawn.detectRange,                   // firing range
            cooldown:     Mathf.Max(0.01f, _spawn.cooldown),    // ATTACK speed
            speed:        _spawn.speed,
            detectRange:  _spawn.detectRange,
            radius:       _spawn.radius,
            hp:           Mathf.Max(1, _spawn.hp),
            damage:       Mathf.Max(1, _spawn.damage),
            group:        group,
            targetMask:   mask,
            scaleOverride:_spawn.scale
        );

        // Debug each spawn timing if needed:
        // Debug.Log($"[EnemyConfigSpawner] Spawn '{_spawn.id}' @ t={Time.time:F2} (interval {_spawnInterval:F2}s)", this);
    }
}
