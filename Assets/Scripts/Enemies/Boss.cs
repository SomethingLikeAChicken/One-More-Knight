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

        private float hoverLineY;
        private float anchorX;
        private float hoverT;
        private bool entering;

        public BossStats Stats => stats;
        public Health Health => health;

        /// <summary>Died to damage — the Run scores the reward and Waves resume.</summary>
        public event Action<Boss> Defeated;

        private void Reset() => health = GetComponent<Health>();

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        /// <summary>Starts the entrance: descend from <paramref name="spawnPosition"/>
        /// to <paramref name="hoverY"/>, then hover around the spawn X. Attacks come
        /// from the runner's Pattern assets and only start once the hover is reached —
        /// firing during the entrance would be an unfair off-screen attack.</summary>
        public void Begin(Vector2 spawnPosition, float hoverY, Combat.BulletSpawner bulletSpawner, Transform target)
        {
            transform.position = spawnPosition;
            anchorX = spawnPosition.x;
            hoverLineY = hoverY;
            hoverT = 0f;
            entering = true;
            health.ResetHealth(stats.maxHealth);

            attackRunner.Initialize(bulletSpawner, heroMask);
            attackRunner.SetTarget(target);
            attackRunner.SetFiring(false);
        }

        private void Update()
        {
            if (!health.IsAlive) return;

            if (entering)
            {
                transform.position += Vector3.down * (stats.entrySpeed * Time.deltaTime);
                if (transform.position.y <= hoverLineY)
                {
                    transform.position = new Vector3(transform.position.x, hoverLineY, 0f);
                    entering = false;
                    // Fresh clocks from the moment the fight actually starts.
                    attackRunner.ResetState();
                }
                return;
            }

            hoverT += Time.deltaTime * stats.hoverSpeed;
            float x = anchorX + Mathf.Sin(hoverT) * stats.hoverAmplitude;
            transform.position = new Vector3(x, hoverLineY, 0f);
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

        private void OnDied(Health _)
        {
            attackRunner.SetFiring(false);
            Defeated?.Invoke(this);
        }
    }
}
