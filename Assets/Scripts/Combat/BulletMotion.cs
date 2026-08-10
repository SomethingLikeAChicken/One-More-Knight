using UnityEngine;

namespace OneMoreKnight.Combat
{
    public enum BulletMotionType
    {
        Linear,
        Sine,
        Homing
    }

    /// <summary>
    /// Per-flight motion spec handed to a Bullet on spawn. A plain value bundle —
    /// the Bullet copies every field in Arm, so pooling can never leak motion state
    /// between flights (ADR-0003 invariant).
    /// </summary>
    public readonly struct BulletMotion
    {
        public readonly BulletMotionType Type;
        public readonly float SineAmplitude;
        public readonly float SineFrequency;
        public readonly float HomingTurnSpeed;
        public readonly float HomingDuration;
        public readonly float Acceleration;
        public readonly Transform Target;

        public static BulletMotion Linear(float acceleration = 0f)
            => new BulletMotion(BulletMotionType.Linear, 0f, 0f, 0f, 0f, acceleration, null);

        public static BulletMotion Sine(float amplitude, float frequency, float acceleration = 0f)
            => new BulletMotion(BulletMotionType.Sine, amplitude, frequency, 0f, 0f, acceleration, null);

        public static BulletMotion Homing(Transform target, float turnSpeed, float duration, float acceleration = 0f)
            => new BulletMotion(BulletMotionType.Homing, 0f, 0f, turnSpeed, duration, acceleration, target);

        private BulletMotion(BulletMotionType type, float sineAmplitude, float sineFrequency,
                             float homingTurnSpeed, float homingDuration, float acceleration, Transform target)
        {
            Type = type;
            SineAmplitude = sineAmplitude;
            SineFrequency = sineFrequency;
            HomingTurnSpeed = homingTurnSpeed;
            HomingDuration = homingDuration;
            Acceleration = acceleration;
            Target = target;
        }
    }
}
