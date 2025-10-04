using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CooldownButton : MonoBehaviour
{
    [Header("Single TMP text (required)")]
    [Tooltip("Text that shows the unit ID and, during cooldown, 'ID (seconds)'.")]
    public TMP_Text text;

    [Header("Optional Radial/Linear Fill")]
    [Tooltip("Image set to Type=Filled. FillAmount animates 1→0 during cooldown.")]
    public Image cooldownFill;

    [Header("Behavior")]
    public bool useUnscaledTime = true;
    public bool showTenths = true; // 0.0 vs whole seconds
    [Tooltip("Format used during cooldown. {0}=ID, {1}=time string")]
    public string appendFormat = "{0} ({1})";

    private Button _button;
    private string _baseLabel = ""; // the ID we always restore to

    private float _timeLeft;
    private float _duration;
    private bool _running;

    void Awake()
    {
        _button = GetComponent<Button>();

        if (cooldownFill != null && cooldownFill.type != Image.Type.Filled)
            cooldownFill.type = Image.Type.Filled;

        if (cooldownFill != null) cooldownFill.fillAmount = 0f;

        if (text != null)
            _baseLabel = text.text ?? "";
    }

    /// <summary>Call this once after you set the button text to the unit ID.</summary>
    public void SetBaseLabel(string id)
    {
        _baseLabel = id ?? "";
        if (!_running && text != null) text.text = _baseLabel;
    }

    /// <summary>Starts cooldown, disables the button, and appends seconds to the label.</summary>
    public void Trigger(float seconds)
    {
        _duration = Mathf.Max(0.01f, seconds);
        _timeLeft = _duration;
        _running  = true;

        if (_button != null) _button.interactable = false;
        if (cooldownFill != null) cooldownFill.fillAmount = 1f;

        UpdateVisuals(force: true);
    }

    /// <summary>Cancels cooldown and restores the label.</summary>
    public void Cancel()
    {
        _running = false;
        _timeLeft = 0f;

        if (_button != null) _button.interactable = true;
        if (cooldownFill != null) cooldownFill.fillAmount = 0f;
        if (text != null) text.text = _baseLabel;
    }

    void Update()
    {
        if (!_running) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _timeLeft -= dt;

        if (_timeLeft <= 0f)
        {
            _running = false;
            _timeLeft = 0f;

            if (_button != null) _button.interactable = true;
            if (cooldownFill != null) cooldownFill.fillAmount = 0f;
            if (text != null) text.text = _baseLabel;
            return;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals(bool force = false)
    {
        if (cooldownFill != null && _duration > 0f)
            cooldownFill.fillAmount = Mathf.Clamp01(_timeLeft / _duration);

        if (text == null) return;

        float display = Mathf.Max(0f, _timeLeft);
        string timeStr = showTenths ? display.ToString("0.0") : Mathf.CeilToInt(display).ToString();

        // ID with appended seconds
        text.text = string.Format(appendFormat, _baseLabel, timeStr);
    }
}
