using UnityEngine;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// Tuning for one Enemy type, held in an asset rather than as magic numbers inside a
    /// MonoBehaviour (AGENTS.md seam for M4).
    ///
    /// This is a <b>shared, read-only</b> asset. It describes an Enemy type; it must never
    /// hold the state of an Enemy currently alive in the Run (ADR-0003). Per-Wave scaling
    /// is passed to <see cref="Enemy.Spawn"/> as a multiplier — never written back here.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Min(1)] public int maxHealth = 3;
        [Min(0f)] public float moveSpeed = 2.2f;
        [Min(0)] public int scoreValue = 100;
        [Min(0)] public int contactDamage = 1;
    }
}
