using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

public class RowSpawnButton : MonoBehaviour
{
    [Header("Which left box does this row control?")]
    public Transform sourceLeftBox;

    [Header("Visuals")]
    public Color spawnColor = Color.cyan;

    Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        if (_btn != null) _btn.onClick.AddListener(SpawnFromSource);
    }

    void SpawnFromSource()
    {
        if (MiniSquareSpawner.Instance == null || sourceLeftBox == null) return;
        var p = sourceLeftBox.position;
        // Nudge Z so minis render above sprites
        MiniSquareSpawner.Instance.SpawnMini(new float3(p.x, p.y, -0.02f), Faction.Player, spawnColor);
    }
}
