using UnityEngine;

public class MirrorFollower2D : MonoBehaviour
{
    [Header("Source (Left Box)")]
    public Transform source;       // assign the corresponding LEFT box here in Inspector

    [Header("Mirror Settings")]
    public float mirrorAxisX = 0f; // mirror across x = 0 (the Y axis)
    public MoveMode2D moveMode = MoveMode2D.Free2D; // match row: Bottom=HorizontalOnly, Mid/Top=Free2D
    public float followLerp = 1f;  // 1 = snap, <1 = smooth

    float _lockedY; // for HorizontalOnly

    void Awake()
    {
        _lockedY = transform.position.y;
    }

    void LateUpdate()
    {
        if (source == null) return;

        Vector3 s = source.position;
        float mirroredX = mirrorAxisX + (mirrorAxisX - s.x); // 2*axis - x

        Vector3 target;
        if (moveMode == MoveMode2D.HorizontalOnly)
            target = new Vector3(mirroredX, _lockedY, 0f);
        else
            target = new Vector3(mirroredX, s.y, 0f);

        if (followLerp >= 1f || Time.deltaTime <= 0f)
            transform.position = target;
        else
            transform.position = Vector3.Lerp(transform.position, target, Mathf.Clamp01(followLerp));
    }
}
