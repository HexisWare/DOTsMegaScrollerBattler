using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CooldownButton : MonoBehaviour
{
    [Tooltip("Optional overlay color while cooling")]
    public Color overlayColor = new Color(0, 0, 0, 0.35f);

    Button _btn;
    TMP_Text _tmp;
    Text _legacy;
    string _baseLabel = "";
    Image _overlay;
    float _remain;
    bool _cooling;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _tmp = GetComponentInChildren<TMP_Text>(true);
        _legacy = (_tmp == null) ? GetComponentInChildren<Text>(true) : null;

        // build a dim overlay (child)
        var rt = (RectTransform)transform;
        var overlayGO = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
        var ort = overlayGO.GetComponent<RectTransform>();
        ort.SetParent(rt, false);
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        _overlay = overlayGO.GetComponent<Image>();
        _overlay.raycastTarget = false;
        _overlay.enabled = false;
        _overlay.color = overlayColor;
    }

    public void SetBaseLabel(string s)
    {
        _baseLabel = s ?? "";
        if (!_cooling) ApplyLabel(_baseLabel);
    }

    public void StartCooldown(float seconds)
    {
        if (seconds <= 0f) return;
        _remain = seconds;
        _cooling = true;
        if (_btn) _btn.interactable = false;
        if (_overlay) _overlay.enabled = true;
    }

    void Update()
    {
        if (!_cooling) return;

        _remain -= Time.unscaledDeltaTime; // UI unaffected by timeScale
        if (_remain <= 0f)
        {
            _cooling = false;
            _remain = 0f;
            if (_btn) _btn.interactable = true;
            if (_overlay) _overlay.enabled = false;
            ApplyLabel(_baseLabel);
        }
        else
        {
            ApplyLabel($"{_baseLabel} ({_remain:0.0}s)");
        }
    }

    void ApplyLabel(string s)
    {
        if (_tmp != null) _tmp.SetText(s);
        else if (_legacy != null) _legacy.text = s;
        gameObject.name = $"Btn_{s}";
    }
}
