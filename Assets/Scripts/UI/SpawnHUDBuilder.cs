using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

public class SpawnHUDBuilder : MonoBehaviour
{
    [Header("Row Containers (Horizontal Layout Groups)")]
    public RectTransform rowTop;
    public RectTransform rowMiddle;
    public RectTransform rowBottom;

    [Header("Left Player Boxes (sources)")]
    public Transform topLeftBox;
    public Transform middleLeftBox;
    public Transform bottomLeftBox;

    [Header("UI")]
    public Button buttonPrefab;     // simple Button with a text child
    public TextAsset configJson;    // the JSON file above

    SpawnSetConfig _config;

    void Start()
    {
        if (configJson == null) { Debug.LogError("[SpawnHUD] Missing config JSON!"); return; }
        _config = SpawnConfigLoader.LoadFromText(configJson.text);
        if (_config == null || _config.boxes == null) { Debug.LogError("[SpawnHUD] Bad JSON"); return; }

        BuildAll();
    }

    public void Rebuild() // call this if you change JSON at runtime
    {
        ClearRow(rowTop);
        ClearRow(rowMiddle);
        ClearRow(rowBottom);
        BuildAll();
    }

    void BuildAll()
    {
        BuildRow("Top",    rowTop,    topLeftBox);
        BuildRow("Middle", rowMiddle, middleLeftBox);
        BuildRow("Bottom", rowBottom, bottomLeftBox);
    }

    void BuildRow(string id, RectTransform row, Transform sourceBox)
    {
        if (row == null || sourceBox == null) return;

        var boxCfg = _config.boxes.FirstOrDefault(b => string.Equals(b.id, id, StringComparison.OrdinalIgnoreCase));
        if (boxCfg == null || boxCfg.spawns == null) return;

        foreach (var def in boxCfg.spawns)
        {
            if (!def.enabled) continue;

            var btn = Instantiate(buttonPrefab, row);
            var display = string.IsNullOrWhiteSpace(def.label) ? def.id : def.label;
            SetButtonLabel(btn, display);
            //SetButtonLabel(btn, def.label);

            // ensure a CooldownButton exists and knows its base label
            var cd = btn.GetComponent<CooldownButton>();
            if (cd == null) cd = btn.gameObject.AddComponent<CooldownButton>();
            cd.SetBaseLabel(display);

            // local copy for closure
            var defCopy = def;
            var src = sourceBox;

            // click handler
            btn.onClick.AddListener(() =>
            {
                if (MiniSquareSpawner.Instance == null || src == null) return;

                var p = src.position;
                var color = SpawnConfigLoader.ColorFromHtml(defCopy.color, Color.cyan);

                float speed  = defCopy.speed       > 0 ? defCopy.speed       : MiniSquareSpawner.Instance.playerSpeed;
                float detect = defCopy.detectRange > 0 ? defCopy.detectRange : MiniSquareSpawner.Instance.detectRange;
                float radius = defCopy.radius      > 0 ? defCopy.radius      : MiniSquareSpawner.Instance.radius;
                float scale  = defCopy.scale       > 0 ? defCopy.scale       : MiniSquareSpawner.Instance.miniScale;

                // spawn
                MiniSquareSpawner.Instance.SpawnMiniCustom(
                    new Unity.Mathematics.float3(p.x, p.y, -0.02f),
                    Faction.Player, color,
                    speed, detect, radius, scale
                );

                // start cooldown if any
                if (defCopy.cooldown > 0f) cd.StartCooldown(defCopy.cooldown);
            });
        }
    }

    static void ClearRow(RectTransform row)
    {
        for (int i = row.childCount - 1; i >= 0; i--)
            Destroy(row.GetChild(i).gameObject);
    }

    static void SetButtonLabel(Button b, string label)
    {
        // Prefer TMP if present
        var tmp = b.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.SetText(label);           // TMP-safe setter
            b.name = $"Btn_{label}";
            return;
        }

        // Fallback: legacy UGUI Text
        var legacy = b.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            legacy.text = label;
            b.name = $"Btn_{label}";
            return;
        }

        Debug.LogWarning($"[SpawnHUD] No TMP_Text or Text found under button '{b.name}'. " +
                        "Make sure your button prefab has a TMP Text or legacy Text child.");
    }

}
