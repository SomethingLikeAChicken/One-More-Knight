using System;
using UnityEngine;

namespace OneMoreKnight.Run
{
    /// <summary>Offer slot a Pact occupies. The LeaderboardScore multiplier is bound
    /// to the tier, never the individual Pact (#117 §9.2) — randomness picks the
    /// flavour of difficulty, not the ceiling.</summary>
    public enum PactTier
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>What a Pact actually changes in the Run. Every effect reaches the
    /// game as a runtime multiplier or an explicit clock write — shared assets are
    /// never mutated (ADR-0003), and acceleration is an explicit effect, never a
    /// Score→Wave coupling (#117 §9.2).</summary>
    public enum PactEffectType
    {
        /// <summary>Multiplies Enemy move speed on spawn.</summary>
        EnemySpeedScale,
        /// <summary>Multiplies Enemy HP on spawn.</summary>
        EnemyHpScale,
        /// <summary>Multiplies Enemy contact + Bullet damage on spawn (#127 pipeline).</summary>
        EnemyDamageScale,
        /// <summary>Multiplies the Wave pacing delays (delayBetweenGroups,
        /// intermission, slot spawnInterval). Floored by the spawner so the layering
        /// guarantee survives.</summary>
        SpawnCadenceScale,
        /// <summary>One-time forward jump of the Wave clock on activation
        /// (effectiveWave += value). Never rewound when the Pact ends.</summary>
        WaveOffset
    }

    [Serializable]
    public struct PactEffect
    {
        public PactEffectType type;
        [Tooltip("Multiplier for the Scale types; whole waves for WaveOffset.")]
        public float value;
    }

    /// <summary>
    /// One Pact (#129, CONTEXT.md): player-chosen difficulty bought for a
    /// LeaderboardScore multiplier. Exactly one is active at a time; offers appear
    /// at Run start and each Boss kill, one candidate per tier. Shared, read-only
    /// asset (ADR-0003) — all runtime state lives on the <see cref="PactDirector"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Pact", fileName = "Pact")]
    public class Pact : ScriptableObject
    {
        [Tooltip("Shown on the offer card and the HUD.")]
        public string displayName = "Pact";
        [Tooltip("One line on the offer card: what the bargain costs the player.")]
        [TextArea(2, 3)] public string description = "";
        public PactTier tier = PactTier.Easy;
        public PactEffect[] effects = new PactEffect[0];
    }
}
