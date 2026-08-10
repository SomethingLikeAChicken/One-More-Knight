using UnityEngine;

namespace OneMoreKnight.Combat.Patterns
{
    public enum Formation
    {
        Single,
        Arc,
        Circle,
        /// <summary>Side by side, perpendicular to the fire direction — a sweeping wall.</summary>
        Wall
    }

    public enum DirectionMode
    {
        Down,
        Fixed,
        AimedAtTarget,
        Radial
    }

    public enum MotionType
    {
        Linear,
        /// <summary>Snakes along the travel direction (amplitude + frequency).</summary>
        Sine,
        /// <summary>Steers toward the target for a limited duration, then flies straight.</summary>
        Homing
    }

    public enum OriginMode
    {
        /// <summary>Bullets from the actor itself (default).</summary>
        Muzzle,
        /// <summary>A telegraphed RIFT opens at the target's position; the pattern
        /// erupts from there (#70).</summary>
        RiftAtTarget,
        /// <summary>A telegraphed RIFT opens at a random point in the upper field.</summary>
        RiftRandom
    }

    /// <summary>
    /// One Attack Pattern (CONTEXT.md), composed of the six ADR-0003 axes:
    /// Origin / Formation / Direction / Motion / Timing / Bullet.
    ///
    /// A <b>shared, read-only description</b> of an attack — it must never hold the
    /// runtime state of an actor firing it (cooldown clocks, burst counters live on
    /// <see cref="AttackPatternRunner"/>). The same asset works for an Enemy or a
    /// Boss: pattern code only ever sees a source and an optional target.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Attack Pattern", fileName = "AttackPattern")]
    public class AttackPattern : ScriptableObject
    {
        [Header("Origin")]
        [Tooltip("Distance from the source's centre along each Bullet's own direction.")]
        [Min(0f)] public float muzzleOffset = 0.35f;
        [Tooltip("Rift modes open a telegraphed square elsewhere and erupt from it (#70).")]
        public OriginMode originMode = OriginMode.Muzzle;
        [Tooltip("Rift only: seconds the marker warns before the eruption.")]
        [Min(0.2f)] public float riftTelegraph = 0.8f;

        [Header("Formation")]
        public Formation formation = Formation.Single;
        [Tooltip("Bullets per emission (Arc, Circle, Wall).")]
        [Min(1)] public int bulletCount = 1;
        [Tooltip("Total spread of an Arc in degrees, centred on the base direction.")]
        [Range(0f, 360f)] public float spreadAngle = 60f;
        [Tooltip("Wall only: world units between neighbouring Bullets.")]
        [Min(0.1f)] public float wallSpacing = 0.8f;

        [Header("Direction")]
        public DirectionMode direction = DirectionMode.Down;
        [Tooltip("Angle in degrees for Fixed direction. 0 = down, counter-clockwise.")]
        public float fixedAngle;

        [Header("Motion")]
        public MotionType motion = MotionType.Linear;
        [Tooltip("Sine only: sideways amplitude in world units.")]
        [Min(0f)] public float sineAmplitude = 0.6f;
        [Tooltip("Sine only: oscillations in radians per second.")]
        [Min(0f)] public float sineFrequency = 6f;
        [Tooltip("Homing only: degrees per second the Bullet can turn toward the target.")]
        [Min(0f)] public float homingTurnSpeed = 120f;
        [Tooltip("Homing only: steering stops after this many seconds (fairness cap).")]
        [Min(0f)] public float homingDuration = 1.5f;

        [Header("Timing")]
        [Min(0.05f)] public float cooldown = 3f;
        [Min(0f)] public float initialDelay = 1f;
        [Tooltip("Emissions per burst; 1 = a single emission per cooldown.")]
        [Min(1)] public int burstCount = 1;
        [Tooltip("Seconds between emissions inside one burst.")]
        [Min(0.02f)] public float burstSpacing = 0.2f;
        [Tooltip("Rotates the base direction by this many degrees per emission - spirals and rotating fans. 0 = off.")]
        public float angleStepPerEmission;

        [Header("Telegraph (CONTEXT.md: the warning before a strong attack)")]
        [Tooltip("Seconds the actor pulses before the burst fires. 0 = no telegraph.")]
        [Min(0f)] public float telegraphDuration;
        public Color telegraphColor = Color.white;

        [Header("Bullet")]
        [Min(0.1f)] public float bulletSpeed = 3.5f;
        [Min(1)] public int bulletDamage = 1;
        [Tooltip("Speed change per second over the Bullet's lifetime. Negative = decelerating.")]
        public float acceleration;
        [Tooltip("Superseded by motion colour-coding (#48): the runner tints by motion " +
                 "(green straight, violet sine, red homing). Kept for tooling/tests that " +
                 "spawn outside the runner.")]
        public Color bulletTint = new Color(1f, 0.32f, 0.38f);
    }
}
