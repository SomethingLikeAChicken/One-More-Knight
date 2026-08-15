using System.Collections.Generic;
using UnityEngine;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// Runs the Pact layer (#129): at Run start and after every Boss kill it offers
    /// three random candidates — one per tier — plus refusal. Exactly one Pact is
    /// active at a time (#117 §9.2); taking one replaces the last, refusing drops
    /// to ×1. The game freezes (timeScale 0) while the offer is open; earned Score
    /// cannot advance while frozen and #68 keeps Boss rewards off the pacing clock,
    /// so an offer can never be interrupted by the next summon.
    /// </summary>
    public class PactDirector : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private Scoring.RunStats runStats;

        [Header("The bargain (#117 §9.2: multiplier bound to the TIER)")]
        [SerializeField] private Pact[] pool = new Pact[0];
        [SerializeField] [Min(1f)] private float easyMultiplier = 1.4f;
        [SerializeField] [Min(1f)] private float mediumMultiplier = 2f;
        [SerializeField] [Min(1f)] private float hardMultiplier = 3f;

        private readonly List<Pact> tierScratch = new List<Pact>(8);
        private System.Random rng;
        private PactOfferPanel panel;

        /// <summary>The live Pact, null when refused/none — the HUD reads this.</summary>
        public Pact ActivePact { get; private set; }

        /// <summary>The active tier multiplier, 1 without a Pact.</summary>
        public float ActiveMultiplier => ActivePact == null ? 1f : TierMultiplier(ActivePact.tier);

        private void Awake()
        {
            rng = new System.Random(System.Environment.TickCount ^ unchecked((int)0x9e3779b9));
            panel = PactOfferPanel.Create(this);
            if (bossDirector != null) bossDirector.BossDefeated += OnBossDefeated;
        }

        private void Start()
        {
            // The pre-Run offer (#117 §9.2) - the Run is one frame old, nothing has
            // spawned; the freeze is invisible.
            OpenOffer();
        }

        private void OnDestroy()
        {
            if (bossDirector != null) bossDirector.BossDefeated -= OnBossDefeated;
            Time.timeScale = 1f; // never leave a dead scene frozen
        }

        private void OnBossDefeated() => OpenOffer();

        private void OpenOffer()
        {
            if (runManager == null || runManager.IsOver || pool.Length == 0 || panel == null) return;
            Pact easy = PickTier(PactTier.Easy);
            Pact medium = PickTier(PactTier.Medium);
            Pact hard = PickTier(PactTier.Hard);
            if (easy == null && medium == null && hard == null) return;
            Time.timeScale = 0f;
            panel.Show(easy, medium, hard,
                       easyMultiplier, mediumMultiplier, hardMultiplier, ActivePact);
        }

        /// <summary>Panel callback: <paramref name="pact"/> null = refused.</summary>
        public void Resolve(Pact pact)
        {
            Time.timeScale = 1f;

            // Unwind the previous bargain completely; WaveOffset jumps are
            // deliberately not rewound - the clock only runs forward.
            if (waveSpawner != null)
            {
                waveSpawner.PactSpeedScale = 1f;
                waveSpawner.PactHpScale = 1f;
                waveSpawner.PactDamageScale = 1f;
                waveSpawner.PactCadenceScale = 1f;
            }

            ActivePact = pact;
            runManager.PactScoreMultiplier = ActiveMultiplier;

            if (pact != null && waveSpawner != null)
            {
                foreach (PactEffect effect in pact.effects)
                {
                    switch (effect.type)
                    {
                        case PactEffectType.EnemySpeedScale:
                            waveSpawner.PactSpeedScale *= effect.value; break;
                        case PactEffectType.EnemyHpScale:
                            waveSpawner.PactHpScale *= effect.value; break;
                        case PactEffectType.EnemyDamageScale:
                            waveSpawner.PactDamageScale *= effect.value; break;
                        case PactEffectType.SpawnCadenceScale:
                            waveSpawner.PactCadenceScale *= effect.value; break;
                        case PactEffectType.WaveOffset:
                            waveSpawner.AdvanceWaves(Mathf.RoundToInt(effect.value)); break;
                    }
                }
            }

            if (runStats != null)
                runStats.RecordPactSegment(pact != null ? pact.name : "none", ActiveMultiplier,
                                           bossDirector != null ? bossDirector.BossesDefeated : 0);
        }

        public float TierMultiplier(PactTier tier)
        {
            switch (tier)
            {
                case PactTier.Hard: return hardMultiplier;
                case PactTier.Medium: return mediumMultiplier;
                default: return easyMultiplier;
            }
        }

        private Pact PickTier(PactTier tier)
        {
            tierScratch.Clear();
            foreach (Pact p in pool)
                if (p != null && p.tier == tier) tierScratch.Add(p);
            if (tierScratch.Count == 0) return null;
            // Avoid re-offering the live Pact when an alternative exists.
            if (tierScratch.Count > 1 && ActivePact != null) tierScratch.Remove(ActivePact);
            return tierScratch[rng.Next(tierScratch.Count)];
        }
    }
}
