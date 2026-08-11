using System;
using UnityEngine;
using OneMoreKnight.Combat.Patterns;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// One Boss state gated by an HP band (CONTEXT.md: Phase), with its own Patterns
    /// and movement feel. Ordered on <see cref="BossStats.phases"/>; the first entry
    /// should enter at fraction 1 (fight start).
    /// </summary>
    [Serializable]
    public class BossPhase
    {
        public string name = "Phase";
        [Tooltip("The Phase begins when the HP fraction drops to or below this. First Phase = 1.")]
        [Range(0f, 1f)] public float entersAtHpFraction = 1f;
        public AttackPattern[] patterns;
        [Min(0f)] public float hoverSpeedMultiplier = 1f;

        [Header("Visuals")]
        [Tooltip("Multiplied onto the Boss sprite for the whole Phase. White = unchanged.")]
        public Color tint = Color.white;
        [Tooltip("Entry pulse peak as a scale multiplier. 1 = no pulse.")]
        [Min(1f)] public float entryPulseScale = 1f;
        [Min(0.05f)] public float entryPulseDuration = 0.4f;

        [Header("Summons (null = the Phase summons nothing)")]
        [Tooltip("Enemy type this Phase calls in beside the Boss while it keeps firing.")]
        public EnemyStats summonType;
        [Min(1)] public int summonCount = 2;
        [Min(0.5f)] public float summonInterval = 5f;
    }

    /// <summary>
    /// Tuning for one Boss, held in an asset like <see cref="EnemyStats"/>. Shared and
    /// read-only at runtime (ADR-0003) — per-fight state lives on the <see cref="Boss"/>.
    /// This asset IS the per-Boss definition: PRD §5.3's "BossDefinition" folded in
    /// here rather than split across a second asset type (noted in issue #12).
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Boss Stats", fileName = "BossStats")]
    public class BossStats : ScriptableObject
    {
        [Min(1)] public int maxHealth = 60;
        [Min(0)] public int contactDamage = 1;
        [Min(0)] public int scoreReward = 1000;
        [Tooltip("Bestiary lore on the website (#46).")]
        [TextArea] public string description;
        [Tooltip("Bestiary tactics: how it behaves and how to beat it (#60).")]
        [TextArea] public string tactics;
        [Tooltip("Shown on the HUD name tag and the bestiary. Empty = asset name minus the Boss prefix.")]
        public string displayName;

        /// <summary>The name players see (#53).</summary>
        public string DisplayName => !string.IsNullOrEmpty(displayName) ? displayName
            : name.StartsWith("Boss") ? name.Substring(4) : name;
        [Tooltip("Rating against a stage's maximum difficulty - the boss-pool currency (issue #30).")]
        [Min(1)] public int difficulty = 1;

        [Header("Identity (applied to the one Boss prefab on Begin)")]
        public Sprite sprite;
        [Min(0.1f)] public float scale = 1f;

        [Header("Movement")]
        [Min(0f)] public float entrySpeed = 2f;
        [Min(0f)] public float hoverSpeed = 1.1f;
        [Min(0f)] public float hoverAmplitude = 2.4f;

        [Header("Lackeys (#81 - empty = fights alone)")]
        [Tooltip("Boss definitions spawned flanking this Boss; it is INVULNERABLE " +
                 "until every lackey falls. Lackey assets stay out of the progression " +
                 "pool - they exist only through this reference. Not recursive.")]
        public BossStats[] lackeys = new BossStats[0];

        [Header("Shield (#79 - 0 = no ward)")]
        [Tooltip("Ward points broken before HP takes damage. The HUD bar shows the " +
                 "ward in blue; phases wait for the break because the HP fraction " +
                 "does not move while the ward holds.")]
        [Min(0)] public int shieldHealth;
        [Tooltip("Sprite held in front of the Boss while the ward holds (Bulwark's " +
                 "slab is the house style). Tinted blue at runtime.")]
        public Sprite shieldSprite;

        [Header("Phases (ordered; empty = the runner's own serialized Patterns)")]
        public BossPhase[] phases = new BossPhase[0];
    }
}
