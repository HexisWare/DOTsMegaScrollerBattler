using UnityEngine;
using Unity.Mathematics;

public class TimedEnemySpawner : MonoBehaviour
{
    public float interval = 1.2f;
    public Color enemyColor = Color.red;

    [Header("Enemy Shooter Stats")]
    public float enemyCooldown = 1.0f;
    public float enemyRange    = 0.8f;
    public float enemySpeed    = 6f;
    public float enemyDetect   = 0.6f;
    public float enemyRadius   = 0.15f;
    public float enemyScale    = 0.2f;
    public int   enemyHP       = 3;
    public int   enemyDamage   = 1;

    [Header("Enemy Group")]
    public GroupKind group = GroupKind.Ground; // set Top=Orbital, Middle=Air, Bottom=Ground

    [Header("Enemy Can Attack")]
    public bool canAttackGround = true;
    public bool canAttackAir    = false;
    public bool canAttackOrbital= false;

    float _t;

    void Awake()
    {
        // If you want range to default to detect, do it here (instance-safe)
        if (enemyRange <= 0f) enemyRange = enemyDetect;
    }

    int BuildMask()
    {
        int m = 0;
        if (canAttackGround)  m |= 1 << (int)GroupKind.Ground;
        if (canAttackAir)     m |= 1 << (int)GroupKind.Air;
        if (canAttackOrbital) m |= 1 << (int)GroupKind.Orbital;
        return m;
    }

    void Update()
    {
        if (MiniSquareSpawner.Instance == null) return;

        _t += Time.deltaTime;
        if (_t < interval) return;
        _t = 0f;

        int targetMask = BuildMask();
        var p = transform.position;

        MiniSquareSpawner.Instance.SpawnEnemyShooter(
            new float3(p.x, p.y, -0.02f),
            enemyColor,
            range:        enemyRange,
            cooldown:     enemyCooldown,
            speed:        enemySpeed,
            detectRange:  enemyDetect,
            radius:       enemyRadius,
            hp:           enemyHP,
            damage:       enemyDamage,
            group:        group,
            targetMask:   targetMask,
            scaleOverride: enemyScale
        );
    }
}
