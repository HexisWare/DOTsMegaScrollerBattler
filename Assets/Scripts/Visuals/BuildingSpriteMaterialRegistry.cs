using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DOTS/Sprites/Building Sprite Material Registry")]
public class BuildingSpriteMaterialRegistry : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string id;       // e.g. "Top", "Middle", "Bottom" (or any custom key)
        public Material material;
    }

    public Entry[] entries;

    private Dictionary<string, Material> _map;

    public void InitIfNeeded()
    {
        if (_map != null) return;
        _map = new Dictionary<string, Material>(entries?.Length ?? 0, System.StringComparer.Ordinal);
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (!string.IsNullOrWhiteSpace(e.id) && e.material != null)
                _map[e.id] = e.material;
        }
    }

    public Material TryGet(string id)
    {
        InitIfNeeded();
        if (string.IsNullOrEmpty(id)) return null;
        return _map != null && _map.TryGetValue(id, out var m) ? m : null;
    }
}
