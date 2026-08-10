using System.Collections.Generic;
using UnityEngine;

namespace OneMoreKnight.Combat.Patterns
{
    /// <summary>
    /// The per-actor emitter: holds ALL runtime state for the Patterns its actor is
    /// firing (cooldown clocks, burst progress) — the asset itself stays stateless
    /// (ADR-0003). Actor-agnostic: it has a source (its own transform), an optional
    /// target, and a hit mask; it does not know what kind of Combat Actor it sits on.
    ///
    /// Tick-driven on purpose — <b>no coroutines</b>. Coroutines die silently when a
    /// pooled GameObject is deactivated, truncating bursts (ADR-0003 invariant);
    /// plain fields reset in <see cref="ResetState"/> survive pooling cleanly.
    /// </summary>
    public class AttackPatternRunner : MonoBehaviour
    {
        [SerializeField] private AttackPattern[] patterns = new AttackPattern[0];

        private struct SlotState
        {
            public float NextFireAt;     // next burst may start at this time
            public int BurstRemaining;   // emissions left in the running burst
            public float NextEmissionAt; // next emission inside the running burst
            public int EmissionCount;    // total emissions fired - drives spiral rotation
        }

        private readonly List<Emission> emissionBuffer = new List<Emission>(32);
        private BulletSpawner bulletSpawner;
        private Transform target;
        private LayerMask hitMask;
        private SlotState[] slots = new SlotState[0];
        private bool firing;

        /// <summary>One-time wiring — scene infrastructure, not per-life state.</summary>
        public void Initialize(BulletSpawner spawner, LayerMask mask)
        {
            bulletSpawner = spawner;
            hitMask = mask;
        }

        /// <summary>Who AimedAtTarget points at. Null is fine — aimed falls back to Down.</summary>
        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>Swaps the Pattern set (e.g. an Enemy type's asset at spawn, or a
        /// Boss Phase change later) and resets all timing state.</summary>
        public void SetPatterns(params AttackPattern[] newPatterns)
        {
            patterns = newPatterns ?? new AttackPattern[0];
            ResetState();
        }

        /// <summary>Resets every per-life clock. Called on (re)spawn — a pooled actor
        /// recycled mid-burst must not inherit or truncate anything.</summary>
        public void ResetState()
        {
            if (slots.Length != patterns.Length) slots = new SlotState[patterns.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].NextFireAt = Time.time + (patterns[i] != null ? patterns[i].initialDelay : 0f);
                slots[i].BurstRemaining = 0;
                slots[i].NextEmissionAt = 0f;
                slots[i].EmissionCount = 0;
            }
            firing = true;
        }

        /// <summary>Gates emission without touching timing state (e.g. a Boss still
        /// entering, or a Run that just ended).</summary>
        public void SetFiring(bool value) => firing = value;

        private void OnEnable()
        {
            // Pool reuse re-enables the object; state must already be fresh by the
            // time the first Update runs.
            ResetState();
        }

        private void Update()
        {
            if (!firing || bulletSpawner == null) return;

            float now = Time.time;
            for (int i = 0; i < slots.Length; i++)
            {
                AttackPattern pattern = patterns[i];
                if (pattern == null) continue;

                if (slots[i].BurstRemaining > 0)
                {
                    if (now < slots[i].NextEmissionAt) continue;
                    Emit(pattern, ref slots[i]);
                    slots[i].BurstRemaining--;
                    slots[i].NextEmissionAt = now + pattern.burstSpacing;
                    continue;
                }

                if (now < slots[i].NextFireAt) continue;
                slots[i].NextFireAt = now + pattern.cooldown;
                Emit(pattern, ref slots[i]);
                slots[i].BurstRemaining = pattern.burstCount - 1;
                slots[i].NextEmissionAt = now + pattern.burstSpacing;
            }
        }

        private void Emit(AttackPattern pattern, ref SlotState slot)
        {
            Vector2? targetPos = target != null ? (Vector2?)target.position : null;
            float spin = pattern.angleStepPerEmission * slot.EmissionCount;
            slot.EmissionCount++;
            AttackPatternEngine.ComputeEmission(pattern, transform.position, targetPos, emissionBuffer, spin);

            BulletMotion motion;
            switch (pattern.motion)
            {
                case MotionType.Sine:
                    motion = BulletMotion.Sine(pattern.sineAmplitude, pattern.sineFrequency, pattern.acceleration);
                    break;
                case MotionType.Homing:
                    motion = BulletMotion.Homing(target, pattern.homingTurnSpeed, pattern.homingDuration, pattern.acceleration);
                    break;
                default:
                    motion = BulletMotion.Linear(pattern.acceleration);
                    break;
            }

            for (int i = 0; i < emissionBuffer.Count; i++)
            {
                Emission e = emissionBuffer[i];
                bulletSpawner.Spawn(e.Origin, e.Direction, pattern.bulletSpeed, pattern.bulletDamage,
                                    hitMask, pattern.bulletTint, motion);
            }
        }
    }
}
