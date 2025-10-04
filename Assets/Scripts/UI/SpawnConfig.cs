using System;
using UnityEngine;

[Serializable] public class SpawnSetConfig { public BoxConfig[] boxes; }
[Serializable] public class BoxConfig { public string id; public SpawnDef[] spawns; }
[Serializable] public class SpawnDef
{
    public string id;
    public string label;
    public bool enabled;
    public string color;     // #RRGGBB or #RRGGBBAA
    public float speed;
    public float detectRange;
    public float radius;
    public float scale;
    public float cooldown;
}

public static class SpawnConfigLoader
{
    public static SpawnSetConfig LoadFromText(string json)
    {
        // Unity's JsonUtility needs a root object and public fields (we have that)
        return JsonUtility.FromJson<SpawnSetConfig>(json);
    }

    public static Color ColorFromHtml(string html, Color fallback)
    {
        if (!string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out var c)) return c;
        return fallback;
    }
}
