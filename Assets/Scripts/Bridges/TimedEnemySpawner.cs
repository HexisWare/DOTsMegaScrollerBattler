using UnityEngine;
using Unity.Mathematics;

public class TimedEnemySpawner : MonoBehaviour
{
    public float intervalSeconds = 0.75f;
    public Color color = Color.red;
    float t;

    void Update()
    {
        if (MiniSquareSpawner.Instance == null) return;
        t += Time.deltaTime;
        if (t >= intervalSeconds)
        {
            Debug.Log($"[TimedEnemySpawner] Spawn at {transform.position}");

            t = 0f;
            var p = (Vector2)transform.position;
            MiniSquareSpawner.Instance.SpawnMini(new float3(p.x, p.y, 0), Faction.Enemy, color);
        }
    }
}
