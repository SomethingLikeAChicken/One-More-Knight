using UnityEngine;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Hero;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>One roster slot: which Boss enters, and at what Score.</summary>
    [System.Serializable]
    public class BossEncounter
    {
        public BossStats boss;
        [Min(1)] public int scoreThreshold = 1000;
    }

    /// <summary>
    /// Paces the Run's climaxes: an ordered roster of Boss encounters at rising Score
    /// thresholds. When the Score crosses the next threshold, Wave spawning pauses and
    /// that Boss enters; on Defeated the reward is scored, Waves resume, and the
    /// roster advances. After the last encounter the Run keeps escalating on Waves.
    /// </summary>
    public class BossDirector : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private BulletSpawner bulletSpawner;
        [SerializeField] private Boss bossPrefab;

        [Header("Roster")]
        [SerializeField] private BossEncounter[] encounters = new BossEncounter[0];
        [SerializeField] [Min(0f)] private float hoverLineFromTop = 1.6f;

        private int nextEncounter;

        public Boss ActiveBoss { get; private set; }

        private void Awake() => runManager.Changed += OnRunChanged;

        private void OnDestroy()
        {
            if (runManager != null) runManager.Changed -= OnRunChanged;
            if (ActiveBoss != null) ActiveBoss.Defeated -= OnBossDefeated;
        }

        private void OnRunChanged()
        {
            if (ActiveBoss != null || runManager.IsOver) return;
            if (nextEncounter >= encounters.Length) return;
            if (runManager.Score < encounters[nextEncounter].scoreThreshold) return;
            Summon(encounters[nextEncounter].boss);
            nextEncounter++;
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
            runManager.AddScore(boss.Stats.scoreReward);
            Destroy(boss.gameObject);
            ActiveBoss = null;
            waveSpawner.ResumeSpawning();
        }
    }
}
