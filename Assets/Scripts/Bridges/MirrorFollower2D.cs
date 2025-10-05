using UnityEngine;

public class MirrorFollower2D : MonoBehaviour
{
    [Header("Source (Left Box)")]
    public Transform source;       // assign the corresponding LEFT box here in Inspector

    [Header("Mirror Settings")]
    public float mirrorAxisX = 0f; // mirror across x = 0 (the Y axis)
    public MoveMode2D moveMode = MoveMode2D.Free2D; // Bottom=HorizontalOnly, Mid/Top=Free2D
    [Tooltip("1 = snap each frame, <1 = interpolate (0..1).")]
    public float followLerp = 1f;

    private float _lockedY; // for HorizontalOnly
    private BuildingStatsMono _myStats;

    void Awake()
    {
        _lockedY = transform.position.y;
        _myStats = GetComponent<BuildingStatsMono>();
    }

    void LateUpdate()
    {
        if (source == null) return;

        // ❌ If THIS (mirrored) building is dead, do not move it.
        if (_myStats != null && _myStats.currentHP <= 0)
            return;

        Vector3 s = source.position;
        float mirroredX = mirrorAxisX + (mirrorAxisX - s.x); // 2*axis - x

        Vector3 target = (moveMode == MoveMode2D.HorizontalOnly)
            ? new Vector3(mirroredX, _lockedY, transform.position.z)
            : new Vector3(mirroredX, s.y,       transform.position.z);

        if (followLerp >= 1f || Time.deltaTime <= 0f)
            transform.position = target;
        else
            transform.position = Vector3.Lerp(transform.position, target, Mathf.Clamp01(followLerp));
    }
}
