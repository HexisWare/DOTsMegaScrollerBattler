using System;
using UnityEngine;

[Serializable] public class SpawnSetConfig { public BoxConfig[] boxes; }

[Serializable] public class BoxConfig
{
    public string id;           // "Top" | "Middle" | "Bottom"
    public string group;        // "Orbital" | "Air" | "Ground"
    public SpawnDef[] spawns;
}

[Serializable] public class SpawnDef
{
    public string id;
    public string label;
    public bool   enabled;

    public string color;
    public float  speed;
    public float  detectRange;   // used for Shooter.Range AND Agent.DetectRange
    public float  radius;
    public float  scale;

    public float  cooldown;      // ATTACK speed (seconds between shots)
    public float  spawnCooldown; // SPAWN cooldown (UI button disable time / enemy spawn interval)

    public int    hp;
    public int    damage;

    public string[] canAttack;   // e.g. ["Ground","Air"]

    public string sprite;
}

public static class SpawnConfigLoader
{
    public static SpawnSetConfig LoadFromText(string json) =>
        JsonUtility.FromJson<SpawnSetConfig>(json);

    public static Color ColorFromHtml(string html, Color fallback) =>
        (!string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out var c)) ? c : fallback;

    public static GroupKind ParseGroup(string s)
    {
        if (string.IsNullOrEmpty(s)) return GroupKind.Ground;
        s = s.Trim().ToLowerInvariant();
        return s switch
        {
            "air"     => GroupKind.Air,
            "orbital" => GroupKind.Orbital,
            _         => GroupKind.Ground
        };
    }

    public static int MaskFromStrings(string[] arr)
    {
        if (arr == null || arr.Length == 0) return 0;
        int m = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            var g = ParseGroup(arr[i]);
            m |= 1 << (int)g;
        }
        return m;
    }
}
