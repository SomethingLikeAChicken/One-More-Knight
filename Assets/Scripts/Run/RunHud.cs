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

        private static readonly Color PipFull = new Color(0.95f, 0.3f, 0.35f);
        private static readonly Color PipEmpty = new Color(0.25f, 0.12f, 0.16f);

        private readonly List<Image> pips = new List<Image>(8);

        private void Update()
        {
            scoreText.text = $"SCORE  {runManager.Score:n0}";
            waveText.text = $"WAVE  {runManager.Wave}";

            // Capped so debug health pools (or a future absurd Boon) cannot flood the
            // canvas with pip objects; past the cap the row simply stays full-width.
            EnsurePips(Mathf.Min(heroHealth.Max, 12));
            for (int i = 0; i < pips.Count; i++)
                pips[i].color = i < heroHealth.Current ? PipFull : PipEmpty;

            var boss = bossDirector != null ? bossDirector.ActiveBoss : null;
            bool bossActive = boss != null && boss.Health.IsAlive;
            if (bossBar.activeSelf != bossActive) bossBar.SetActive(bossActive);
            if (bossActive)
            {
                float fraction = Mathf.Clamp01((float)boss.Health.Current / boss.Health.Max);
                bossBarFill.anchorMax = new Vector2(fraction, 1f);
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
