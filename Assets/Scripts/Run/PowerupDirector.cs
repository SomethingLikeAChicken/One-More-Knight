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

        [Header("Drops")]
        [Range(0f, 1f)] [SerializeField] private float dropChance = 0.05f;
        [Tooltip("Sprites indexed by PowerupType: Damage, MaxHp, MoveSpeed, FireRate.")]
        [SerializeField] private Sprite[] typeSprites = new Sprite[4];
        [Tooltip("Relative weights per PowerupType - hearts stay RARE (nerfed on live feedback).")]
        [SerializeField] private float[] typeWeights = { 1f, 0.12f, 1f, 1f };

        private System.Random rng;

        private void Awake()
        {
            rng = new System.Random(System.Environment.TickCount ^ 0x2b992ddf);
            if (waveSpawner != null) waveSpawner.EnemyKilled += OnEnemyKilled;
            if (bossDirector != null) bossDirector.BossDefeatedAt += OnBossDefeatedAt;
        }

        private void OnDestroy()
        {
            if (waveSpawner != null) waveSpawner.EnemyKilled -= OnEnemyKilled;
            if (bossDirector != null) bossDirector.BossDefeatedAt -= OnBossDefeatedAt;
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

            if (rng.NextDouble() < dropChance) Drop(position, RollType());
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
            float total = 0f;
            foreach (float w in typeWeights) total += w;
            double roll = rng.NextDouble() * total;
            for (int i = 0; i < typeWeights.Length; i++)
            {
                roll -= typeWeights[i];
                if (roll <= 0) return (PowerupType)i;
            }
            return PowerupType.Damage;
        }
    }
}
