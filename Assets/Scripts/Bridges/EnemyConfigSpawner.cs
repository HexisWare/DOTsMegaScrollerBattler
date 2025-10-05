using UnityEngine;
using Unity.Mathematics;

public class EnemyConfigSpawner : MonoBehaviour
{
    public Lane lane = Lane.Top;
    public TextAsset enemyConfigJson;
    public bool useUnscaledTime = false;
    public float fallbackInterval = 1.2f;

    private SpawnSetConfig _cfg;
    private BoxConfig _box;
    private SpawnDef _spawn;
    private float _spawnInterval;
    private float _timer;

    private BuildingStatsMono _stats; // building HP reference
    private string _spriteId;         // <- sprite id from JSON (used by the registry on MiniSquareSpawner)

    void Awake()
    {
        _stats = GetComponent<BuildingStatsMono>(); // on the same right-lane object

        if (enemyConfigJson == null)
        {
            Debug.LogError("[EnemyConfigSpawner] No enemyConfigJson assigned.", this);
            enabled = false;
            return;
        }

        _cfg = SpawnConfigLoader.LoadFromText(enemyConfigJson.text);
        if (_cfg == null || _cfg.boxes == null || _cfg.boxes.Length == 0)
        {
            Debug.LogError("[EnemyConfigSpawner] Parsed config is empty/invalid.", this);
            enabled = false;
            return;
        }

        string wantedId = lane.ToString();
        _box = System.Array.Find(_cfg.boxes, b => string.Equals(b.id, wantedId, System.StringComparison.OrdinalIgnoreCase));
        if (_box == null || _box.spawns == null || _box.spawns.Length == 0)
        {
            Debug.LogError($"[EnemyConfigSpawner] Box '{wantedId}' invalid.", this);
            enabled = false;
            return;
        }

        _spawn = System.Array.Find(_box.spawns, s => s.enabled);
        if (_spawn == null)
        {
            Debug.LogWarning($"[EnemyConfigSpawner] Box '{wantedId}' has no ENABLED spawns.", this);
            enabled = false;
            return;
        }

        _spriteId = _spawn.sprite; // <- carry sprite id through to spawns

        _spawnInterval = (_spawn.spawnCooldown > 0f) ? _spawn.spawnCooldown : fallbackInterval;
        _timer = 0f;

        Debug.Log(
            $"[EnemyConfigSpawner] Lane={lane} Box='{_box.id}' " +
            $"Spawn='{_spawn.id}' sprite='{_spriteId}' " +
            $"spawnCooldown={_spawn.spawnCooldown:F2}s intervalUsed={_spawnInterval:F2}s attackCooldown={_spawn.cooldown:F2}s",
            this
        );
    }

    void Update()
    {
        // Stop entirely if building is dead
        if (_stats != null && _stats.currentHP <= 0)
        {
            enabled = false;
            return;
        }

        if (MiniSquareSpawner.Instance == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _timer += dt;
        if (_timer < _spawnInterval) return;
        _timer -= _spawnInterval;

        var pos   = transform.position;
        var color = SpawnConfigLoader.ColorFromHtml(_spawn.color, Color.red);
        var group = SpawnConfigLoader.ParseGroup(_box.group);
        int mask  = SpawnConfigLoader.MaskFromStrings(_spawn.canAttack);

        // Pass spriteId -> MiniSquareSpawner will look it up in UnitSpriteMaterialRegistry
        MiniSquareSpawner.Instance.SpawnEnemyShooter(
            new float3(pos.x, pos.y, -0.02f),
            tint:             color,
            range:            _spawn.detectRange,
            cooldown:         Mathf.Max(0.01f, _spawn.cooldown),
            speed:            _spawn.speed,
            detectRange:      _spawn.detectRange,
            radius:           _spawn.radius,
            hp:               Mathf.Max(1, _spawn.hp),
            damage:           Mathf.Max(1, _spawn.damage),
            group:            group,
            targetMask:       mask,
            scaleOverride:    _spawn.scale,
            spriteId:         _spriteId    // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        );
    }
}
