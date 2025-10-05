// Assets/Scripts/Bridges/MirrorBuildingTarget.cs
using UnityEngine;

[DisallowMultipleComponent]
public class MirrorBuildingTarget : MonoBehaviour
{
    public Transform sourceLeft;  // assign the matching LEFT (player) building transform
    public float mirrorAxisX = 0f;

    private BuildingStatsMono _my;   // this building (enemy side)
    private BuildingStatsMono _src;  // source (player side)

    void Awake()
    {
        _my  = GetComponent<BuildingStatsMono>();
        if (sourceLeft != null)
            _src = sourceLeft.GetComponent<BuildingStatsMono>();
    }

    void LateUpdate()
    {
        if (_my == null || _src == null) return;

        // Dead mirror never moves (and forget any pending order)
        if (_my.currentHP <= 0)
        {
            if (_my.hasTarget) _my.hasTarget = false;
            return;
        }

        // If the source (player) is dead, do nothing
        if (_src.currentHP <= 0) return;

        // Only mirror when the player building currently has an active target
        if (!_src.hasTarget) return;

        // Mirror across vertical line x = mirrorAxisX
        float dx = _src.targetPos.x - mirrorAxisX;
        float mirroredX = mirrorAxisX - dx;

        Vector3 mirrored = new Vector3(mirroredX, _src.targetPos.y, transform.position.z);

        // Respect lane rule: Bottom = horizontal only
        if (_my.movementKind == MovementKind.HorizontalOnly)
            mirrored.y = transform.position.y;

        _my.targetPos = mirrored;
        _my.hasTarget = true;
    }
}
