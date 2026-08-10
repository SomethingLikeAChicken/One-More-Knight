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
            waveText.text = $"WAVE  {runManager.Wave}";

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
                Color c = damageVignette.color;
                c.a = vignettePeak * vignetteStrength;
                damageVignette.color = c;
                if (damageVignette.enabled != vignetteStrength > 0f)
                    damageVignette.enabled = vignetteStrength > 0f;
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
                    string title = boss.Stats.name.StartsWith("Boss")
                        ? boss.Stats.name.Substring(4) : boss.Stats.name;
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
