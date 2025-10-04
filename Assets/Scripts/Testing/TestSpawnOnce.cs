using UnityEngine;
using Unity.Mathematics;

public class TestSpawnOnce : MonoBehaviour
{
    void Start()
    {
        if (MiniSquareSpawner.Instance == null) { Debug.LogError("Spawner missing"); return; }
        MiniSquareSpawner.Instance.SpawnMini(new float3(-2, 0, 0), Faction.Player, Color.cyan);
        MiniSquareSpawner.Instance.SpawnMini(new float3( 2, 0, 0), Faction.Enemy,  Color.red);
        Debug.Log("[TestSpawnOnce] Spawned two test minis");
    }
}
