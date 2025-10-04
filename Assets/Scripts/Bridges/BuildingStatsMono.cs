using UnityEngine;

[DisallowMultipleComponent]
public class BuildingStatsMono : MonoBehaviour
{
    [Header("Who/What am I")]
    public Faction   faction = Faction.Player;   // set by loader
    public GroupKind group   = GroupKind.Ground; // set by loader

    [Header("Stats")]
    public int   maxHP    = 200;
    public int   currentHP= 200;

    [Tooltip("Seconds between shots (turret attack rate).")]
    public float cooldown = 2.0f;

    [Header("Shooter (optional overrides from config)")]
    public float shootRange = 7.5f;
    public int   damage     = 1;
    public int   targetMask = (1 << (int)GroupKind.Ground) | (1 << (int)GroupKind.Air) | (1 << (int)GroupKind.Orbital);

    public void ApplyFromConfig(int hp, float cd, GroupKind g, Faction f,
                                float shootRangeOpt = 0f, int damageOpt = 0, int targetMaskOpt = 0)
    {
        maxHP     = Mathf.Max(1, hp);
        currentHP = maxHP;
        cooldown  = Mathf.Max(0.01f, cd);
        group     = g;
        faction   = f;

        if (shootRangeOpt > 0f) shootRange = shootRangeOpt;
        if (damageOpt     > 0)  damage     = damageOpt;
        if (targetMaskOpt > 0)  targetMask = targetMaskOpt;
    }
}
