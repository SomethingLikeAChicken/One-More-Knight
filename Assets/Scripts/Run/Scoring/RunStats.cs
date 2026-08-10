using System.Collections.Generic;
using System.Text;
using UnityEngine;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Hero;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run.Scoring
{
    /// <summary>
    /// Per-Run statistics for the achievement system (#63) and richer ADR-0005 run
    /// summaries: kills by type, damage taken, first-damage wave, powerups, curses.
    /// Scene-lifetime state — a fresh Run resets by construction.
    /// </summary>
    public class RunStats : MonoBehaviour
    {
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private RunManager runManager;
        [SerializeField] private Health heroHealth;
        [SerializeField] private HeroUpgrades heroUpgrades;

        private readonly Dictionary<string, int> kills = new Dictionary<string, int>(32);
        private int damageTaken;
        private int firstHitWave = -1; // -1 = never hit
        private int powerupsCollected;
        private int cursesSuffered;
        private int lastHp = int.MaxValue;

        private void Awake()
        {
            if (waveSpawner != null) waveSpawner.EnemyKilled += OnEnemyKilled;
            if (heroHealth != null) heroHealth.Changed += OnHeroHealthChanged;
            if (heroUpgrades != null)
            {
                heroUpgrades.PowerupApplied += OnPowerupApplied;
                heroUpgrades.CurseApplied += OnCurseApplied;
            }
        }

        private void OnDestroy()
        {
            if (waveSpawner != null) waveSpawner.EnemyKilled -= OnEnemyKilled;
            if (heroHealth != null) heroHealth.Changed -= OnHeroHealthChanged;
            if (heroUpgrades != null)
            {
                heroUpgrades.PowerupApplied -= OnPowerupApplied;
                heroUpgrades.CurseApplied -= OnCurseApplied;
            }
        }

        private void OnEnemyKilled(EnemyStats stats, Vector2 _)
        {
            kills[stats.name] = kills.TryGetValue(stats.name, out int c) ? c + 1 : 1;
        }

        private void OnHeroHealthChanged(Health health)
        {
            if (lastHp == int.MaxValue)
            {
                // First event is the spawn-time sync, not damage.
                lastHp = health.Current;
                return;
            }
            if (health.Current < lastHp)
            {
                damageTaken += lastHp - health.Current;
                if (firstHitWave < 0 && runManager != null) firstHitWave = runManager.Wave;
            }
            lastHp = health.Current;
        }

        private void OnPowerupApplied(PowerupType _) => powerupsCollected++;
        private void OnCurseApplied(CurseType _) => cursesSuffered++;

        /// <summary>The submission meta (#63): everything the site's achievement
        /// evaluation needs, hand-built JSON like the rest of the bridge layer.</summary>
        public string ToMetaJson()
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"wave\":").Append(runManager != null ? runManager.Wave : 0)
              .Append(",\"bosses\":").Append(bossDirector != null ? bossDirector.BossesDefeated : 0)
              .Append(",\"dmg\":").Append(damageTaken)
              .Append(",\"firstHitWave\":").Append(firstHitWave)
              .Append(",\"powerups\":").Append(powerupsCollected)
              .Append(",\"curses\":").Append(cursesSuffered)
              .Append(",\"kills\":{");
            bool first = true;
            foreach (var kv in kills)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(kv.Key).Append("\":").Append(kv.Value);
            }
            sb.Append("}}");
            return sb.ToString();
        }
    }
}
