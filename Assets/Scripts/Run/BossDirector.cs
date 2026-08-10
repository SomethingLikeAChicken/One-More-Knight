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

        public Boss ActiveBoss { get; private set; }

        /// <summary>Kills so far this Run — drives the backdrop palette.</summary>
        public int BossesDefeated { get; private set; }

        public event System.Action BossDefeated;

        private void Awake()
        {
            // Per-Run seed, owned System.Random - the ADR-0005 seam, like the waves.
            rng = new System.Random(System.Environment.TickCount ^ 0x5f3759df);
            runManager.Changed += OnRunChanged;
        }

        private void OnDestroy()
        {
            if (runManager != null) runManager.Changed -= OnRunChanged;
            if (ActiveBoss != null) ActiveBoss.Defeated -= OnBossDefeated;
        }

        private void OnRunChanged()
        {
            if (ActiveBoss != null || runManager.IsOver || progression == null) return;
            progression.GetStage(stageIndex, out int threshold, out int maxDifficulty);
            if (runManager.Score < threshold) return;

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

        private void Summon(BossStats definition)
        {
            waveSpawner.StopSpawning();

            Rect bounds = playArea.Bounds;
            var spawn = new Vector2(bounds.center.x, playArea.SpawnLineY);
            ActiveBoss = Instantiate(bossPrefab, spawn, Quaternion.identity);
            ActiveBoss.Defeated += OnBossDefeated;

            // The Hero is the target of AimedAtTarget Patterns. Pattern code itself
            // stays actor-agnostic - the wiring decides who is source and target.
            var hero = FindAnyObjectByType<HeroController>();
            ActiveBoss.Begin(definition, spawn, bounds.yMax - hoverLineFromTop, bulletSpawner,
                             hero != null ? hero.transform : null);
        }

        private void OnBossDefeated(Boss boss)
        {
            boss.Defeated -= OnBossDefeated;
            BossesDefeated++;
            BossDefeated?.Invoke();
            runManager.AddScore(boss.Stats.scoreReward);
            Destroy(boss.gameObject);
            ActiveBoss = null;
            waveSpawner.ResumeSpawning();
        }
    }
}
