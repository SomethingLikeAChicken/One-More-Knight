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
            public float NextFireAt;      // next burst may start at this time
            public int BurstRemaining;    // emissions left in the running burst
            public float NextEmissionAt;  // next emission inside the running burst
            public int EmissionCount;     // total emissions fired - drives spiral rotation
            public float TelegraphUntil;  // > now while this slot is telegraphing
            public Vector2 RiftPoint;     // where a rift burst erupts from (#70)
            public bool RiftArmed;        // this slot fires from RiftPoint, not the actor
        }

        private System.Random riftRng;

        private readonly List<Emission> emissionBuffer = new List<Emission>(32);
        private BulletSpawner bulletSpawner;
        private Transform target;
        private LayerMask hitMask;
        private SlotState[] slots = new SlotState[0];
        private bool firing;
        private SpriteRenderer visual;
        private Color visualBaseColor;
        private int telegraphingSlots; // how many slots currently hold the pulse

        /// <summary>One-time wiring — scene infrastructure, not per-life state.</summary>
        public void Initialize(BulletSpawner spawner, LayerMask mask)
        {
            bulletSpawner = spawner;
            hitMask = mask;
        }

        /// <summary>Who AimedAtTarget points at. Null is fine — aimed falls back to Down.</summary>
        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>Per-actor damage multiplier on every emission (#127) — the Wave
        /// curve's lethality lever, applied here so the shared Pattern asset is never
        /// mutated (ADR-0003). Reset to 1 in <see cref="ResetState"/>: a recycled
        /// pooled actor must not inherit the previous life's scale, and actors that
        /// never scale (Boss, Hero) simply stay at 1.</summary>
        public float DamageScale { get; set; } = 1f;

        /// <summary>Swaps the Pattern set (e.g. an Enemy type's asset at spawn, or a
        /// Boss Phase change later) and resets all timing state.</summary>
        public void SetPatterns(params AttackPattern[] newPatterns)
        {
            patterns = newPatterns ?? new AttackPattern[0];
            ResetState();
        }

        /// <summary>Resets every per-life clock. Called on (re)spawn — a pooled actor
        /// recycled mid-burst or mid-telegraph must not inherit or truncate anything.</summary>
        public void ResetState()
        {
            if (slots.Length != patterns.Length) slots = new SlotState[patterns.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].NextFireAt = Time.time + (patterns[i] != null ? patterns[i].initialDelay : 0f);
                slots[i].BurstRemaining = 0;
                slots[i].NextEmissionAt = 0f;
                slots[i].EmissionCount = 0;
                slots[i].TelegraphUntil = 0f;
                slots[i].RiftArmed = false;
            }
            telegraphingSlots = 0;
            DamageScale = 1f;
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            firing = true;
        }

        /// <summary>Gates emission without touching timing state (e.g. a Boss still
        /// entering, or a Run that just ended).</summary>
        public void SetFiring(bool value)
        {
            firing = value;
            if (!value && telegraphingSlots > 0)
            {
                telegraphingSlots = 0;
                if (visual != null) visual.color = visualBaseColor;
            }
        }

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

                if (slots[i].TelegraphUntil > 0f)
                {
                    // Holding fire while the warning shows; the burst aims when it
                    // actually starts, so dodging during the telegraph is honest.
                    if (now < slots[i].TelegraphUntil) continue;
                    slots[i].TelegraphUntil = 0f;
                    if (!slots[i].RiftArmed)
                    {
                        // Rifts warn with their marker, not the actor pulse.
                        telegraphingSlots--;
                        if (telegraphingSlots == 0 && visual != null) visual.color = visualBaseColor;
                    }
                    StartBurst(pattern, ref slots[i], now);
                    continue;
                }

                if (now < slots[i].NextFireAt) continue;
                if (pattern.originMode != OriginMode.Muzzle)
                {
                    // Rift cast (#70): open the marker where the pattern will erupt
                    // and hold the burst until it resolves. The marker IS the warning.
                    slots[i].RiftPoint = PickRiftPoint(pattern);
                    slots[i].RiftArmed = true;
                    slots[i].TelegraphUntil = now + pattern.riftTelegraph;
                    RiftMarker.Spawn(slots[i].RiftPoint, pattern.riftTelegraph);
                    continue;
                }
                if (pattern.telegraphDuration > 0f)
                {
                    if (telegraphingSlots == 0 && visual != null) visualBaseColor = visual.color;
                    telegraphingSlots++;
                    slots[i].TelegraphUntil = now + pattern.telegraphDuration;
                    telegraphColor = pattern.telegraphColor;
                    continue;
                }
                StartBurst(pattern, ref slots[i], now);
            }

            if (telegraphingSlots > 0 && visual != null)
            {
                float t = Mathf.PingPong(Time.time * 7f, 1f);
                visual.color = Color.Lerp(visualBaseColor, telegraphColor, t);
            }
        }

        private Color telegraphColor = Color.white;

        /// <summary>Wave-modifier hook (#57): FRENZY scales every runner's cooldowns.
        /// The WaveSpawner sets it per wave and resets it to 1 when waves stop, so
        /// Boss fights (waves paused) always run unscaled.</summary>
        public static float GlobalCooldownScale = 1f;

        private void StartBurst(AttackPattern pattern, ref SlotState slot, float now)
        {
            slot.NextFireAt = now + pattern.cooldown * GlobalCooldownScale;
            Emit(pattern, ref slot);
            slot.BurstRemaining = pattern.burstCount - 1;
            slot.NextEmissionAt = now + pattern.burstSpacing;
        }

        /// <summary>Motion is colour-coded (#48): the tint tells the player how the
        /// Bullet MOVES, engine-enforced so no asset can break the code. Green flies
        /// straight, violet snakes, red hunts. Authored bulletTint no longer applies;
        /// hero Bullets bypass the runner entirely and keep gold.</summary>
        private static readonly Color LinearTint = new Color(0.45f, 1f, 0.4f);
        private static readonly Color SineTint = new Color(0.8f, 0.45f, 1f);
        private static readonly Color HomingTint = new Color(1f, 0.3f, 0.32f);

        /// <summary>Where a rift opens: at the target's position at cast time
        /// (dodgeable — it does not follow), or a random point in the upper field.</summary>
        private Vector2 PickRiftPoint(AttackPattern pattern)
        {
            if (pattern.originMode == OriginMode.RiftAtTarget && target != null)
                return target.position;

            if (riftRng == null) riftRng = new System.Random(gameObject.GetHashCode() ^ System.Environment.TickCount);
            var cam = Camera.main;
            if (cam == null) return (Vector2)transform.position + Vector2.down * 3f;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector2 c = cam.transform.position;
            float x = c.x + ((float)riftRng.NextDouble() * 2f - 1f) * (halfW - 1.2f);
            float y = c.y + ((float)riftRng.NextDouble() * 0.55f + 0.05f) * halfH;
            return new Vector2(x, y);
        }

        private void Emit(AttackPattern pattern, ref SlotState slot)
        {
            Vector2? targetPos = target != null ? (Vector2?)target.position : null;
            float spin = pattern.angleStepPerEmission * slot.EmissionCount;
            slot.EmissionCount++;
            Vector2 source = slot.RiftArmed && pattern.originMode != OriginMode.Muzzle
                ? slot.RiftPoint : (Vector2)transform.position;
            AttackPatternEngine.ComputeEmission(pattern, source, targetPos, emissionBuffer, spin);

            BulletMotion motion;
            Color tint;
            switch (pattern.motion)
            {
                case MotionType.Sine:
                    motion = BulletMotion.Sine(pattern.sineAmplitude, pattern.sineFrequency, pattern.acceleration);
                    tint = SineTint;
                    break;
                case MotionType.Homing:
                    motion = BulletMotion.Homing(target, pattern.homingTurnSpeed, pattern.homingDuration, pattern.acceleration);
                    tint = HomingTint;
                    break;
                default:
                    motion = BulletMotion.Linear(pattern.acceleration);
                    tint = LinearTint;
                    break;
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(pattern.bulletDamage * DamageScale));
            for (int i = 0; i < emissionBuffer.Count; i++)
            {
                Emission e = emissionBuffer[i];
                bulletSpawner.Spawn(e.Origin, e.Direction, pattern.bulletSpeed, damage,
                                    hitMask, tint, motion);
            }
        }
    }
}
