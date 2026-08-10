using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Hero;
using OneMoreKnight.Run;

namespace OneMoreKnight.Waves
{
    /// <summary>
    /// Plays the Run's <see cref="WaveSequence"/>: authored Waves of choreographed
    /// Enemy groups (CONTEXT.md: Wave), looping with capped multipliers once the
    /// authored list is exhausted. The next Wave starts once the previous one is
    /// cleared.
    ///
    /// The Wave loop is a coroutine on a scene object that is never deactivated. That
    /// matters: coroutines stop when their GameObject is disabled, and pooling
    /// disables objects — so this loop deliberately does not live on anything
    /// poolable (AGENTS.md gotcha).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private RunManager runManager;
        [SerializeField] private BulletSpawner bulletSpawner;
        [SerializeField] private WaveSequence sequence;

        private ObjectPool<Enemy> pool;
        private Coroutine loop;
        private Transform target;
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

        private void Start()
        {
            // The Hero is the target for AimedAtTarget Enemy Patterns. Wiring decides
            // who is source and target; pattern code stays actor-agnostic (ADR-0003).
            var hero = FindAnyObjectByType<HeroController>();
            target = hero != null ? hero.transform : null;
            loop = StartCoroutine(RunWaves());
        }

        private Enemy CreateEnemy()
        {
            Enemy enemy = Instantiate(enemyPrefab, transform);
            // Subscribed once per instance, not per spawn — a pooled Enemy is reused many
            // times and per-spawn subscription would stack handlers.
            enemy.Killed += OnEnemyKilled;
            enemy.Retired += OnEnemyRetired;
            enemy.Initialize(bulletSpawner, target);
            return enemy;
        }

        private IEnumerator RunWaves()
        {
            while (true)
            {
                WaveDefinition wave = sequence.Resolve(waveNumber, out float hpMult, out float speedMult);
                if (wave == null) yield break;

                waveNumber++;
                runManager.ReportWave(waveNumber);

                foreach (EnemyGroup group in wave.groups)
                {
                    if (group.type == null) continue;
                    if (group.delayBeforeGroup > 0f) yield return new WaitForSeconds(group.delayBeforeGroup);

                    for (int i = 0; i < group.count; i++)
                    {
                        SpawnOne(group, i, hpMult, speedMult);
                        if (group.spawnInterval > 0f) yield return new WaitForSeconds(group.spawnInterval);
                    }
                }

                while (alive > 0) yield return null;

                yield return new WaitForSeconds(sequence.intermission);
            }
        }

        private void SpawnOne(EnemyGroup group, int index, float hpMultiplier, float speedMultiplier)
        {
            Rect bounds = playArea.Bounds;
            float anchorX = Mathf.Lerp(bounds.center.x, group.anchor >= 0f ? bounds.xMax : bounds.xMin,
                                       Mathf.Abs(group.anchor));

            float x;
            switch (group.formation)
            {
                case GroupFormation.Vee:
                    // 0 leads at the anchor; pairs step outward. Spawn order + interval
                    // stagger the entry heights, so the wedge reads on screen.
                    int side = index % 2 == 1 ? 1 : -1;
                    int step = (index + 1) / 2;
                    x = anchorX + side * step * group.spacing;
                    break;
                case GroupFormation.Column:
                    x = anchorX;
                    break;
                default: // Line
                    float t = (index + 0.5f) / group.count;
                    x = Mathf.Lerp(bounds.xMin, bounds.xMax, t);
                    break;
            }
            x = Mathf.Clamp(x, bounds.xMin, bounds.xMax);

            Enemy enemy = pool.Get();
            enemy.Spawn(group.type, new Vector2(x, playArea.SpawnLineY), speedMultiplier, hpMultiplier,
                        playArea.DespawnLineY);
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

        /// <summary>Resumes after a pause. The Wave counter is a field, so the sequence
        /// continues where it stopped instead of restarting at Wave 1.</summary>
        public void ResumeSpawning()
        {
            if (loop != null) return;
            loop = StartCoroutine(RunWaves());
        }
    }
}
