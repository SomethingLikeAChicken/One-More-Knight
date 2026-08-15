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

        /// <summary>A kill happened — type + where. The spoils system (#55) listens.</summary>
        public event System.Action<EnemyStats, Vector2> EnemyKilled;

        /// <summary>Test surface (#57): forces the next waves' modifier roll.</summary>
        public WaveModifier? TestForceModifier { get; set; }

        /// <summary>The running wave's modifier (#57).</summary>
        public WaveModifier CurrentModifier { get; private set; } = WaveModifier.None;

        [Header("Wave modifiers (#57)")]
        [SerializeField] [Min(2)] private int modifiersFromWave = 16;
        [Range(0f, 1f)] [SerializeField] private float modifierChance = 0.45f;

        // Pact hooks (#129): runtime multipliers the PactDirector sets on top of the
        // wave curve, applied at the next wave's spawn computation. All default 1.
        public float PactSpeedScale { get; set; } = 1f;
        public float PactHpScale { get; set; } = 1f;
        public float PactDamageScale { get; set; } = 1f;

        private float pactCadenceScale = 1f;

        /// <summary>Scales every pacing delay (group layering, intermission, slot
        /// spawn intervals). Clamped to [0.5, 1]: at 0.5 the 2.2 s layering
        /// guarantee still reads as 1.1 s between groups — the floor the #117 B
        /// constraint demands.</summary>
        public float PactCadenceScale
        {
            get => pactCadenceScale;
            set => pactCadenceScale = Mathf.Clamp(value, 0.5f, 1f);
        }

        /// <summary>Explicit forward jump of the Wave clock (#129 WaveOffset) —
        /// acceleration is a Pact effect, never a Score coupling (#117 §9.2).</summary>
        public void AdvanceWaves(int count) => waveNumber += Mathf.Max(0, count);

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
                CurrentModifier = RollModifier(waveNumber);
                runManager.ReportWave(waveNumber,
                    CurrentModifier == WaveModifier.None ? "" : CurrentModifier.ToString().ToUpperInvariant());
                // The bestiary's modifier encyclopedia unlocks on first sight (#72).
                if (CurrentModifier != WaveModifier.None)
                    Run.Scoring.EncounterReporter.Report("Mod" + CurrentModifier);
                progression.MultipliersFor(waveNumber, out float hpMult, out float speedMult,
                                           out float damageMult);

                // Modifier levers (#57) - spikes on top of the bounded curve.
                if (CurrentModifier == WaveModifier.Haste) speedMult *= 1.25f;
                if (CurrentModifier == WaveModifier.Ironclad) hpMult *= 1.35f;
                // Pact levers (#129) - the chosen bargain, applied the same way.
                speedMult *= PactSpeedScale;
                hpMult *= PactHpScale;
                damageMult *= PactDamageScale;
                runManager.WaveScoreMultiplier = CurrentModifier == WaveModifier.Gilded ? 1.5f : 1f;
                Combat.Patterns.AttackPatternRunner.GlobalCooldownScale =
                    CurrentModifier == WaveModifier.Frenzy ? 0.75f : 1f;

                Compose(waveNumber);
                for (int g = 0; g < plan.Count; g++)
                {
                    if (g > 0) yield return new WaitForSeconds(progression.delayBetweenGroups * pactCadenceScale);
                    yield return SpawnGroup(plan[g], hpMult, speedMult, damageMult);
                }

                while (alive > 0) yield return null;

                yield return new WaitForSeconds(progression.intermission * pactCadenceScale);
            }
        }

        /// <summary>Spends the wave's budget on random eligible groups. Greedy but
        /// random: shuffle-pick anything affordable until nothing fits.</summary>
        private WaveModifier RollModifier(int wave)
        {
            if (wave < modifiersFromWave) return WaveModifier.None;
            if (TestForceModifier.HasValue) return TestForceModifier.Value;
            if (rng.NextDouble() >= modifierChance) return WaveModifier.None;
            return (WaveModifier)rng.Next(1, 6); // Haste..Swarm
        }

        private void Compose(int wave)
        {
            plan.Clear();
            int budget = progression.BudgetFor(wave);
            if (CurrentModifier == WaveModifier.Swarm)
                budget = Mathf.RoundToInt(budget * 1.3f);

            ComposeWithFloor(wave, budget, progression.CostFloorFor(wave));
            if (plan.Count == 0 && progression.pool.Length > 0)
            {
                // A floor above every pool cost would compose empty waves forever -
                // impossible by authoring rule, guarded anyway (#125).
                Debug.LogWarning("WaveSpawner: cost floor " + progression.CostFloorFor(wave)
                    + " starves the pool at wave " + wave + " - composing without the floor");
                ComposeWithFloor(wave, budget, 0);
            }
        }

        /// <summary>One composition pass. The floor (#125) retires groups cheaper
        /// than it, so a late wave spends its budget on few, hard groups and STOPS
        /// once nothing at or above the floor fits - the unspent remainder is the
        /// point (fewer, harder enemies), never topped back up with cheap filler.</summary>
        private void ComposeWithFloor(int wave, int remaining, int costFloor)
        {
            while (true)
            {
                eligible.Clear();
                foreach (GroupDefinition g in progression.pool)
                    if (g != null && g.minWave <= wave
                        && g.difficulty >= costFloor && g.difficulty <= remaining)
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

        private IEnumerator SpawnGroup(GroupDefinition group, float hpMultiplier, float speedMultiplier,
                                       float damageMultiplier)
        {
            foreach (GroupSlot slot in group.slots)
            {
                if (slot.type == null) continue;
                if (slot.delayBeforeSlot > 0f)
                    yield return new WaitForSeconds(slot.delayBeforeSlot * pactCadenceScale);

                for (int i = 0; i < slot.count; i++)
                {
                    SpawnOne(slot, i, hpMultiplier, speedMultiplier, damageMultiplier);
                    if (slot.spawnInterval > 0f)
                        yield return new WaitForSeconds(slot.spawnInterval * pactCadenceScale);
                }
            }
        }

        private void SpawnOne(GroupSlot slot, int index, float hpMultiplier, float speedMultiplier,
                              float damageMultiplier)
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
                        damageMultiplier, playArea.DespawnLineY);
            alive++;
        }

        /// <summary>Spawns one Enemy outside the Wave plan — the Boss summon seam.
        /// Same pool, scoring, and despawn wiring as Wave spawns; no Wave multipliers,
        /// and it counts toward <c>alive</c> so a resumed Wave still waits for it.</summary>
        public void SpawnReinforcement(EnemyStats type, Vector2 position)
        {
            if (type == null) return;
            Rect bounds = playArea.Bounds;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);

            Enemy enemy = pool.Get();
            enemy.Spawn(type, position, 1f, 1f, 1f, playArea.DespawnLineY);
            alive++;
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            // Raw points; the Gilded lever lives on RunManager.WaveScoreMultiplier and
            // only inflates LeaderboardScore - the pacing clock stays honest (#123).
            runManager.AddScore(enemy.Stats.scoreValue);
            EnemyKilled?.Invoke(enemy.Stats, enemy.transform.position);
        }

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
            // Bosses fight unmodified (#57) and pay unmultiplied (#123).
            Combat.Patterns.AttackPatternRunner.GlobalCooldownScale = 1f;
            runManager.WaveScoreMultiplier = 1f;
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
