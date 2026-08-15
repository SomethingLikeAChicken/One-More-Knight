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
            (Hero.PowerupType.FireRate, "BOLT"),
            (Hero.PowerupType.Aegis, "AEGIS")
        };
        private readonly System.Text.StringBuilder buffBuilder = new System.Text.StringBuilder(64);
        [SerializeField] [Min(0.05f)] private float vignetteFade = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float vignettePeak = 0.55f;

        private static readonly Color PipFull = new Color(0.95f, 0.3f, 0.35f);
        private static readonly Color PipEmpty = new Color(0.25f, 0.12f, 0.16f);

        private static readonly Color WardBarBlue = new Color(0.4f, 0.68f, 1f);
        private static readonly Color GuardedGrey = new Color(0.42f, 0.42f, 0.48f);

        private readonly List<Image> pips = new List<Image>(8);
        private int lastHeroHp = int.MaxValue;
        private float vignetteStrength;
        private Image bossBarFillImage;
        private Color bossBarBaseColor;

        private struct LackeyBar
        {
            public GameObject Root;
            public RectTransform Fill;
            public Image FillImage;
            public Text Name;
        }
        private readonly List<LackeyBar> lackeyBars = new List<LackeyBar>(2);

        private void Start()
        {
            // PixelLab skin (#115): generated font on every readout, iron frame
            // around the boss bar. Cloned lackey bars inherit both for free.
            var theme = Flow.UiTheme.Instance;
            if (theme == null) return;
            if (theme.font != null)
            {
                foreach (var t in new[] { scoreText, waveText, bossNameText, curseText, buffText })
                    if (t != null) t.font = theme.font;
                if (bossBar != null)
                    foreach (var t in bossBar.GetComponentsInChildren<Text>(true))
                        t.font = theme.font;
            }
            if (theme.bossBarFrame != null && bossBar != null && bossBar.transform.Find("Frame") == null)
            {
                var frameGo = new GameObject("Frame");
                frameGo.transform.SetParent(bossBar.transform, false);
                frameGo.transform.SetSiblingIndex(0);
                var frame = frameGo.AddComponent<Image>();
                frame.sprite = theme.bossBarFrame;
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = false;
                var rt = frame.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(-12f, -10f);
                rt.offsetMax = new Vector2(12f, 10f);
            }
        }

        private void Update()
        {
            scoreText.text = $"SCORE  {runManager.LeaderboardScore:n0}"; // the ranked figure (#123)
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
                // Lazy-cached so the ward colouring (#79) needs no scene rewiring.
                if (bossBarFillImage == null)
                {
                    bossBarFillImage = bossBarFill.GetComponent<Image>();
                    bossBarBaseColor = bossBarFillImage.color;
                }

                // While the ward holds, the bar IS the ward: blue, draining to the
                // break (#79). Guarded by lackeys (#81): grey until the guard falls.
                // Then the violet HP fill.
                bool warded = boss.Health.Shield > 0;
                bool guarded = boss.Health.Invulnerable;
                float fraction = warded
                    ? Mathf.Clamp01((float)boss.Health.Shield / Mathf.Max(1, boss.Stats.shieldHealth))
                    : Mathf.Clamp01((float)boss.Health.Current / boss.Health.Max);
                bossBarFill.anchorMax = new Vector2(fraction, 1f);
                Color barColor = guarded ? GuardedGrey : warded ? WardBarBlue : bossBarBaseColor;
                if (bossBarFillImage.color != barColor) bossBarFillImage.color = barColor;

                // Name tag (#53): people want to know what they are fighting.
                if (bossNameText != null)
                {
                    string title = boss.Stats.DisplayName;
                    if (boss.CurrentPhase >= 0 && boss.CurrentPhase < boss.Stats.phases.Length
                        && !string.IsNullOrEmpty(boss.Stats.phases[boss.CurrentPhase].name))
                        title += "  ·  " + boss.Stats.phases[boss.CurrentPhase].name;
                    if (guarded) title += "  ·  GUARDED";
                    else if (warded) title += "  ·  SHIELD";
                    title = title.ToUpperInvariant();
                    if (bossNameText.text != title) bossNameText.text = title;
                }
            }

            UpdateLackeyBars(bossActive);
        }

        /// <summary>One small bar per live lackey (#81), cloned from the main BossBar
        /// like the pips clone their template — no scene rewiring.</summary>
        private void UpdateLackeyBars(bool bossActive)
        {
            var lackeys = bossDirector != null ? bossDirector.ActiveLackeys : null;
            int liveCount = bossActive && lackeys != null ? lackeys.Count : 0;

            while (lackeyBars.Count < liveCount)
            {
                GameObject clone = Instantiate(bossBar, bossBar.transform.parent);
                clone.name = "LackeyBar" + lackeyBars.Count;
                var rt = clone.GetComponent<RectTransform>();
                var templateRt = bossBar.GetComponent<RectTransform>();
                float width = templateRt.rect.width;
                rt.localScale = new Vector3(0.42f, 0.75f, 1f);
                rt.anchoredPosition = templateRt.anchoredPosition
                    + new Vector2((lackeyBars.Count == 0 ? -1f : 1f) * width * 0.24f,
                                  -templateRt.rect.height * 0.75f - 14f);
                lackeyBars.Add(new LackeyBar
                {
                    Root = clone,
                    Fill = (RectTransform)clone.transform.Find("Fill"),
                    FillImage = clone.transform.Find("Fill").GetComponent<Image>(),
                    Name = clone.GetComponentInChildren<Text>(true)
                });
            }

            for (int i = 0; i < lackeyBars.Count; i++)
            {
                bool used = i < liveCount && lackeys[i] != null && lackeys[i].Health.IsAlive;
                if (lackeyBars[i].Root.activeSelf != used) lackeyBars[i].Root.SetActive(used);
                if (!used) continue;

                var lackey = lackeys[i];
                float fraction = Mathf.Clamp01((float)lackey.Health.Current / lackey.Health.Max);
                lackeyBars[i].Fill.anchorMax = new Vector2(fraction, 1f);
                if (lackeyBars[i].FillImage.color != bossBarBaseColor)
                    lackeyBars[i].FillImage.color = bossBarBaseColor;
                string title = lackey.Stats.DisplayName.ToUpperInvariant();
                if (lackeyBars[i].Name != null && lackeyBars[i].Name.text != title)
                    lackeyBars[i].Name.text = title;
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
