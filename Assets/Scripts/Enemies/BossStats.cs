using UnityEngine;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// Tuning for one Boss, held in an asset like <see cref="EnemyStats"/>. Shared and
    /// read-only at runtime (ADR-0003) — per-fight state lives on the <see cref="Boss"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Boss Stats", fileName = "BossStats")]
    public class BossStats : ScriptableObject
    {
        [Min(1)] public int maxHealth = 60;
        [Min(0)] public int contactDamage = 1;
        [Min(0)] public int scoreReward = 1000;

        [Header("Movement")]
        [Min(0f)] public float entrySpeed = 2f;
        [Min(0f)] public float hoverSpeed = 1.1f;
        [Min(0f)] public float hoverAmplitude = 2.4f;
    }
}
