// Assets/Scripts/Bridges/BuildingAutoMover.cs
using UnityEngine;

[DisallowMultipleComponent]
public class BuildingAutoMover : MonoBehaviour
{
    public float arriveEpsilon = 0.02f;

    private BuildingStatsMono _stats;

    void Awake()
    {
        _stats = GetComponent<BuildingStatsMono>();
    }

    void Update()
    {
        if (_stats == null) return;

        // Dead buildings never move, and forget any pending order
        if (_stats.currentHP <= 0)
        {
            if (_stats.hasTarget) _stats.hasTarget = false;
            return;
        }

        if (!_stats.hasTarget) return;

        Vector3 pos    = transform.position;
        Vector3 target = _stats.targetPos;

        if (_stats.movementKind == MovementKind.HorizontalOnly)
            target.y = pos.y;

        Vector3 delta = target - pos;
        float dist    = delta.magnitude;

        if (dist <= arriveEpsilon)
        {
            transform.position = new Vector3(target.x, target.y, pos.z);
            _stats.hasTarget = false; // arrived
            return;
        }

        float step = _stats.moveSpeed * Time.deltaTime;
        if (step > dist) step = dist;

        transform.position = pos + delta.normalized * step;
    }
}
