using UnityEngine;

namespace OneMoreKnight.Hero
{
    public enum PowerupType
    {
        Damage,
        MaxHp,   // kept for pickup identity: acts as an instant +1 heal (#67)
        MoveSpeed,
        FireRate,
        /// <summary>One-hit ward via Health.Shield (#83) - late-game only.</summary>
        Aegis,
        /// <summary>Instant: clears every hostile Bullet on screen (#83) - late-game only.</summary>
        Purge
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
        Weakness,
        /// <summary>The chill of the grave (#139): move ×0.75 AND fire ×1.3 — a
        /// milder, mixed Leaden+Jammed.</summary>
        Frost,
        /// <summary>Your prayers go unheard (#139): Powerup pickups do nothing while
        /// active — an untouched pickup stays on the field for after.</summary>
        Silence,
        /// <summary>Your limbs betray you (#139): movement controls inverted.</summary>
        Hex
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

        private readonly float[] buffEndsAt = new float[6];
        private CurseType curse = CurseType.None;
        private float curseEndsAt;
        private Combat.Health health;
        private AegisAura aura;

        // Pact hooks (#135): the chosen bargain's hero-side levers, set and reset by
        // the PactDirector - default 1 = no Pact.
        public float PactMoveScale { get; set; } = 1f;
        public float PactFireCooldownScale { get; set; } = 1f;
        public float PactCurseDurationScale { get; set; } = 1f;

        /// <summary>A pickup landed — RunStats listens (#63).</summary>
        public event System.Action<PowerupType> PowerupApplied;

        /// <summary>A death-curse landed — RunStats listens (#63).</summary>
        public event System.Action<CurseType> CurseApplied;

        // Aegis is over the moment its ward is spent, timer or not (#83).
        public bool BuffActive(PowerupType type) => Time.time < buffEndsAt[(int)type]
            && (type != PowerupType.Aegis || (health != null && health.Shield > 0));
        public float BuffRemaining(PowerupType type) => Mathf.Max(0f, buffEndsAt[(int)type] - Time.time);

        private void Awake() => health = GetComponent<Combat.Health>();

        private void Update()
        {
            // An unspent Aegis ward expires with its clock (#83).
            if (health != null && health.Shield > 0
                && buffEndsAt[(int)PowerupType.Aegis] > 0f && !BuffActive(PowerupType.Aegis))
            {
                health.SetShield(0);
                buffEndsAt[(int)PowerupType.Aegis] = 0f;
            }
        }

        public CurseType ActiveCurse => Time.time < curseEndsAt ? curse : CurseType.None;
        public float CurseRemaining => Mathf.Max(0f, curseEndsAt - Time.time);

        /// <summary>Added to the Hero's bullet damage. Weakness can push it negative —
        /// the controller clamps the final damage to at least 1.</summary>
        public int BonusDamage =>
            (BuffActive(PowerupType.Damage) ? swordBonusDamage : 0)
            - (ActiveCurse == CurseType.Weakness ? 1 : 0);

        public float MoveSpeedMultiplier =>
            (BuffActive(PowerupType.MoveSpeed) ? wingSpeedMultiplier : 1f)
            * (ActiveCurse == CurseType.Leaden ? 0.6f : ActiveCurse == CurseType.Frost ? 0.75f : 1f)
            * PactMoveScale;

        public float FireCooldownMultiplier =>
            (BuffActive(PowerupType.FireRate) ? boltCooldownMultiplier : 1f)
            * (ActiveCurse == CurseType.Jammed ? 1.8f : ActiveCurse == CurseType.Frost ? 1.3f : 1f)
            * PactFireCooldownScale;

        /// <summary>Hex (#139): the controller negates its merged movement input.</summary>
        public bool InvertControls => ActiveCurse == CurseType.Hex;

        /// <summary>Applies one pickup: hearts heal +1 current (never past max), Aegis
        /// raises the one-hit ward (#83), Purge is instant (the spoils system executes
        /// the clear via <see cref="PowerupApplied"/>), the rest start/refresh their
        /// 10s clock. Returns false only for a full-HP heart.</summary>
        public bool Apply(PowerupType type)
        {
            // Silence (#139): nothing lands while the bell tolls. Returning false
            // leaves the pickup on the field - collectable once the curse ends.
            if (ActiveCurse == CurseType.Silence) return false;
            switch (type)
            {
                case PowerupType.MaxHp:
                    if (health == null || health.Current >= health.Max) return false;
                    health.Heal(1);
                    break;
                case PowerupType.Aegis:
                    health.SetShield(1);
                    buffEndsAt[(int)type] = Time.time + buffDuration;
                    if (aura == null) aura = AegisAura.Attach(transform, health);
                    break;
                case PowerupType.Purge:
                    break; // world effect - PowerupDirector listens
                default:
                    buffEndsAt[(int)type] = Time.time + buffDuration;
                    break;
            }
            PowerupApplied?.Invoke(type);
            return true;
        }

        public void ApplyCurse(CurseType type)
        {
            if (type == CurseType.None) return;
            curse = type;
            curseEndsAt = Time.time + curseDuration * PactCurseDurationScale;
            CurseApplied?.Invoke(type);
        }
    }
}
