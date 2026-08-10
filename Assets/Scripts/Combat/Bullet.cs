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

        private Vector2 velocity;
        private int damage;
        private float lifetime;
        private float age;
        private Action<Bullet> release;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        /// <summary>Prepares a pooled Bullet for one flight. Resets all per-life state —
        /// including the tint, or a recycled Enemy shot would leak its red onto a Hero shot.</summary>
        public void Arm(Vector2 origin, Vector2 direction, float speed, int bulletDamage,
                        LayerMask hitMask, float maxLifetime, Action<Bullet> releaseCallback,
                        Color? tint = null)
        {
            transform.position = origin;
            velocity = direction.normalized * speed;
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
            transform.position += (Vector3)(velocity * dt);

            age += dt;
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
