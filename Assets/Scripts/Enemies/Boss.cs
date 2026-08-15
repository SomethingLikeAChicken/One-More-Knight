using System;
using UnityEngine;
using OneMoreKnight.Combat;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// The first Boss (CONTEXT.md: Boss) — a strong Enemy, not a separate concept: it
    /// reuses <see cref="Health"/>, the Hero-Bullet damage path, and the Enemy layer.
    ///
    /// Deliberately <b>hardcoded M3 shape</b> (ADR-0003 builds concrete first): enters
    /// from above to a hover line, then hovers side to side. Attack Patterns and the
    /// Phase change land in the next sub-issues; M4 abstracts all of it into the
    /// pattern engine. One Boss per Run — instantiated, not pooled.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Boss : MonoBehaviour
    {
        [SerializeField] private BossStats stats;
        [SerializeField] private Health health;
        [SerializeField] private LayerMask heroMask;
        [SerializeField] private Combat.Patterns.AttackPatternRunner attackRunner;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private CircleCollider2D circleCollider;
        private BossShield shieldVisual;
        private SpriteLoopAnimator idleAnimator;
        private Waves.WaveSpawner reinforcements;
        private float nextSummonAt;
        private float hoverLineY;
        private float anchorX;
        private float hoverT;
        private bool entering;
        private int currentPhase = -1;
        private Vector3 baseScale;
        private float pulseEndsAt;
        private float pulsePeak = 1f;
        private float pulseDuration = 1f;

        [Header("Breach (#141)")]
        [Tooltip("Incoming-damage multiplier while a Phase-transition breach is open.")]
        [SerializeField] [Min(1f)] private float breachMultiplier = 2f;
        [SerializeField] [Min(0f)] private float breachDuration = 4f;
        private float breachUntil;

        public BossStats Stats => stats;
        public Health Health => health;

        /// <summary>The guard is down (#141) — the HUD advertises the window.</summary>
        public bool BreachActive => Time.time < breachUntil && health != null && health.IsAlive;

        /// <summary>Index into <see cref="BossStats.phases"/>; -1 until the fight starts.</summary>
        public int CurrentPhase => currentPhase;

        /// <summary>Died to damage — the Run scores the reward and Waves resume.</summary>
        public event Action<Boss> Defeated;

        /// <summary>A Phase just began, including the first at fight start (#95) —
        /// the spoils system feeds long d8+ fights through this.</summary>
        public event Action<Boss, int> PhaseEntered;

        private void Reset() => health = GetComponent<Health>();

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            circleCollider = GetComponent<CircleCollider2D>();
            // Added here rather than on the prefab, like the Enemy (#116).
            idleAnimator = GetComponent<SpriteLoopAnimator>();
            if (idleAnimator == null) idleAnimator = gameObject.AddComponent<SpriteLoopAnimator>();
            baseScale = transform.localScale; // Begin overrides this per Boss
            health.Died += OnDied;
            health.Changed += OnHealthChanged;
            health.ShieldBroken += OnShieldBroken;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= OnDied;
                health.Changed -= OnHealthChanged;
                health.ShieldBroken -= OnShieldBroken;
            }
        }

        /// <summary>Starts the entrance as <paramref name="definition"/> — the one Boss
        /// prefab plays every Boss; identity is data, like the Enemy (#16). Descends
        /// from <paramref name="spawnPosition"/> to <paramref name="hoverY"/>, then
        /// hovers around the spawn X. Attacks come from the Phase Pattern assets and
        /// only start once the hover is reached — firing during the entrance would be
        /// an unfair off-screen attack.</summary>
        public void Begin(BossStats definition, Vector2 spawnPosition, float hoverY,
                          Combat.BulletSpawner bulletSpawner, Transform target,
                          Waves.WaveSpawner reinforcementSpawner = null)
        {
            reinforcements = reinforcementSpawner;
            stats = definition;
            transform.position = spawnPosition;
            anchorX = spawnPosition.x;
            hoverLineY = hoverY;
            hoverT = 0f;
            entering = true;
            currentPhase = -1;
            health.ResetHealth(stats.maxHealth);

            // The ward (#79): damage breaks it before HP moves; the visual shatters
            // via ShieldBroken. Phases wait naturally - the HP fraction is frozen.
            if (stats.shieldHealth > 0)
            {
                health.SetShield(stats.shieldHealth);
                shieldVisual = BossShield.Spawn(transform, stats.shieldSprite);
            }

            if (stats.sprite != null) spriteRenderer.sprite = stats.sprite;
            spriteRenderer.color = Color.white;
            idleAnimator.PlayLoop(stats.idleFrames, stats.idleFps);
            baseScale = Vector3.one * stats.scale;
            transform.localScale = baseScale;
            ColliderFit.FitCircle(circleCollider, spriteRenderer.sprite);
            Run.Scoring.EncounterReporter.Report(stats.name);

            attackRunner.Initialize(bulletSpawner, heroMask);
            attackRunner.SetTarget(target);
            attackRunner.SetFiring(false);
        }

        private void Update()
        {
            if (!health.IsAlive) return;

            // Breach expiry (#141): tick-driven like everything else on the Boss.
            if (breachUntil > 0f && Time.time >= breachUntil)
            {
                breachUntil = 0f;
                health.IncomingDamageScale = 1f;
            }

            if (entering)
            {
                transform.position += Vector3.down * (stats.entrySpeed * Time.deltaTime);
                if (transform.position.y <= hoverLineY)
                {
                    transform.position = new Vector3(transform.position.x, hoverLineY, 0f);
                    entering = false;
                    // The fight starts here: the Phase matching the CURRENT HP fraction
                    // (Hero Bullets can already hit during the entrance), else the
                    // runner's own serialized Patterns with fresh clocks.
                    if (stats.phases.Length > 0)
                        EnterPhase(PhaseForFraction((float)health.Current / health.Max));
                    else attackRunner.ResetState();
                }
                return;
            }

            float speedMultiplier = currentPhase >= 0 ? stats.phases[currentPhase].hoverSpeedMultiplier : 1f;
            hoverT += Time.deltaTime * stats.hoverSpeed * speedMultiplier;
            float x = anchorX + Mathf.Sin(hoverT) * stats.hoverAmplitude;
            transform.position = new Vector3(x, hoverLineY, 0f);

            // Summons: the Phase calls Enemies in beside the Boss while it keeps
            // firing (a Summoner archetype is pure data on top of this).
            if (currentPhase >= 0 && Time.time >= nextSummonAt)
            {
                BossPhase phase = stats.phases[currentPhase];
                if (phase.summonType != null && reinforcements != null)
                {
                    for (int i = 0; i < phase.summonCount; i++)
                    {
                        float side = phase.summonCount == 1 ? 0f : i - (phase.summonCount - 1) * 0.5f;
                        var pos = new Vector2(transform.position.x + side * 1.4f,
                                              transform.position.y + 0.9f);
                        reinforcements.SpawnReinforcement(phase.summonType, pos);
                    }
                }
                nextSummonAt = Time.time + phase.summonInterval;
            }

            // Entry pulse: scale spikes to the Phase's peak and eases back. Tick-driven
            // like the runner - no coroutine to die quietly.
            if (Time.time < pulseEndsAt)
            {
                float remaining = (pulseEndsAt - Time.time) / pulseDuration;
                transform.localScale = baseScale * Mathf.Lerp(1f, pulsePeak, remaining);
            }
            else if (transform.localScale != baseScale)
            {
                transform.localScale = baseScale;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!health.IsAlive) return;
            if ((heroMask.value & (1 << other.gameObject.layer)) == 0) return;

            Health hero = other.GetComponentInParent<Health>();
            if (hero == null || !hero.IsAlive) return;

            // Unlike a basic Enemy, the Boss is not consumed by contact.
            hero.TakeDamage(stats.contactDamage);
        }

        /// <summary>Deepest Phase whose entry threshold the fraction has reached —
        /// later Phases win, so a 50% band outranks the 100% opener below half HP.</summary>
        private int PhaseForFraction(float fraction)
        {
            int phase = 0;
            for (int i = 1; i < stats.phases.Length; i++)
                if (fraction <= stats.phases[i].entersAtHpFraction) phase = i;
            return phase;
        }

        private void OnHealthChanged(Health _)
        {
            // Phases only move while the fight is on. SetPatterns resets every burst
            // and cooldown clock, so the previous Phase's in-flight bursts cancel
            // cleanly (ADR-0003) — and an equal index never re-triggers.
            if (entering || currentPhase < 0 || !health.IsAlive) return;

            int next = PhaseForFraction((float)health.Current / health.Max);
            if (next != currentPhase)
            {
                // BREACH (#141): a Phase TRANSITION drops the guard - double damage
                // for a few seconds. Fights reward pressure at the dangerous moment
                // instead of stretching a flat bar; the opening Phase never breaches.
                breachUntil = Time.time + breachDuration;
                health.IncomingDamageScale = breachMultiplier;
                EnterPhase(next);
            }
        }

        private void EnterPhase(int index)
        {
            currentPhase = index;
            BossPhase phase = stats.phases[index];
            attackRunner.SetPatterns(phase.patterns);
            // First volley after one full interval — a Phase opens with bullets, not adds.
            nextSummonAt = Time.time + phase.summonInterval;

            // The Phase change must be readable (CONTEXT.md: Telegraph): the look
            // changes with the behaviour, not silently alongside it.
            spriteRenderer.color = phase.tint;
            if (phase.entryPulseScale > 1f)
            {
                pulsePeak = phase.entryPulseScale;
                pulseDuration = phase.entryPulseDuration;
                pulseEndsAt = Time.time + phase.entryPulseDuration;
            }

            PhaseEntered?.Invoke(this, index);
        }

        /// <summary>Ends the guarded state (#81): the last lackey fell, the Boss is
        /// vulnerable and announces it with a phase-entry-style kick. The director
        /// owns WHEN; the Boss owns how it looks.</summary>
        public void Unleash()
        {
            health.Invulnerable = false;
            pulsePeak = 1.4f;
            pulseDuration = 0.6f;
            pulseEndsAt = Time.time + pulseDuration;
        }

        private void OnShieldBroken(Health _)
        {
            // The unlock must be unmissable (CONTEXT.md: Telegraph): the ward
            // shatters and the Boss itself kicks, like a phase entry.
            if (shieldVisual != null) shieldVisual.Break();
            shieldVisual = null;
            pulsePeak = 1.35f;
            pulseDuration = 0.5f;
            pulseEndsAt = Time.time + pulseDuration;
        }

        private void OnDied(Health _)
        {
            attackRunner.SetFiring(false);
            Defeated?.Invoke(this);
        }
    }
}
