using UnityEngine;

namespace OneMoreKnight.Hero
{
    public enum PowerupType
    {
        Damage,
        MaxHp,
        MoveSpeed,
        FireRate
    }

    public enum CurseType
    {
        None,
        /// <summary>Move speed ×0.6 while active.</summary>
        Leaden,
        /// <summary>Fire cooldown ×1.8 while active.</summary>
        Jammed
    }

    /// <summary>
    /// The Hero's per-Run modifier state (#55): powerup stacks (capped — the heart
    /// cap is the endless-balance guard) and the active curse. Scene-lifetime state:
    /// a fresh Run loads a fresh Game scene, so everything resets by construction.
    /// </summary>
    public class HeroUpgrades : MonoBehaviour
    {
        [Header("Caps")]
        [SerializeField] [Min(0)] private int damageCap = 3;
        [SerializeField] [Min(1)] private int maxHpCap = 6;
        [SerializeField] [Min(0)] private int speedStackCap = 3;
        [SerializeField] [Min(0)] private int fireStackCap = 3;

        [Header("Per-stack strength")]
        [SerializeField] private float speedPerStack = 0.12f;
        [SerializeField] private float fireCooldownPerStack = 0.15f;

        [Header("Curses")]
        [SerializeField] [Min(0.5f)] private float curseDuration = 4f;

        private int damageStacks;
        private int speedStacks;
        private int fireStacks;
        private CurseType curse = CurseType.None;
        private float curseEndsAt;

        public int BonusDamage => damageStacks;
        public float MoveSpeedMultiplier =>
            (1f + speedStacks * speedPerStack) * (ActiveCurse == CurseType.Leaden ? 0.6f : 1f);
        public float FireCooldownMultiplier =>
            (1f - fireStacks * fireCooldownPerStack) * (ActiveCurse == CurseType.Jammed ? 1.8f : 1f);

        public CurseType ActiveCurse => Time.time < curseEndsAt ? curse : CurseType.None;
        public float CurseRemaining => Mathf.Max(0f, curseEndsAt - Time.time);

        /// <summary>Applies one pickup. Returns false when the relevant cap is hit
        /// (the pickup is consumed either way — greed is not refunded).</summary>
        public bool Apply(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.Damage:
                    if (damageStacks >= damageCap) return false;
                    damageStacks++;
                    return true;
                case PowerupType.MaxHp:
                    var health = GetComponent<Combat.Health>();
                    if (health == null || health.Max >= maxHpCap) return false;
                    health.IncreaseMax(1); // +1 max AND +1 current - never a full heal
                    return true;
                case PowerupType.MoveSpeed:
                    if (speedStacks >= speedStackCap) return false;
                    speedStacks++;
                    return true;
                default:
                    if (fireStacks >= fireStackCap) return false;
                    fireStacks++;
                    return true;
            }
        }

        public void ApplyCurse(CurseType type)
        {
            if (type == CurseType.None) return;
            curse = type;
            curseEndsAt = Time.time + curseDuration;
        }
    }
}
