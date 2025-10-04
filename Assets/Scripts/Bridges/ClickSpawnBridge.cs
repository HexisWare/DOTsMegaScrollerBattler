// ClickSpawnBridge2D.cs
using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(Collider2D))]
public class ClickSpawnBridge2D : MonoBehaviour
{
    public Color color = Color.cyan;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (MiniSquareSpawner.Instance == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 mp = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 p2 = new Vector2(mp.x, mp.y);

        // Only spawn if THIS exact object was clicked
        var hit = Physics2D.OverlapPoint(p2);
        if (hit != null && hit.transform == transform)
        {
            Debug.Log($"[ClickSpawnBridge] Click at {transform.position}");
            var p = (Vector2)transform.position;
            MiniSquareSpawner.Instance.SpawnMini(new float3(p.x, p.y, 0), Faction.Player, color);
        }
    }
}
