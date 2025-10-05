using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Visuals/Sprite Material Registry", fileName = "UnitSpriteMaterialRegistry")]
public class SpriteMaterialRegistry : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string id;         // e.g. "top_striker"
        public Material material; // URP/Unlit with your texture
    }

    public Material defaultUnitMaterial; // optional fallback
    public Entry[] entries;

    private Dictionary<string, Material> _map;

    public void InitIfNeeded()
    {
        if (_map != null) return;
        _map = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        if (entries == null) return;
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.id) && e.material != null)
                _map[e.id] = e.material;
    }

    public Material GetMaterial(string id)
    {
        InitIfNeeded();
        if (!string.IsNullOrEmpty(id) && _map.TryGetValue(id, out var m)) return m;
        return defaultUnitMaterial;
    }

    public IEnumerable<Material> EnumerateAllMaterials()
    {
        InitIfNeeded();
        if (defaultUnitMaterial != null) yield return defaultUnitMaterial;
        foreach (var kv in _map) if (kv.Value != null) yield return kv.Value;
    }
}
