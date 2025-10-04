using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildingShooterBridge : MonoBehaviour
{
    [Header("Refs")]
    public BuildingStatsMono stats;          // auto-fetched if null

    [Header("Shooting")]
    public float shootRange = 7.5f;          // meters; overridden from stats if >0 there
    public int   damage     = 1;             // overridden from stats if >0 there
    [Tooltip("Allowed target groups bitmask (bits: Ground=1<<0, Air=1<<1, Orbital=1<<2). Overridden from stats if >0 there.")]
    public int   targetMask = (1 << (int)GroupKind.Ground) | (1 << (int)GroupKind.Air) | (1 << (int)GroupKind.Orbital);

    [Header("Timing")]
    public bool useUnscaledTime = false;

    // internals
    private EntityManager _em;
    private EntityQuery   _enemyQuery;
    private float         _timer;

    void Awake()
    {
        if (stats == null) stats = GetComponent<BuildingStatsMono>();
        if (stats == null)
        {
            Debug.LogError("[BuildingShooterBridge] Missing BuildingStatsMono.", this);
            enabled = false; return;
        }

        // Override public defaults with values baked by BuildingConfigApplier (if present)
        if (stats.shootRange > 0f) shootRange = stats.shootRange;
        if (stats.damage     > 0)  damage     = stats.damage;
        if (stats.targetMask > 0)  targetMask = stats.targetMask;

        _em = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Candidate targets: all minis (Agents) that have a transform, health, and a group
        _enemyQuery = _em.CreateEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<Agent>(),
            ComponentType.ReadOnly<AttackGroup>(),
            ComponentType.ReadOnly<UnitHealth>()
        );

        _timer = 0f;
    }

    void Update()
    {
        if (MiniSquareSpawner.Instance == null) return;
        // 👇 add this line so dead buildings never shoot
        if (stats != null && stats.currentHP <= 0) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _timer += dt;

        // Fire when our building cooldown elapses
        if (_timer < stats.cooldown) return;

        // Find nearest legal enemy within range
        float3 myPos = transform.position; myPos.z = -0.02f;
        int myFaction = (int)stats.faction;

        using var ents  = _enemyQuery.ToEntityArray(Allocator.Temp);
        using var xfs   = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using var agts  = _enemyQuery.ToComponentDataArray<Agent>(Allocator.Temp);
        using var grps  = _enemyQuery.ToComponentDataArray<AttackGroup>(Allocator.Temp);

        Entity best = Entity.Null;
        float bestD2 = float.MaxValue;
        float range2 = shootRange * shootRange;

        for (int i = 0; i < ents.Length; i++)
        {
            // Opposite faction only
            if ((int)agts[i].Faction == myFaction) continue;

            // Group mask check
            int bit = 1 << (int)grps[i].Value;
            if ((targetMask & bit) == 0) continue;

            float2 d = (float2)(myPos.xy - xfs[i].Position.xy);
            float d2 = math.lengthsq(d);
            if (d2 <= range2 && d2 < bestD2)
            {
                bestD2 = d2;
                best   = ents[i];
            }
        }

        if (best == Entity.Null) return;

        // Shoot at snapshot of target position
        float3 tpos = _em.GetComponentData<LocalTransform>(best).Position;
        float3 dir  = tpos - myPos;
        float len   = math.length(dir);
        if (len <= 1e-5f) return;
        dir /= len;

        MiniSquareSpawner.Instance.SpawnProjectileFromBuilding(myPos, dir, stats.faction, damage);

        _timer = 0f; // reset attack timer
    }
}
