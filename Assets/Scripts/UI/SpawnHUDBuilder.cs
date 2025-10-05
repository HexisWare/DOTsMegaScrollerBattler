using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;

public class SpawnHUDBuilder : MonoBehaviour
{
    [Header("Config")]
    public TextAsset playerConfigJson;

    [Header("UI")]
    public Transform rowTop;
    public Transform rowMiddle;
    public Transform rowBottom;
    public Button buttonPrefab;

    [Header("Player Box Anchors (world)")]
    public Transform playerTopAnchor;
    public Transform playerMiddleAnchor;
    public Transform playerBottomAnchor;

    // cooldown gate per button
    private readonly Dictionary<Button, float> _nextReadyAt = new();

    // per-row state to disable when building is dead
    private class RowState
    {
        public BuildingStatsMono stats;
        public List<Button> buttons = new List<Button>();
        public bool disabled;
    }
    private RowState _topRow, _midRow, _botRow;

    void Start()
    {
        _topRow = new RowState { stats = playerTopAnchor ? playerTopAnchor.GetComponent<BuildingStatsMono>() : null };
        _midRow = new RowState { stats = playerMiddleAnchor ? playerMiddleAnchor.GetComponent<BuildingStatsMono>() : null };
        _botRow = new RowState { stats = playerBottomAnchor ? playerBottomAnchor.GetComponent<BuildingStatsMono>() : null };

        if (playerConfigJson == null)
        {
            Debug.LogError("[SpawnHUDBuilder] No playerConfigJson assigned.");
            return;
        }

        var cfg = SpawnConfigLoader.LoadFromText(playerConfigJson.text);
        if (cfg?.boxes == null || cfg.boxes.Length == 0)
        {
            Debug.LogError("[SpawnHUDBuilder] Config has no boxes.");
            return;
        }

        foreach (var box in cfg.boxes)
        {
            Transform row = null, anchor = null;
            RowState state = null;
            switch (box.id.ToLowerInvariant())
            {
                case "top":    row = rowTop;    anchor = playerTopAnchor;    state = _topRow; break;
                case "middle": row = rowMiddle; anchor = playerMiddleAnchor; state = _midRow; break;
                case "bottom": row = rowBottom; anchor = playerBottomAnchor; state = _botRow; break;
                default:       row = rowBottom; anchor = playerBottomAnchor; state = _botRow; break;
            }
            if (row == null || anchor == null) { Debug.LogWarning($"[SpawnHUDBuilder] Missing row/anchor for '{box.id}'."); continue; }

            var group = SpawnConfigLoader.ParseGroup(box.group);
            if (box.spawns == null) continue;

            foreach (var def in box.spawns)
            {
                if (!def.enabled) continue;

                var btn   = Instantiate(buttonPrefab, row);
                var label = btn.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = def.id;

                var cool = btn.GetComponent<CooldownButton>();
                if (cool != null)
                {
                    if (cool.text == null && label != null) cool.text = label;
                    cool.SetBaseLabel(def.id);
                }

                var color   = SpawnConfigLoader.ColorFromHtml(def.color, Color.white);
                var mask    = SpawnConfigLoader.MaskFromStrings(def.canAttack);
                var speed   = def.speed;
                var detect  = def.detectRange;
                var rad     = def.radius;
                var scale   = def.scale;
                var hp      = Mathf.Max(1, def.hp);
                var dmg     = Mathf.Max(1, def.damage);
                var atkCd   = Mathf.Max(0.01f, def.cooldown);
                var spawnCd = (def.spawnCooldown > 0f) ? def.spawnCooldown : atkCd;
                var spriteId = def.sprite;

                _nextReadyAt[btn] = 0f;

                btn.onClick.AddListener(() =>
                {
                    // hard gate: if building is dead, no spawn
                    var stats = state?.stats;
                    if (stats != null && stats.currentHP <= 0) return;

                    if (MiniSquareSpawner.Instance == null) return;

                    float now = Time.unscaledTime;
                    float readyAt = _nextReadyAt.TryGetValue(btn, out var t) ? t : 0f;
                    if (now < readyAt) return;

                    _nextReadyAt[btn] = now + spawnCd;

                    var cb = btn.GetComponent<CooldownButton>();
                    if (cb != null) cb.Trigger(spawnCd);
                    else { btn.interactable = false; StartCoroutine(ReenableAfter(btn, spawnCd)); }

                    var p = anchor.position;
                    MiniSquareSpawner.Instance.SpawnPlayerShooter(
                        new float3(p.x, p.y, -0.02f),
                        color,
                        rangeFromConfig:       detect,
                        cooldownFromConfig:    atkCd,
                        speedFromConfig:       speed,
                        detectRangeFromConfig: detect,
                        radiusFromConfig:      rad,
                        hpFromConfig:          hp,
                        damageFromConfig:      dmg,
                        group:                 group,
                        targetMask:            mask,
                        scaleOverride:         scale,
                        spriteId:              spriteId
                    );
                });

                state.buttons.Add(btn);
            }
        }
    }

    void Update()
    {
        // If a building dies, disable its row’s buttons
        DisableRowIfDead(_topRow);
        DisableRowIfDead(_midRow);
        DisableRowIfDead(_botRow);
    }

    private void DisableRowIfDead(RowState rs)
    {
        if (rs == null || rs.stats == null) return;
        if (rs.disabled) return;
        if (rs.stats.currentHP > 0) return;

        rs.disabled = true;
        for (int i = 0; i < rs.buttons.Count; i++)
        {
            var b = rs.buttons[i];
            if (b == null) continue;
            b.interactable = false;

            // Optional: annotate the text to make it obvious
            var label = b.GetComponentInChildren<TMP_Text>(true);
            if (label != null && !label.text.EndsWith(" (X)"))
                label.text = label.text + " (X)";
        }
    }

    private IEnumerator ReenableAfter(Button b, float seconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, seconds));
        if (b != null) b.interactable = true;
    }
}
