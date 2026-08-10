using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Run;

namespace OneMoreKnight.Waves
{
    /// <summary>
    /// Spawns endless Waves (CONTEXT.md: Wave) — a group of Enemies that enters together.
    /// The next Wave starts once the previous one is cleared, and each Wave is slightly
    /// larger and faster than the last, up to a cap.
    ///
    /// The Wave loop is a coroutine on a scene object that is never deactivated. That
    /// matters: coroutines stop when their GameObject is disabled, and pooling disables
    /// objects — so this loop deliberately does not live on anything poolable
    /// (AGENTS.md gotcha).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private RunManager runManager;
        [SerializeField] private BulletSpawner bulletSpawner;

        [Header("Wave shape")]
        [SerializeField] [Min(1)] private int baseEnemyCount = 4;
        [SerializeField] [Min(0)] private int enemiesAddedPerWave = 2;
        [SerializeField] [Min(1)] private int maxEnemiesPerWave = 24;
        [SerializeField] [Min(0f)] private float speedIncreasePerWave = 0.07f;
        [SerializeField] [Min(1f)] private float maxSpeedMultiplier = 2.5f;
        [SerializeField] [Min(0f)] private float spawnInterval = 0.28f;
        [SerializeField] [Min(0f)] private float intermission = 1.5f;

        private ObjectPool<Enemy> pool;
        private Coroutine loop;
        private int alive;
        private int waveNumber;

        private void Awake()
        {
            pool = new ObjectPool<Enemy>(
                createFunc: CreateEnemy,
                actionOnGet: e => e.gameObject.SetActive(true),
                actionOnRelease: e => e.gameObject.SetActive(false),
                actionOnDestroy: e => { if (e != null) Destroy(e.gameObject); },
                collectionCheck: true,
                defaultCapacity: 32,
                maxSize: 256);
        }

        private void Start() => loop = StartCoroutine(RunWaves());

        private Enemy CreateEnemy()
        {
            Enemy enemy = Instantiate(enemyPrefab, transform);
            // Subscribed once per instance, not per spawn — a pooled Enemy is reused many
            // times and per-spawn subscription would stack handlers.
            enemy.Killed += OnEnemyKilled;
            enemy.Retired += OnEnemyRetired;
            enemy.Initialize(bulletSpawner);
            return enemy;
        }

        private IEnumerator RunWaves()
        {
            while (true)
            {
                waveNumber++;
                runManager.ReportWave(waveNumber);

                int count = Mathf.Min(
                    baseEnemyCount + (waveNumber - 1) * enemiesAddedPerWave,
                    maxEnemiesPerWave);

                float speedMultiplier = Mathf.Min(
                    1f + (waveNumber - 1) * speedIncreasePerWave,
                    maxSpeedMultiplier);

                for (int i = 0; i < count; i++)
                {
                    SpawnOne(i, count, speedMultiplier);
                    yield return new WaitForSeconds(spawnInterval);
                }

                while (alive > 0) yield return null;

                yield return new WaitForSeconds(intermission);
            }
        }

        private void SpawnOne(int index, int count, float speedMultiplier)
        {
            Rect bounds = playArea.Bounds;

            float t = (index + 0.5f) / count;
            float x = Mathf.Lerp(bounds.xMin, bounds.xMax, t);

            // Cosmetic jitter only. When ADR-0005's seeded run RNG lands, anything that
            // affects the outcome moves onto that stream — this does not.
            x = Mathf.Clamp(x + Random.Range(-0.35f, 0.35f), bounds.xMin, bounds.xMax);

            Enemy enemy = pool.Get();
            enemy.Spawn(new Vector2(x, playArea.SpawnLineY), speedMultiplier, playArea.DespawnLineY);
            alive++;
        }

        private void OnEnemyKilled(Enemy enemy) => runManager.AddScore(enemy.Stats.scoreValue);

        private void OnEnemyRetired(Enemy enemy)
        {
            alive--;
            pool.Release(enemy);
        }

        /// <summary>Pauses the Wave loop (Run over, or a Boss takes the stage). Enemies
        /// already on screen keep falling; nothing new enters.</summary>
        public void StopSpawning()
        {
            if (loop == null) return;
            StopCoroutine(loop);
            loop = null;
        }

        /// <summary>Resumes after a pause. The Wave counter is a field, so escalation
        /// continues where it stopped instead of restarting at Wave 1.</summary>
        public void ResumeSpawning()
        {
            if (loop != null) return;
            loop = StartCoroutine(RunWaves());
        }
    }
}
