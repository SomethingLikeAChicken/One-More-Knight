using UnityEngine;

namespace OneMoreKnight.Hero
{
    public enum PowerupType
    {
        Damage,
        MaxHp,   // kept for pickup identity: acts as an instant +1 heal (#67)
        MoveSpeed,
        FireRate
    }

    public enum CurseType
    {
        None,
        /// <summary>Move speed ×0.6 while active.</summary>
        Leaden,
        /// <summary>Fire cooldown ×1.8 while active.</summary>
        Jammed,
        /// <summary>The screen edges close in — vision shrinks while active (#68).</summary>
        Blind,
        /// <summary>Bullet damage −1 (min 1) while active (#68).</summary>
        Weakness
    }

    /// <summary>
    /// The Hero's modifier state. Buffs are 10-second TIMED effects (#67 hotfix —
    /// permanent stacks let testers coast to 120k): picking the same type again
    /// refreshes the clock. The heart is an instant +1 heal instead. Curses are the
    /// timed debuff mirror (#55/#68). Scene-lifetime state.
    /// </summary>
    public class HeroUpgrades : MonoBehaviour
    {
        [Header("Buffs (#67)")]
        [SerializeField] [Min(1f)] private float buffDuration = 10f;
        [SerializeField] [Min(0)] private int swordBonusDamage = 2;
        [SerializeField] private float wingSpeedMultiplier = 1.3f;
        [SerializeField] private float boltCooldownMultiplier = 0.65f;

        [Header("Curses")]
        [SerializeField] [Min(0.5f)] private float curseDuration = 4f;

        private readonly float[] buffEndsAt = new float[4];
        private CurseType curse = CurseType.None;
        private float curseEndsAt;

        /// <summary>A pickup landed — RunStats listens (#63).</summary>
        public event System.Action<PowerupType> PowerupApplied;

        /// <summary>A death-curse landed — RunStats listens (#63).</summary>
        public event System.Action<CurseType> CurseApplied;

        public bool BuffActive(PowerupType type) => Time.time < buffEndsAt[(int)type];
        public float BuffRemaining(PowerupType type) => Mathf.Max(0f, buffEndsAt[(int)type] - Time.time);

        public CurseType ActiveCurse => Time.time < curseEndsAt ? curse : CurseType.None;
        public float CurseRemaining => Mathf.Max(0f, curseEndsAt - Time.time);

        /// <summary>Added to the Hero's bullet damage. Weakness can push it negative —
        /// the controller clamps the final damage to at least 1.</summary>
        public int BonusDamage =>
            (BuffActive(PowerupType.Damage) ? swordBonusDamage : 0)
            - (ActiveCurse == CurseType.Weakness ? 1 : 0);

        public float MoveSpeedMultiplier =>
            (BuffActive(PowerupType.MoveSpeed) ? wingSpeedMultiplier : 1f)
            * (ActiveCurse == CurseType.Leaden ? 0.6f : 1f);

        public float FireCooldownMultiplier =>
            (BuffActive(PowerupType.FireRate) ? boltCooldownMultiplier : 1f)
            * (ActiveCurse == CurseType.Jammed ? 1.8f : 1f);

        /// <summary>Applies one pickup: hearts heal +1 current (never past max), the
        /// rest start/refresh their 10s clock. Returns false only for a full-HP heart.</summary>
        public bool Apply(PowerupType type)
        {
            if (type == PowerupType.MaxHp)
            {
                var health = GetComponent<Combat.Health>();
                if (health == null || health.Current >= health.Max) return false;
                health.Heal(1);
            }
            else
            {
                buffEndsAt[(int)type] = Time.time + buffDuration;
            }
            PowerupApplied?.Invoke(type);
            return true;
        }

        public void ApplyCurse(CurseType type)
        {
            if (type == CurseType.None) return;
            curse = type;
            curseEndsAt = Time.time + curseDuration;
            CurseApplied?.Invoke(type);
        }
    }
}
