using UnityEngine;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Hero;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// Paces the Run's climaxes from a <see cref="BossProgression"/> (issue #30):
    /// at each stage's Score threshold a random eligible Boss (difficulty ≤ the
    /// stage cap) enters; on Defeated the reward is scored, Waves resume, and the
    /// stage advances — endlessly, per the progression's endless rule.
    /// </summary>
    public class BossDirector : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private BulletSpawner bulletSpawner;
        [SerializeField] private Boss bossPrefab;
        [SerializeField] private BossProgression progression;

        [Header("Pacing")]
        [SerializeField] [Min(0f)] private float hoverLineFromTop = 1.6f;

        private readonly System.Collections.Generic.List<BossStats> eligible =
            new System.Collections.Generic.List<BossStats>(8);
        private System.Random rng;
        private BossStats lastPicked;
        private int stageIndex;
        private int rewardScore; // paid-out boss rewards - excluded from stage checks (#68)

        public Boss ActiveBoss { get; private set; }

        private readonly System.Collections.Generic.List<Boss> activeLackeys =
            new System.Collections.Generic.List<Boss>(2);

        /// <summary>Live lackeys of the current encounter (#81) — the HUD draws a bar per entry.</summary>
        public System.Collections.Generic.IReadOnlyList<Boss> ActiveLackeys => activeLackeys;

        /// <summary>Kills so far this Run — drives the backdrop palette. Main bosses
        /// only; lackey deaths (#81) pay rewards and drops but do not advance this.</summary>
        public int BossesDefeated { get; private set; }

        public event System.Action BossDefeated;

        /// <summary>Defeat with the death position — the spoils system (#55) listens.</summary>
        public event System.Action<Vector2> BossDefeatedAt;

        /// <summary>Defeat with the identity (#91) — RunStats tallies WHICH boss fell,
        /// mains and lackeys alike, so "slay the Pale King" can be an achievement.</summary>
        public event System.Action<BossStats> BossSlain;

        /// <summary>The MAIN boss entered a phase (#95) — the spoils system feeds
        /// long d8+ fights on this. Lackey phases do not relay.</summary>
        public event System.Action<Boss> BossPhaseEntered;

        private void Awake()
        {
            // Per-Run seed, owned System.Random - the ADR-0005 seam, like the waves.
            rng = new System.Random(System.Environment.TickCount ^ 0x5f3759df);
            runManager.Changed += OnRunChanged;
        }

        private void OnDestroy()
        {
            if (runManager != null) runManager.Changed -= OnRunChanged;
            if (ActiveBoss != null)
            {
                ActiveBoss.Defeated -= OnBossDefeated;
                ActiveBoss.PhaseEntered -= OnBossPhaseEntered;
            }
            foreach (Boss lackey in activeLackeys)
                if (lackey != null) lackey.Defeated -= OnLackeyDefeated;
        }

        private void OnRunChanged()
        {
            if (ActiveBoss != null || runManager.IsOver || progression == null) return;
            progression.GetStage(stageIndex, out int threshold, out int maxDifficulty);
            // Only EARNED score summons bosses - a big reward must not chain straight
            // into the next fight (#68).
            if (runManager.Score - rewardScore < threshold) return;

            BossStats pick = PickEligible(maxDifficulty);
            if (pick == null) return;
            Summon(pick);
            lastPicked = pick;
            stageIndex++;
        }

        /// <summary>Random pool Boss with difficulty ≤ the cap, avoiding the
        /// immediately previous Boss when alternatives exist.</summary>
        private BossStats PickEligible(int maxDifficulty)
        {
            eligible.Clear();
            foreach (BossStats b in progression.pool)
                if (b != null && b.difficulty <= maxDifficulty) eligible.Add(b);
            if (eligible.Count == 0) return null;
            if (eligible.Count > 1 && lastPicked != null) eligible.Remove(lastPicked);
            return eligible[rng.Next(eligible.Count)];
        }

        /// <summary>Starts an encounter with <paramref name="definition"/> now. Public
        /// as the seam for the Arena and for staged play-mode tests; the Run itself
        /// always arrives here through the Score thresholds.</summary>
        public void Summon(BossStats definition)
        {
            waveSpawner.StopSpawning();

            Rect bounds = playArea.Bounds;
            // The Hero is the target of AimedAtTarget Patterns. Pattern code itself
            // stays actor-agnostic - the wiring decides who is source and target.
            var hero = FindAnyObjectByType<HeroController>();
            Transform target = hero != null ? hero.transform : null;

            ActiveBoss = SpawnBossInstance(definition, bounds.center.x,
                                           bounds.yMax - hoverLineFromTop, target);
            ActiveBoss.Defeated += OnBossDefeated;
            ActiveBoss.PhaseEntered += OnBossPhaseEntered;

            // Lackeys (#81): the guard spawns flanking the main Boss on a lower hover
            // line, and the main Boss is untouchable until the guard falls.
            if (definition.lackeys != null && definition.lackeys.Length > 0)
            {
                ActiveBoss.Health.Invulnerable = true;
                for (int i = 0; i < definition.lackeys.Length; i++)
                {
                    BossStats lackeyDef = definition.lackeys[i];
                    if (lackeyDef == null) continue;
                    float side = definition.lackeys.Length == 1 ? 0f
                        : (i - (definition.lackeys.Length - 1) * 0.5f) * 2f;
                    Boss lackey = SpawnBossInstance(lackeyDef, bounds.center.x + side * 2.3f,
                                                    bounds.yMax - hoverLineFromTop - 1.3f, target);
                    lackey.Defeated += OnLackeyDefeated;
                    activeLackeys.Add(lackey);
                }
            }
        }

        private Boss SpawnBossInstance(BossStats definition, float x, float hoverY, Transform target)
        {
            var spawn = new Vector2(x, playArea.SpawnLineY);
            Boss boss = Instantiate(bossPrefab, spawn, Quaternion.identity);
            boss.Begin(definition, spawn, hoverY, bulletSpawner, target, waveSpawner);
            return boss;
        }

        private void OnLackeyDefeated(Boss lackey)
        {
            lackey.Defeated -= OnLackeyDefeated;
            activeLackeys.Remove(lackey);

            // A lackey pays like a boss (reward outside the stage thresholds, #68,
            // and a guaranteed drop) but does not count as one (#81).
            BossSlain?.Invoke(lackey.Stats);
            BossDefeatedAt?.Invoke(lackey.transform.position);
            rewardScore += lackey.Stats.scoreReward;
            runManager.AddScore(lackey.Stats.scoreReward);
            Destroy(lackey.gameObject);

            if (activeLackeys.Count == 0 && ActiveBoss != null && ActiveBoss.Health.IsAlive)
                ActiveBoss.Unleash();
        }

        private void OnBossPhaseEntered(Boss boss, int _) => BossPhaseEntered?.Invoke(boss);

        private void OnBossDefeated(Boss boss)
        {
            boss.Defeated -= OnBossDefeated;
            boss.PhaseEntered -= OnBossPhaseEntered;
            // Defensive: a guarded Boss cannot die, but leave no orphaned lackeys
            // if a future data combination finds a way.
            foreach (Boss lackey in activeLackeys)
            {
                if (lackey == null) continue;
                lackey.Defeated -= OnLackeyDefeated;
                Destroy(lackey.gameObject);
            }
            activeLackeys.Clear();
            BossesDefeated++;
            BossSlain?.Invoke(boss.Stats);
            BossDefeated?.Invoke();
            BossDefeatedAt?.Invoke(boss.transform.position);
            rewardScore += boss.Stats.scoreReward;
            runManager.AddScore(boss.Stats.scoreReward);
            Destroy(boss.gameObject);
            ActiveBoss = null;
            waveSpawner.ResumeSpawning();
        }
    }
}
