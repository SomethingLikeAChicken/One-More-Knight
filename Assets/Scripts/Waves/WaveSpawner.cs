using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Hero;
using OneMoreKnight.Run;

namespace OneMoreKnight.Waves
{
    /// <summary>
    /// Composes each Wave from the <see cref="DifficultyProgression"/>'s group pool
    /// (issue #24): the wave's budget is spent on random eligible groups, spawned
    /// sequentially with a layering delay. The difficulty curve is fixed; the
    /// composition is not — logical randomness.
    ///
    /// Randomness draws from an <b>owned System.Random seeded per Run</b> — the
    /// ADR-0005 seam; never UnityEngine.Random. The Wave loop is a coroutine on a
    /// scene object that is never deactivated (AGENTS.md gotcha).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private RunManager runManager;
        [SerializeField] private BulletSpawner bulletSpawner;
        [SerializeField] private DifficultyProgression progression;

        private ObjectPool<Enemy> pool;
        private Coroutine loop;
        private Transform target;
        private System.Random rng;
        private readonly List<GroupDefinition> plan = new List<GroupDefinition>(8);
        private readonly List<GroupDefinition> eligible = new List<GroupDefinition>(16);
        private int alive;
        private int waveNumber;

        /// <summary>The groups composed for the current Wave — read-only test surface.</summary>
        public string LastWavePlan { get; private set; } = "";

        private void Awake()
        {
            // Per-Run seed. When ADR-0005's Run Summary lands, this seed is what gets
            // recorded; System.Environment keeps it off UnityEngine.Random entirely.
            rng = new System.Random(System.Environment.TickCount);

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
            var hero = FindAnyObjectByType<HeroController>();
            target = hero != null ? hero.transform : null;
            loop = StartCoroutine(RunWaves());
        }

        private Enemy CreateEnemy()
        {
            Enemy enemy = Instantiate(enemyPrefab, transform);
            enemy.Killed += OnEnemyKilled;
            enemy.Retired += OnEnemyRetired;
            enemy.Initialize(bulletSpawner, target);
            return enemy;
        }

        private IEnumerator RunWaves()
        {
            while (true)
            {
                waveNumber++;
                runManager.ReportWave(waveNumber);
                progression.MultipliersFor(waveNumber, out float hpMult, out float speedMult);

                Compose(waveNumber);
                for (int g = 0; g < plan.Count; g++)
                {
                    if (g > 0) yield return new WaitForSeconds(progression.delayBetweenGroups);
                    yield return SpawnGroup(plan[g], hpMult, speedMult);
                }

                while (alive > 0) yield return null;

                yield return new WaitForSeconds(progression.intermission);
            }
        }

        /// <summary>Spends the wave's budget on random eligible groups. Greedy but
        /// random: shuffle-pick anything affordable until nothing fits.</summary>
        private void Compose(int wave)
        {
            plan.Clear();
            int remaining = progression.BudgetFor(wave);

            while (true)
            {
                eligible.Clear();
                foreach (GroupDefinition g in progression.pool)
                    if (g != null && g.minWave <= wave && g.difficulty <= remaining)
                        eligible.Add(g);
                if (eligible.Count == 0) break;

                GroupDefinition pick = eligible[rng.Next(eligible.Count)];
                plan.Add(pick);
                remaining -= pick.difficulty;
            }

            var names = new System.Text.StringBuilder();
            foreach (GroupDefinition g in plan) names.Append(g.name).Append('(').Append(g.difficulty).Append(") ");
            LastWavePlan = "wave " + wave + " budget " + progression.BudgetFor(wave) + ": " + names;
        }

        private IEnumerator SpawnGroup(GroupDefinition group, float hpMultiplier, float speedMultiplier)
        {
            foreach (GroupSlot slot in group.slots)
            {
                if (slot.type == null) continue;
                if (slot.delayBeforeSlot > 0f) yield return new WaitForSeconds(slot.delayBeforeSlot);

                for (int i = 0; i < slot.count; i++)
                {
                    SpawnOne(slot, i, hpMultiplier, speedMultiplier);
                    if (slot.spawnInterval > 0f) yield return new WaitForSeconds(slot.spawnInterval);
                }
            }
        }

        private void SpawnOne(GroupSlot slot, int index, float hpMultiplier, float speedMultiplier)
        {
            Rect bounds = playArea.Bounds;
            float anchorX = Mathf.Lerp(bounds.center.x, slot.anchor >= 0f ? bounds.xMax : bounds.xMin,
                                       Mathf.Abs(slot.anchor));

            float x;
            switch (slot.formation)
            {
                case GroupFormation.Vee:
                    int side = index % 2 == 1 ? 1 : -1;
                    int step = (index + 1) / 2;
                    x = anchorX + side * step * slot.spacing;
                    break;
                case GroupFormation.Column:
                    x = anchorX;
                    break;
                default:
                    float t = (index + 0.5f) / slot.count;
                    x = Mathf.Lerp(bounds.xMin, bounds.xMax, t);
                    break;
            }
            x = Mathf.Clamp(x, bounds.xMin, bounds.xMax);

            Enemy enemy = pool.Get();
            enemy.Spawn(slot.type, new Vector2(x, playArea.SpawnLineY), speedMultiplier, hpMultiplier,
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

        /// <summary>Resumes after a pause. The Wave counter is a field, so the curve
        /// continues where it stopped instead of restarting at Wave 1.</summary>
        public void ResumeSpawning()
        {
            if (loop != null) return;
            loop = StartCoroutine(RunWaves());
        }
    }
}
