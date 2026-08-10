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

        public event Action<Health> Changed;
        public event Action<Health> Died;

        private void Awake() => ResetHealth();

        /// <summary>
        /// Restores full Health. Pooled actors MUST call this on spawn — deactivation
        /// is not a reset (ADR-0003).
        /// </summary>
        public void ResetHealth(int newMax = 0)
        {
            if (newMax > 0) maxHealth = newMax;
            Current = maxHealth;
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
            if (!IsAlive || amount <= 0) return;

            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(this);

            if (Current == 0) Died?.Invoke(this);
        }
    }
}
