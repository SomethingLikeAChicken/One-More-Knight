using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OneMoreKnight.Combat;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// The in-Run HUD, uGUI (decision recorded in issue #28: uGUI over UI Toolkit —
    /// simple overlay, mature docs, no WebGL surprises). Score and Wave readouts,
    /// Hero Health as pips, and the Boss HP bar while an encounter is active.
    ///
    /// Poll-based: the HUD reads state every frame instead of wiring events — at this
    /// size the simplicity is worth more than the callbacks.
    /// </summary>
    public class RunHud : MonoBehaviour
    {
        [Header("State sources")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private Health heroHealth;
        [SerializeField] private BossDirector bossDirector;

        [Header("Widgets")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text waveText;
        [SerializeField] private RectTransform healthPipContainer;
        [SerializeField] private Image healthPipTemplate;
        [SerializeField] private GameObject bossBar;
        [SerializeField] private RectTransform bossBarFill;
        [SerializeField] private Text bossNameText;
        [SerializeField] private Image damageVignette;
        [SerializeField] private Text curseText;
        [SerializeField] private Text buffText;
        [SerializeField] private Hero.HeroUpgrades heroUpgrades;

        private static readonly (Hero.PowerupType type, string label)[] BuffLabels =
        {
            (Hero.PowerupType.Damage, "SWORD"),
            (Hero.PowerupType.MoveSpeed, "WING"),
            (Hero.PowerupType.FireRate, "BOLT")
        };
        private readonly System.Text.StringBuilder buffBuilder = new System.Text.StringBuilder(64);
        [SerializeField] [Min(0.05f)] private float vignetteFade = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float vignettePeak = 0.55f;

        private static readonly Color PipFull = new Color(0.95f, 0.3f, 0.35f);
        private static readonly Color PipEmpty = new Color(0.25f, 0.12f, 0.16f);

        private readonly List<Image> pips = new List<Image>(8);
        private int lastHeroHp = int.MaxValue;
        private float vignetteStrength;

        private void Update()
        {
            scoreText.text = $"SCORE  {runManager.Score:n0}";
            waveText.text = string.IsNullOrEmpty(runManager.WaveModifierLabel)
                ? $"WAVE  {runManager.Wave}"
                : $"WAVE  {runManager.Wave}  —  {runManager.WaveModifierLabel}";

            // Capped so debug health pools (or a future absurd Boon) cannot flood the
            // canvas with pip objects; past the cap the row simply stays full-width.
            EnsurePips(Mathf.Min(heroHealth.Max, 12));
            for (int i = 0; i < pips.Count; i++)
                pips[i].color = i < heroHealth.Current ? PipFull : PipEmpty;

            // Damage vignette: spikes when the polled HP drops, fades every frame.
            // A poll fits the HUD's design (no events) and a heal/reset stays silent.
            if (heroHealth.Current < lastHeroHp) vignetteStrength = 1f;
            lastHeroHp = heroHealth.Current;
            if (damageVignette != null)
            {
                vignetteStrength = Mathf.Max(0f, vignetteStrength - Time.deltaTime / vignetteFade);
                // Blind curse (#68): the vignette holds shut, near-black, for the duration.
                bool blind = heroUpgrades != null && heroUpgrades.ActiveCurse == Hero.CurseType.Blind;
                Color c = blind ? new Color(0.12f, 0.02f, 0.2f, 0.92f)
                                : new Color(1f, 1f, 1f, vignettePeak * vignetteStrength);
                damageVignette.color = c;
                bool visible = blind || vignetteStrength > 0f;
                if (damageVignette.enabled != visible) damageVignette.enabled = visible;
            }

            // Curse readout (#55): name + seconds while a death-curse is active.
            if (curseText != null)
            {
                var curse = heroUpgrades != null ? heroUpgrades.ActiveCurse : Hero.CurseType.None;
                string label = curse == Hero.CurseType.None ? ""
                    : $"CURSED — {curse.ToString().ToUpperInvariant()}  {heroUpgrades.CurseRemaining:0.0}s";
                if (curseText.text != label) curseText.text = label;
            }

            // Buff readout (#67): the timed blessings, in gold.
            if (buffText != null && heroUpgrades != null)
            {
                buffBuilder.Length = 0;
                foreach (var (type, name) in BuffLabels)
                {
                    if (!heroUpgrades.BuffActive(type)) continue;
                    if (buffBuilder.Length > 0) buffBuilder.Append("   ");
                    buffBuilder.Append(name).Append(' ')
                        .Append(heroUpgrades.BuffRemaining(type).ToString("0.0")).Append('s');
                }
                string buffs = buffBuilder.ToString();
                if (buffText.text != buffs) buffText.text = buffs;
            }

            var boss = bossDirector != null ? bossDirector.ActiveBoss : null;
            bool bossActive = boss != null && boss.Health.IsAlive;
            if (bossBar.activeSelf != bossActive) bossBar.SetActive(bossActive);
            if (bossActive)
            {
                float fraction = Mathf.Clamp01((float)boss.Health.Current / boss.Health.Max);
                bossBarFill.anchorMax = new Vector2(fraction, 1f);

                // Name tag (#53): people want to know what they are fighting.
                if (bossNameText != null)
                {
                    string title = boss.Stats.DisplayName;
                    if (boss.CurrentPhase >= 0 && boss.CurrentPhase < boss.Stats.phases.Length
                        && !string.IsNullOrEmpty(boss.Stats.phases[boss.CurrentPhase].name))
                        title += "  ·  " + boss.Stats.phases[boss.CurrentPhase].name;
                    title = title.ToUpperInvariant();
                    if (bossNameText.text != title) bossNameText.text = title;
                }
            }
        }

        private void EnsurePips(int count)
        {
            while (pips.Count < count)
            {
                Image pip = Instantiate(healthPipTemplate, healthPipContainer);
                pip.gameObject.SetActive(true);
                pips.Add(pip);
            }
            for (int i = 0; i < pips.Count; i++)
                pips[i].gameObject.SetActive(i < count);
        }
    }
}
