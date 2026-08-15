using System;
using UnityEngine;

namespace OneMoreKnight.Combat
{
    /// <summary>
    /// Hit points for a Combat Actor — the shared abstraction over Hero and Enemy
    /// (CONTEXT.md). Nothing here knows which one it is attached to.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] [Min(1)] private int maxHealth = 3;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;

        /// <summary>Ward points consumed before HP (#79). 0 = unwarded. Actor-agnostic:
        /// a Boss ward and the Hero's Aegis blessing are the same mechanic.</summary>
        public int Shield { get; private set; }

        /// <summary>While set, damage is ignored outright (#81) — distinct from the
        /// ward, which drains. A guarded lackey Boss sets this until its guard falls.</summary>
        public bool Invulnerable { get; set; }

        public event Action<Health> Changed;
        public event Action<Health> Died;

        /// <summary>The moment the last shield point breaks — visuals listen.</summary>
        public event Action<Health> ShieldBroken;

        /// <summary>Multiplier on incoming damage (#141) — the BREACH seam. Runtime
        /// state, default 1; reset by <see cref="ResetHealth"/> so pooled actors
        /// never inherit a window. Only the Boss sets it today.</summary>
        public float IncomingDamageScale { get; set; } = 1f;

        private void Awake() => ResetHealth();

        /// <summary>
        /// Restores full Health. Pooled actors MUST call this on spawn — deactivation
        /// is not a reset (ADR-0003).
        /// </summary>
        public void ResetHealth(int newMax = 0)
        {
            if (newMax > 0) maxHealth = newMax;
            Current = maxHealth;
            Shield = 0;
            Invulnerable = false;
            IncomingDamageScale = 1f;
            Changed?.Invoke(this);
        }

        /// <summary>Sets the ward outright (#79) — not additive, so re-applying a
        /// one-hit Aegis refreshes rather than stacks.</summary>
        public void SetShield(int amount)
        {
            Shield = Mathf.Max(0, amount);
            Changed?.Invoke(this);
        }

        /// <summary>Raises the ceiling and grants the same amount of current HP —
        /// deliberately NOT a full heal (#55's endless-balance guard).</summary>
        public void IncreaseMax(int amount)
        {
            if (amount <= 0) return;
            maxHealth += amount;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(this);
        }

        /// <summary>Restores current HP up to the ceiling (#67: the heart pickup).</summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive || Current >= maxHealth) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(this);
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0 || Invulnerable) return;
            // BREACH (#141): a lowered guard multiplies the whole hit, ward and all.
            if (IncomingDamageScale != 1f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * IncomingDamageScale));

            // The ward eats the whole hit — no bleed-through (#79). A hit that breaks
            // the last point is fully spent breaking it; fairness over bookkeeping.
            if (Shield > 0)
            {
                Shield = Mathf.Max(0, Shield - amount);
                Changed?.Invoke(this);
                if (Shield == 0) ShieldBroken?.Invoke(this);
                return;
            }

            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(this);

            if (Current == 0) Died?.Invoke(this);
        }
    }
}
