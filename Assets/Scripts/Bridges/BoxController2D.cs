using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(Collider2D))]
public class BoxController2D : MonoBehaviour
{
    [Header("Movement")]
    public MoveMode2D moveMode = MoveMode2D.Free2D;
    public bool useBounds = false;
    public Rect bounds = new Rect(-8, -4.5f, 16, 9); // x,y,width,height in world units

    [Header("Spawning (Player)")]
    public Color spawnColor = Color.cyan;
    [Tooltip("Spawn on Right-Click, or Shift+Left-Click to avoid conflicts with dragging.")]
    public bool allowShiftLeftClickSpawn = true;

    Camera _cam;
    bool _dragging;
    Vector2 _grabOffset;
    float _lockedY;   // for HorizontalOnly

    void Awake()
    {
        _cam = Camera.main;
        _lockedY = transform.position.y;
    }

    void Update()
    {
        if (_cam == null) return;

        // ----- Spawning (player) -----
        bool spawn = Input.GetMouseButtonDown(1) || (allowShiftLeftClickSpawn && Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0));
        if (spawn && MiniSquareSpawner.Instance != null)
        {
            Vector2 p = transform.position;
            MiniSquareSpawner.Instance.SpawnMini(new float3(p.x, p.y, -0.02f), Faction.Player, spawnColor);
        }

        // ----- Drag begin -----
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mp = _cam.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(mp);
            if (hit != null && hit.transform == transform)
            {
                _dragging = true;
                _grabOffset = (Vector2)transform.position - mp;
            }
        }

        // ----- Drag move -----
        if (_dragging && Input.GetMouseButton(0))
        {
            Vector2 mp = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 target = mp + _grabOffset;

            if (moveMode == MoveMode2D.HorizontalOnly)
                target = new Vector2(target.x, _lockedY);

            if (useBounds)
            {
                float minX = bounds.xMin, maxX = bounds.xMax;
                float minY = bounds.yMin, maxY = bounds.yMax;
                target.x = Mathf.Clamp(target.x, minX, maxX);
                target.y = Mathf.Clamp(target.y, minY, maxY);
                if (moveMode == MoveMode2D.HorizontalOnly) target.y = _lockedY;
            }

            transform.position = new Vector3(target.x, target.y, 0f);
        }

        // ----- Drag end -----
        if (Input.GetMouseButtonUp(0))
            _dragging = false;
    }
}
