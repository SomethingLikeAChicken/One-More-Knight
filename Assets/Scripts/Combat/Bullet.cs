using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneMoreKnight.Combat
{
    /// <summary>
    /// A single projectile (CONTEXT.md: Bullet).
    ///
    /// Movement is <b>transform-driven</b> and the object carries no Rigidbody2D, so
    /// nothing competes for its position — the one-owner rule from ADR-0003. Collision
    /// is therefore a manual overlap check rather than a physics callback, which is also
    /// the cheaper model once a bullet-hell is spawning hundreds of these.
    ///
    /// Pooled: every field that outlives one flight is reset in <see cref="Arm"/>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Bullet : MonoBehaviour
    {
        [SerializeField] [Min(0.01f)] private float hitRadius = 0.1f;

        private readonly List<Collider2D> overlaps = new List<Collider2D>(4);
        private ContactFilter2D filter;
        private SpriteRenderer spriteRenderer;

        private Vector2 direction;
        private float speed;
        private BulletMotion motion;
        private Vector2 sineAnchor;
        private float travelled;
        private int damage;
        private float lifetime;
        private float age;
        private Action<Bullet> release;

        /// <summary>True while a Homing Bullet is still allowed to steer — exposed so
        /// tests can verify the fairness cap instead of eyeballing it.</summary>
        public bool IsSteering => motion.Type == BulletMotionType.Homing && age < motion.HomingDuration;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        /// <summary>Prepares a pooled Bullet for one flight. Resets ALL per-life state —
        /// tint, motion type, sine phase, homing target, speed — or a recycled Bullet
        /// would inherit its previous life's behaviour (ADR-0003).</summary>
        public void Arm(Vector2 origin, Vector2 spawnDirection, float spawnSpeed, int bulletDamage,
                        LayerMask hitMask, float maxLifetime, Action<Bullet> releaseCallback,
                        Color? tint = null, BulletMotion? bulletMotion = null)
        {
            transform.position = origin;
            direction = spawnDirection.normalized;
            speed = spawnSpeed;
            motion = bulletMotion ?? BulletMotion.Linear();
            sineAnchor = origin;
            travelled = 0f;
            damage = bulletDamage;
            lifetime = maxLifetime;
            age = 0f;
            release = releaseCallback;
            spriteRenderer.color = tint ?? Color.white;

            filter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true,
                useDepth = false
            };
            filter.SetLayerMask(hitMask);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            speed += motion.Acceleration * dt;
            if (speed < 0.1f) speed = 0.1f;

            switch (motion.Type)
            {
                case BulletMotionType.Sine:
                {
                    // Anchor advances along the base direction; the Bullet oscillates
                    // perpendicular to it. Fully parametric - one owner per position.
                    travelled += speed * dt;
                    sineAnchor += direction * (speed * dt);
                    var perp = new Vector2(-direction.y, direction.x);
                    transform.position = sineAnchor + perp * (Mathf.Sin(age * motion.SineFrequency) * motion.SineAmplitude);
                    break;
                }
                case BulletMotionType.Homing:
                {
                    if (IsSteering && motion.Target != null)
                    {
                        Vector2 toTarget = ((Vector2)motion.Target.position - (Vector2)transform.position).normalized;
                        float maxRadians = motion.HomingTurnSpeed * Mathf.Deg2Rad * dt;
                        direction = Vector3.RotateTowards(direction, toTarget, maxRadians, 0f).normalized;
                    }
                    transform.position += (Vector3)(direction * (speed * dt));
                    break;
                }
                default:
                    transform.position += (Vector3)(direction * (speed * dt));
                    break;
            }

            if (age >= lifetime)
            {
                Release();
                return;
            }

            int count = Physics2D.OverlapCircle(transform.position, hitRadius, filter, overlaps);
            for (int i = 0; i < count; i++)
            {
                Health target = overlaps[i].GetComponentInParent<Health>();
                if (target == null || !target.IsAlive) continue;

                target.TakeDamage(damage);
                Release();
                return;
            }
        }

        private void Release()
        {
            Action<Bullet> callback = release;
            release = null; // guards against a double release putting this in the pool twice
            callback?.Invoke(this);
        }
    }
}
