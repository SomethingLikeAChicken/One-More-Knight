using UnityEngine;
using OneMoreKnight.Enemies;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// Paces the Run's climax: when the Score crosses the threshold, Wave spawning
    /// pauses and the Boss enters; when the Boss is Defeated, the reward is scored and
    /// Waves resume where they left off. One Boss per Run in M3.
    /// </summary>
    public class BossDirector : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private Boss bossPrefab;

        [Header("Pacing")]
        [SerializeField] [Min(1)] private int scoreThreshold = 1000;
        [SerializeField] [Min(0f)] private float hoverLineFromTop = 1.6f;

        private bool summoned;

        public Boss ActiveBoss { get; private set; }

        private void Awake() => runManager.Changed += OnRunChanged;

        private void OnDestroy()
        {
            if (runManager != null) runManager.Changed -= OnRunChanged;
            if (ActiveBoss != null) ActiveBoss.Defeated -= OnBossDefeated;
        }

        private void OnRunChanged()
        {
            if (summoned || runManager.IsOver || runManager.Score < scoreThreshold) return;
            Summon();
        }

        private void Summon()
        {
            summoned = true;
            waveSpawner.StopSpawning();

            Rect bounds = playArea.Bounds;
            var spawn = new Vector2(bounds.center.x, playArea.SpawnLineY);
            ActiveBoss = Instantiate(bossPrefab, spawn, Quaternion.identity);
            ActiveBoss.Defeated += OnBossDefeated;
            ActiveBoss.Begin(spawn, bounds.yMax - hoverLineFromTop);
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
