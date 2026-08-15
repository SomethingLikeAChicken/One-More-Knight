using System;
using UnityEngine;
using OneMoreKnight.Combat;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// The single Enemy type in the MVP: descends the play area, dies to Hero Bullets,
    /// and damages the Hero on contact.
    ///
    /// Pooled — all per-life state resets in <see cref="Spawn"/>. The Killed/Retired
    /// events are subscribed once when the pool creates the instance, not per spawn,
    /// so recycling never leaks handlers.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyStats stats;
        [SerializeField] private Health health;
        [SerializeField] private LayerMask heroMask;
        [SerializeField] private Combat.Patterns.AttackPatternRunner attackRunner;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private CircleCollider2D circleCollider;
        private MiniHealthBar miniBar;
        private float speed;
        private int contactDamage;
        private float despawnY;
        private float anchorX;
        private float age;
        private bool retired;

        public EnemyStats Stats => stats;

        /// <summary>Died to damage — the Run scores this.</summary>
        public event Action<Enemy> Killed;

        /// <summary>Left the Run for any reason (killed, hit the Hero, fell off screen).</summary>
        public event Action<Enemy> Retired;

        private void Reset() => health = GetComponent<Health>();

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            circleCollider = GetComponent<CircleCollider2D>();
            miniBar = GetComponent<MiniHealthBar>();
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        /// <summary>One-time wiring when the pool creates the instance — spawner and
        /// target are scene infrastructure, not per-life state. The target feeds
        /// AimedAtTarget Patterns; pattern code itself stays actor-agnostic.</summary>
        public void Initialize(BulletSpawner spawner, Transform target)
        {
            attackRunner.Initialize(spawner, heroMask);
            attackRunner.SetTarget(target);
        }

        /// <summary>Puts a pooled instance into play as <paramref name="type"/> — the
        /// one prefab plays every Enemy type; identity is data (PRD §5.2).</summary>
        /// <param name="speedMultiplier">Per-Wave scaling, applied here rather than
        /// written into the shared asset. Same for <paramref name="hpMultiplier"/> and
        /// <paramref name="damageMultiplier"/> (#127 — lethality is the headroom lever).</param>
        public void Spawn(EnemyStats type, Vector2 position, float speedMultiplier, float hpMultiplier,
                          float damageMultiplier, float despawnLineY)
        {
            stats = type;
            transform.position = position;
            anchorX = position.x;
            age = 0f;
            speed = stats.moveSpeed * speedMultiplier;
            contactDamage = Mathf.Max(1, Mathf.RoundToInt(stats.contactDamage * damageMultiplier));
            despawnY = despawnLineY;
            retired = false;
            health.ResetHealth(Mathf.Max(1, Mathf.RoundToInt(stats.maxHealth * hpMultiplier)));

            spriteRenderer.sprite = stats.sprite;
            spriteRenderer.color = stats.tint;
            ColliderFit.FitCircle(circleCollider, stats.sprite);
            if (miniBar != null) miniBar.SetVisible(stats.isMiniboss);
            Run.Scoring.EncounterReporter.Report(stats.name);

            // Attacks are driven entirely by the type's Pattern asset (ADR-0003); the
            // runner resets all timing state here, so a recycled instance starts fresh.
            attackRunner.SetPatterns(stats.attack);
            attackRunner.DamageScale = damageMultiplier; // after SetPatterns - ResetState puts it back to 1
        }

        private static readonly Color CurseAura = new Color(0.25f, 0.05f, 0.35f);

        private void Update()
        {
            if (retired) return;

            age += Time.deltaTime;
            float y = transform.position.y - speed * Time.deltaTime;
            float x = stats.movement == MovementProfile.Weave
                ? anchorX + Mathf.Sin(age * stats.weaveFrequency) * stats.weaveAmplitude
                : transform.position.x;
            transform.position = new Vector3(x, y, 0f);

            // Cursed types pulse a dark aura (#55): "killing me costs you" must be
            // readable before the trigger is pulled.
            if (stats.curseOnDeath != Hero.CurseType.None)
                spriteRenderer.color = Color.Lerp(stats.tint, CurseAura,
                    0.5f + 0.5f * Mathf.Sin(age * 5f));

            if (y < despawnY) Retire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (retired) return;
            if ((heroMask.value & (1 << other.gameObject.layer)) == 0) return;

            Health hero = other.GetComponentInParent<Health>();
            if (hero == null || !hero.IsAlive) return;

            hero.TakeDamage(contactDamage);
            Retire(); // the Enemy is consumed by the collision
        }

        private void OnDied(Health _)
        {
            if (retired) return;

            Killed?.Invoke(this);
            Retire();
        }

        private void Retire()
        {
            if (retired) return;
            retired = true;
            Retired?.Invoke(this);
        }
    }
}
