using UnityEngine;

namespace OneMoreKnight.Combat.Patterns
{
    public enum Formation
    {
        Single,
        Arc,
        Circle
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
        Linear
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

        [Header("Formation")]
        public Formation formation = Formation.Single;
        [Tooltip("Bullets per emission (Arc and Circle).")]
        [Min(1)] public int bulletCount = 1;
        [Tooltip("Total spread of an Arc in degrees, centred on the base direction.")]
        [Range(0f, 360f)] public float spreadAngle = 60f;

        [Header("Direction")]
        public DirectionMode direction = DirectionMode.Down;
        [Tooltip("Angle in degrees for Fixed direction. 0 = down, counter-clockwise.")]
        public float fixedAngle;

        [Header("Motion")]
        public MotionType motion = MotionType.Linear;

        [Header("Timing")]
        [Min(0.05f)] public float cooldown = 3f;
        [Min(0f)] public float initialDelay = 1f;
        [Tooltip("Emissions per burst; 1 = a single emission per cooldown.")]
        [Min(1)] public int burstCount = 1;
        [Tooltip("Seconds between emissions inside one burst.")]
        [Min(0.02f)] public float burstSpacing = 0.2f;

        [Header("Bullet")]
        [Min(0.1f)] public float bulletSpeed = 3.5f;
        [Min(1)] public int bulletDamage = 1;
        [Tooltip("Readability rule: Enemy attacks are red/violet, never gold/blue.")]
        public Color bulletTint = new Color(1f, 0.32f, 0.38f);
    }
}
