using UnityEngine;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Flow;
using OneMoreKnight.Run.Scoring;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// The bestiary's practice room (#46): duels the Hero against exactly one
    /// Enemy or Boss type from the <see cref="ArenaCatalog"/>, forever. The type
    /// comes from the hosting page's URL (<c>arena=&lt;Slug&gt;</c>); in the
    /// editor the serialized override is used instead. Encounters are suppressed
    /// here — the diary only unlocks from real Runs.
    /// </summary>
    public class ArenaController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private ArenaCatalog catalog;
        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private BulletSpawner bulletSpawner;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private Boss bossPrefab;
        [SerializeField] private Health heroHealth;

        [Header("Tuning")]
        [SerializeField] [Min(1)] private int heroArenaHealth = 20;
        [SerializeField] [Min(0.2f)] private float respawnDelay = 1.4f;

        [Header("Editor testing (ignored in a browser)")]
        [SerializeField] private string editorOverrideTarget = "Descender";

        private EnemyStats enemyTarget;
        private BossStats bossTarget;
        private Boss activeBoss;
        private float nextSpawnAt;

        private void Start()
        {
            EncounterReporter.Suppressed = true;
            heroHealth.ResetHealth(heroArenaHealth);

            string slug = SceneFlow.ArenaTargetFromUrl();
            if (string.IsNullOrEmpty(slug)) slug = editorOverrideTarget;
            enemyTarget = catalog.FindEnemy(slug);
            if (enemyTarget == null) bossTarget = catalog.FindBoss(slug);
            if (enemyTarget == null && bossTarget == null)
            {
                Debug.LogWarning($"Arena: unknown target '{slug}', falling back to the first enemy.");
                enemyTarget = catalog.enemies.Length > 0 ? catalog.enemies[0] : null;
            }
            nextSpawnAt = Time.time + 0.8f;
        }

        private void OnDestroy()
        {
            EncounterReporter.Suppressed = false;
            if (activeBoss != null) activeBoss.Defeated -= OnBossDefeated;
        }

        private void Update()
        {
            // Bottomless practice health - the Arena is for studying, not surviving.
            if (heroHealth.Current < heroArenaHealth / 2) heroHealth.ResetHealth(heroArenaHealth);

            if (Time.time < nextSpawnAt) return;

            if (bossTarget != null)
            {
                if (activeBoss == null) SpawnBoss();
                return;
            }
            if (enemyTarget != null && !AnyEnemyAlive())
            {
                Rect bounds = playArea.Bounds;
                spawner.SpawnReinforcement(enemyTarget, new Vector2(bounds.center.x, playArea.SpawnLineY));
                nextSpawnAt = Time.time + respawnDelay;
            }
        }

        private bool AnyEnemyAlive()
        {
            foreach (var e in FindObjectsByType<Enemy>())
                if (e.gameObject.activeInHierarchy) return true;
            return false;
        }

        private void SpawnBoss()
        {
            Rect bounds = playArea.Bounds;
            var spawn = new Vector2(bounds.center.x, playArea.SpawnLineY);
            activeBoss = Instantiate(bossPrefab, spawn, Quaternion.identity);
            activeBoss.Defeated += OnBossDefeated;
            var hero = heroHealth.transform;
            activeBoss.Begin(bossTarget, spawn, bounds.yMax - 1.6f, bulletSpawner, hero, spawner);
        }

        private void OnBossDefeated(Boss boss)
        {
            boss.Defeated -= OnBossDefeated;
            Destroy(boss.gameObject);
            activeBoss = null;
            nextSpawnAt = Time.time + respawnDelay;
        }
    }
}
