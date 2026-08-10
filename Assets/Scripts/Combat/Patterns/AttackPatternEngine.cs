using System.Collections.Generic;
using UnityEngine;

namespace OneMoreKnight.Combat.Patterns
{
    /// <summary>One computed spawn: where a Bullet starts and where it heads.</summary>
    public readonly struct Emission
    {
        public readonly Vector2 Origin;
        public readonly Vector2 Direction;

        public Emission(Vector2 origin, Vector2 direction)
        {
            Origin = origin;
            Direction = direction;
        }
    }

    /// <summary>
    /// The pattern engine (ADR-0003): pure math from a Pattern asset plus source /
    /// target positions to a list of spawns. Actor-agnostic — it never knows whether
    /// the source is a Hero, an Enemy, or a Boss — and free of scene dependencies, so
    /// EditMode tests can cover Formation and Direction geometry directly.
    ///
    /// Deterministic by construction: same pattern + same positions = same Bullets
    /// (ADR-0005 — variation, when it comes, draws from an owned seeded RNG).
    /// </summary>
    public static class AttackPatternEngine
    {
        /// <summary>Computes one emission of <paramref name="pattern"/>.
        /// <paramref name="target"/> is optional; AimedAtTarget falls back to Down.</summary>
        public static void ComputeEmission(AttackPattern pattern, Vector2 source, Vector2? target,
                                           List<Emission> results)
        {
            results.Clear();

            Vector2 baseDirection = BaseDirection(pattern, source, target);

            switch (pattern.formation)
            {
                case Formation.Single:
                    Add(results, pattern, source, baseDirection);
                    break;

                case Formation.Arc:
                {
                    int count = pattern.bulletCount;
                    if (count == 1)
                    {
                        Add(results, pattern, source, baseDirection);
                        break;
                    }
                    float step = pattern.spreadAngle / (count - 1);
                    float start = -pattern.spreadAngle * 0.5f;
                    for (int i = 0; i < count; i++)
                        Add(results, pattern, source, Rotate(baseDirection, start + step * i));
                    break;
                }

                case Formation.Circle:
                {
                    int count = pattern.bulletCount;
                    float step = 360f / count;
                    for (int i = 0; i < count; i++)
                        Add(results, pattern, source, Rotate(baseDirection, step * i));
                    break;
                }
            }
        }

        private static Vector2 BaseDirection(AttackPattern pattern, Vector2 source, Vector2? target)
        {
            switch (pattern.direction)
            {
                case DirectionMode.Fixed:
                    return Rotate(Vector2.down, pattern.fixedAngle);
                case DirectionMode.AimedAtTarget:
                    if (target.HasValue && (target.Value - source).sqrMagnitude > 0.0001f)
                        return (target.Value - source).normalized;
                    return Vector2.down;
                case DirectionMode.Radial:
                    // Radial spreads via the Formation offsets; the base only anchors bullet 0.
                    return Vector2.down;
                default:
                    return Vector2.down;
            }
        }

        private static void Add(List<Emission> results, AttackPattern pattern, Vector2 source, Vector2 direction)
        {
            results.Add(new Emission(source + direction * pattern.muzzleOffset, direction));
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
