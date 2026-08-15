using UnityEngine;
using OneMoreKnight.Combat;
using OneMoreKnight.Enemies;
using OneMoreKnight.Hero;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// The spoils system (#55): rolls powerup drops on Enemy kills (owned rng —
    /// ADR-0005 seam), guarantees one on a Boss kill, and applies death-curses
    /// when a cursed Enemy dies. One place, so drop balance is one screw.
    /// </summary>
    public class PowerupDirector : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private Pickup pickupPrefab;
        [SerializeField] private HeroUpgrades heroUpgrades;
        [SerializeField] private LayerMask heroMask;
        [SerializeField] private RunManager runManager;
        [SerializeField] private BulletSpawner bulletSpawner;

        [Header("Drops")]
        [Range(0f, 1f)] [SerializeField] private float dropChance = 0.05f;
        [Tooltip("Sprites indexed by PowerupType: Damage, MaxHp, MoveSpeed, FireRate, Aegis, Purge.")]
        [SerializeField] private Sprite[] typeSprites = new Sprite[6];
        [Tooltip("Relative weights per PowerupType - hearts stay RARE (nerfed on live feedback), " +
                 "the defensive pair (#83) rarer than swords.")]
        [SerializeField] private float[] typeWeights = { 1f, 0.12f, 1f, 1f, 0.3f, 0.25f };
        [Tooltip("Aegis/Purge only roll from this wave (#83) - the early game does not need them.")]
        [SerializeField] [Min(1)] private int lateTypesMinWave = 12;
        [Tooltip("Bosses at or above this difficulty drop one powerup per phase entered " +
                 "(#95) - long endgame fights feed the player instead of just eating time.")]
        [SerializeField] [Min(1)] private int bossPhaseDropMinDifficulty = 8;

        private System.Random rng;

        /// <summary>Pact hook (#135): scales the CHANCE drop roll only — guaranteed
        /// miniboss and Boss-phase drops always pay. Set/reset by the PactDirector.</summary>
        public float PactDropScale { get; set; } = 1f;

        private void Awake()
        {
            rng = new System.Random(System.Environment.TickCount ^ 0x2b992ddf);
            if (waveSpawner != null) waveSpawner.EnemyKilled += OnEnemyKilled;
            if (bossDirector != null)
            {
                bossDirector.BossDefeatedAt += OnBossDefeatedAt;
                bossDirector.BossPhaseEntered += OnBossPhaseEntered;
            }
            if (heroUpgrades != null) heroUpgrades.PowerupApplied += OnPowerupApplied;
        }

        private void OnDestroy()
        {
            if (waveSpawner != null) waveSpawner.EnemyKilled -= OnEnemyKilled;
            if (bossDirector != null)
            {
                bossDirector.BossDefeatedAt -= OnBossDefeatedAt;
                bossDirector.BossPhaseEntered -= OnBossPhaseEntered;
            }
            if (heroUpgrades != null) heroUpgrades.PowerupApplied -= OnPowerupApplied;
        }

        /// <summary>Endgame pacing (#95): a d8+ boss pays one powerup per phase it
        /// enters (the first included), dropped just below it so the fight funds the
        /// dodging it demands.</summary>
        private void OnBossPhaseEntered(Enemies.Boss boss)
        {
            if (boss.Stats == null || boss.Stats.difficulty < bossPhaseDropMinDifficulty) return;
            Drop((Vector2)boss.transform.position + Vector2.down * 1.2f, RollType());
        }

        /// <summary>Purge (#83) is a world effect, so the spoils system executes it —
        /// HeroUpgrades stays hero-local.</summary>
        private void OnPowerupApplied(PowerupType type)
        {
            if (type == PowerupType.Purge && bulletSpawner != null)
                bulletSpawner.ClearThreatening(heroMask);
        }

        private void OnEnemyKilled(EnemyStats stats, Vector2 position)
        {
            // Death-curse first: dying is what cursed types are FOR (#55) - the
            // punishment lands whether or not loot rolls.
            if (stats.curseOnDeath != CurseType.None && heroUpgrades != null)
                heroUpgrades.ApplyCurse(stats.curseOnDeath);

            // Guaranteed carriers (#76) bypass the roll - a miniboss kill must pay out.
            if (stats.guaranteedDrop != GuaranteedDrop.None)
            {
                Drop(position, ToPowerupType(stats.guaranteedDrop));
                return;
            }

            if (rng.NextDouble() < dropChance * PactDropScale) Drop(position, RollType());
        }

        private void OnBossDefeatedAt(Vector2 position) => Drop(position, RollType());

        private void Drop(Vector2 position, PowerupType type)
        {
            var pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
            pickup.Arm(type, typeSprites[(int)type], playArea.DespawnLineY, heroMask);
        }

        private static PowerupType ToPowerupType(GuaranteedDrop drop)
        {
            switch (drop)
            {
                case GuaranteedDrop.MaxHp: return PowerupType.MaxHp;
                case GuaranteedDrop.MoveSpeed: return PowerupType.MoveSpeed;
                case GuaranteedDrop.FireRate: return PowerupType.FireRate;
                default: return PowerupType.Damage;
            }
        }

        private PowerupType RollType()
        {
            // The defensive pair (#83) joins the table late - early waves keep the
            // original four-type roll.
            int rollable = runManager != null && runManager.Wave >= lateTypesMinWave
                ? typeWeights.Length
                : Mathf.Min(typeWeights.Length, (int)PowerupType.Aegis);

            float total = 0f;
            for (int i = 0; i < rollable; i++) total += typeWeights[i];
            double roll = rng.NextDouble() * total;
            for (int i = 0; i < rollable; i++)
            {
                roll -= typeWeights[i];
                if (roll <= 0) return (PowerupType)i;
            }
            return PowerupType.Damage;
        }
    }
}
