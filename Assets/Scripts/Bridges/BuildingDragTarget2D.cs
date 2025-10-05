using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BuildingDragTarget2D : MonoBehaviour
{
    [Header("References")]
    public Camera worldCamera;

    [Header("Line Rendering")]
    [Tooltip("Copy the building's sorting layer & order so the line draws in the same layer.")]
    public bool matchBuildingSorting = true;
    [Tooltip("Used only if no SpriteRenderer/MeshRenderer found on the building.")]
    public string fallbackSortingLayerName = "Default";
    [Tooltip("Rendering order if no renderer was found on the building.")]
    public int fallbackSortingOrder = 0;

    [Header("Visual Style")]
    public float lineThickness = 0.06f;
    public Color lineColor     = new Color(1f, 0.2f, 0.2f, 0.95f);
    [Tooltip("Z added to the line so it doesn't z-fight with the building sprite.")]
    public float zOffset       = 0.001f;

    private BuildingStatsMono _stats;
    private bool _dragging;

    private LineRenderer _line;
    private int _sortingLayerID;
    private int _sortingOrder;

    void Awake()
    {
        _stats = GetComponent<BuildingStatsMono>();
        if (worldCamera == null) worldCamera = Camera.main;

        // Determine sorting (try SpriteRenderer first, then MeshRenderer, else fallback)
        _sortingLayerID = SortingLayer.NameToID(fallbackSortingLayerName);
        _sortingOrder   = fallbackSortingOrder;

        if (matchBuildingSorting)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                _sortingLayerID = sr.sortingLayerID;
                _sortingOrder   = sr.sortingOrder;
            }
            else
            {
                var mr = GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    _sortingLayerID = mr.sortingLayerID;
                    _sortingOrder   = mr.sortingOrder;
                }
            }
        }

        // Build the line
        _line = new GameObject("BuildingMoveLine").AddComponent<LineRenderer>();
        _line.sortingLayerID    = _sortingLayerID;
        _line.sortingOrder      = _sortingOrder;
        _line.alignment         = LineAlignment.TransformZ; // world space
        _line.numCapVertices    = 0;
        _line.numCornerVertices = 0;
        _line.textureMode       = LineTextureMode.Stretch;
        _line.widthMultiplier   = lineThickness;
        _line.positionCount     = 2;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = lineColor };
        _line.material = mat;

        _line.enabled = false;
    }

    void OnDestroy()
    {
        if (_line) Destroy(_line.gameObject);
    }

    void Update()
    {
        // While dragging, OnMouseDrag drives the positions. Don't fight it here.
        if (_dragging) return;

        // Hide if no stats, dead, or no active destination
        if (_stats == null || _stats.currentHP <= 0 || !_stats.hasTarget)
        {
            HideLine();
            return;
        }

        // Keep the line visible until we arrive: start=building pos, end=saved target
        var start = new Vector3(transform.position.x, transform.position.y, transform.position.z + zOffset);
        var end   = new Vector3(_stats.targetPos.x,  _stats.targetPos.y,  transform.position.z + zOffset);

        _line.enabled = true;
        _line.sortingLayerID = _sortingLayerID;
        _line.sortingOrder   = _sortingOrder;
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
    }

    void OnMouseDown()
    {
        if (!CanDrag()) return;
        _dragging = true;
        _line.enabled = true; // show on first drag instantly
    }

    void OnMouseDrag()
    {
        if (!_dragging || !CanDrag()) return;

        var target = WorldMouseToTarget();
        if (_stats.movementKind == MovementKind.HorizontalOnly)
            target.y = transform.position.y;

        var start = new Vector3(transform.position.x, transform.position.y, transform.position.z + zOffset);
        var end   = new Vector3(target.x,              target.y,              transform.position.z + zOffset);

        _line.enabled = true;
        _line.sortingLayerID = _sortingLayerID;
        _line.sortingOrder   = _sortingOrder;
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
    }

    void OnMouseUp()
    {
        if (!_dragging || !CanDrag()) return;
        _dragging = false;

        var target = WorldMouseToTarget();
        if (_stats.movementKind == MovementKind.HorizontalOnly)
            target.y = transform.position.y;

        // Commit the move order; line remains visible until arrival
        _stats.targetPos = new Vector3(target.x, target.y, transform.position.z);
        _stats.hasTarget = true;
    }

    // -------- helpers --------
    private bool CanDrag()
    {
        return _stats != null && _stats.faction == Faction.Player && _stats.currentHP > 0;
    }

    private Vector3 WorldMouseToTarget()
    {
        var cam = worldCamera ? worldCamera : Camera.main;
        var wp  = cam.ScreenToWorldPoint(Input.mousePosition);
        return new Vector3(wp.x, wp.y, transform.position.z + zOffset);
    }

    private void HideLine()
    {
        if (_line) _line.enabled = false;
    }
}
