using UnityEngine;

[DisallowMultipleComponent]
public class BuildingStatsMono : MonoBehaviour
{
    [Header("Who/What am I")]
    public Faction   faction = Faction.Player;
    public GroupKind group   = GroupKind.Ground;

    [Header("Stats")]
    public int   maxHP    = 200;
    public int   currentHP= 200;

    [Tooltip("Seconds between shots (turret attack rate).")]
    public float cooldown = 2.0f;

    [Header("Shooter (optional overrides from config)")]
    public float shootRange = 7.5f;
    public int   damage     = 1;
    public int   targetMask = (1 << (int)GroupKind.Ground) | (1 << (int)GroupKind.Air) | (1 << (int)GroupKind.Orbital);

    [Header("Movement")]
    public float        moveSpeed = 4f;       // NEW: from config
    public MovementKind movementKind = MovementKind.Free;

    [HideInInspector] public Vector3 targetPos;  // saved destination
    [HideInInspector] public bool    hasTarget;  // true if we should move toward targetPos

    public void ApplyFromConfig(int hp, float cd, GroupKind g, Faction f,
                                float shootRangeOpt = 0f, int damageOpt = 0, int targetMaskOpt = 0,
                                float moveSpeedOpt = 0f)
    {
        maxHP     = Mathf.Max(1, hp);
        currentHP = maxHP;
        cooldown  = Mathf.Max(0.01f, cd);
        group     = g;
        faction   = f;

        if (shootRangeOpt > 0f) shootRange = shootRangeOpt;
        if (damageOpt     > 0)  damage     = damageOpt;
        if (targetMaskOpt > 0)  targetMask = targetMaskOpt;
        if (moveSpeedOpt  > 0f) moveSpeed  = moveSpeedOpt;
    }
}
