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
        [SerializeField] [Min(0f)] private float muzzleOffset = 0.35f;

        private BulletSpawner bulletSpawner;
        private float speed;
        private float despawnY;
        private float nextShotAt;
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
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        /// <summary>One-time wiring when the pool creates the instance — the spawner is
        /// scene infrastructure, not per-life state.</summary>
        public void Initialize(BulletSpawner spawner) => bulletSpawner = spawner;

        /// <param name="speedMultiplier">Per-Wave difficulty scaling. Applied here rather
        /// than written into the shared <see cref="EnemyStats"/> asset.</param>
        public void Spawn(Vector2 position, float speedMultiplier, float despawnLineY)
        {
            transform.position = position;
            speed = stats.moveSpeed * speedMultiplier;
            despawnY = despawnLineY;
            retired = false;
            health.ResetHealth(stats.maxHealth);

            // Random first-shot phase so a Wave doesn't fire in lockstep. Cosmetic only —
            // when ADR-0005's seeded run RNG lands, anything outcome-relevant moves there.
            nextShotAt = stats.shotCooldown > 0f
                ? Time.time + UnityEngine.Random.Range(0.6f, 0.6f + stats.shotCooldown)
                : float.PositiveInfinity;
        }

        private void Update()
        {
            if (retired) return;

            transform.position += Vector3.down * (speed * Time.deltaTime);

            if (transform.position.y < despawnY)
            {
                Retire();
                return;
            }

            if (Time.time >= nextShotAt) Shoot();
        }

        /// <summary>The first Enemy attack of M3: a single straight-down Bullet through the
        /// central seam. M4's pattern engine replaces this call site, not the seam.</summary>
        private void Shoot()
        {
            nextShotAt = Time.time + stats.shotCooldown;
            if (bulletSpawner == null) return;

            Vector2 muzzle = (Vector2)transform.position + Vector2.down * muzzleOffset;
            bulletSpawner.Spawn(muzzle, Vector2.down, stats.shotSpeed, stats.shotDamage, heroMask, stats.shotTint);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (retired) return;
            if ((heroMask.value & (1 << other.gameObject.layer)) == 0) return;

            Health hero = other.GetComponentInParent<Health>();
            if (hero == null || !hero.IsAlive) return;

            hero.TakeDamage(stats.contactDamage);
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
