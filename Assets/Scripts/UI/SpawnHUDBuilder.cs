using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;

public class SpawnHUDBuilder : MonoBehaviour
{
    [Header("Config")]
    public TextAsset playerConfigJson; // assign SpawnConfig.json

    [Header("UI")]
    public Transform rowTop;
    public Transform rowMiddle;
    public Transform rowBottom;
    public Button buttonPrefab;

    [Header("Player Box Anchors (world)")]
    public Transform playerTopAnchor;
    public Transform playerMiddleAnchor;
    public Transform playerBottomAnchor;

    // when each button can spawn again (unscaled time)
    private readonly Dictionary<Button, float> _nextReadyAt = new();

    void Start()
    {
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
            switch (box.id.ToLowerInvariant())
            {
                case "top":    row = rowTop;    anchor = playerTopAnchor;    break;
                case "middle": row = rowMiddle; anchor = playerMiddleAnchor; break;
                case "bottom": row = rowBottom; anchor = playerBottomAnchor; break;
                default:       row = rowBottom; anchor = playerBottomAnchor; break;
            }
            if (row == null || anchor == null)
            {
                Debug.LogWarning($"[SpawnHUDBuilder] Missing row/anchor for '{box.id}'. Skipping.");
                continue;
            }

            var group = SpawnConfigLoader.ParseGroup(box.group);

            if (box.spawns == null) continue;
            foreach (var def in box.spawns)
            {
                if (!def.enabled) continue;

                var btn   = Instantiate(buttonPrefab, row);
                var label = btn.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = def.id; // ← always show ID

                // Wire single-text cooldown component (required for appended seconds)
                var cool = btn.GetComponent<CooldownButton>();
                if (cool != null)
                {
                    if (cool.text == null && label != null) cool.text = label;
                    cool.SetBaseLabel(def.id); // cache the ID as base label
                }

                // capture per-button config
                var color   = SpawnConfigLoader.ColorFromHtml(def.color, Color.white);
                var mask    = SpawnConfigLoader.MaskFromStrings(def.canAttack);
                var speed   = def.speed;
                var detect  = def.detectRange;                    // Shooter.Range & Agent.DetectRange
                var rad     = def.radius;
                var scale   = def.scale;
                var hp      = Mathf.Max(1, def.hp);
                var dmg     = Mathf.Max(1, def.damage);
                var atkCd   = Mathf.Max(0.01f, def.cooldown);     // ATTACK speed
                var spawnCd = (def.spawnCooldown > 0f) ? def.spawnCooldown : atkCd; // SPAWN cooldown (UI gate)

                _nextReadyAt[btn] = 0f;

                btn.onClick.AddListener(() =>
                {
                    if (MiniSquareSpawner.Instance == null) return;

                    float now = Time.unscaledTime;
                    float readyAt = _nextReadyAt.TryGetValue(btn, out var t) ? t : 0f;
                    if (now < readyAt) return; // still cooling

                    _nextReadyAt[btn] = now + spawnCd;

                    // Start numeric cooldown on the single TMP text
                    var cb = btn.GetComponent<CooldownButton>();
                    if (cb != null) cb.Trigger(spawnCd);
                    else
                    {
                        // Fallback: disable without text countdown (requires CooldownButton to see seconds)
                        btn.interactable = false;
                        StartCoroutine(ReenableAfter(btn, spawnCd));
                    }

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
                        scaleOverride:         scale
                    );
                });
            }
        }
    }

    private IEnumerator ReenableAfter(Button b, float seconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, seconds));
        if (b != null) b.interactable = true;
    }
}
