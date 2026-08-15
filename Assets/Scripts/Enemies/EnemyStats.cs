using UnityEngine;

namespace OneMoreKnight.Enemies
{
    public enum MovementProfile
    {
        Descend,
        Weave
    }

    /// <summary>
    /// A guaranteed on-death powerup drop (#76). Mirrors <see cref="Hero.PowerupType"/>
    /// plus <c>None</c>; mapped explicitly in PowerupDirector so the two enums may
    /// evolve independently.
    /// </summary>
    public enum GuaranteedDrop
    {
        None,
        Damage,
        MaxHp,
        MoveSpeed,
        FireRate
    }

    /// <summary>
    /// Tuning for one Enemy type, held in an asset rather than as magic numbers inside a
    /// MonoBehaviour (AGENTS.md seam for M4).
    ///
    /// This is a <b>shared, read-only</b> asset. It describes an Enemy type; it must never
    /// hold the state of an Enemy currently alive in the Run (ADR-0003). Per-Wave scaling
    /// is passed to <see cref="Enemy.Spawn"/> as a multiplier — never written back here.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Min(1)] public int maxHealth = 3;
        [Min(0f)] public float moveSpeed = 2.2f;
        [Min(0)] public int scoreValue = 100;
        [Min(0)] public int contactDamage = 1;
        [Tooltip("Bestiary lore on the website (#46).")]
        [TextArea] public string description;
        [Tooltip("Bestiary tactics: how it behaves and how to beat it (#60).")]
        [TextArea] public string tactics;

        [Header("Miniboss (#53)")]
        [Tooltip("Minibosses carry a small tracking HP bar. Content flag, no other behaviour change.")]
        public bool isMiniboss;

        [Header("Drops (#76)")]
        [Tooltip("None = the normal drop-chance roll. Anything else ALWAYS drops that " +
                 "powerup on death - makes minibosses worth the detour.")]
        public GuaranteedDrop guaranteedDrop = GuaranteedDrop.None;

        [Header("Curse (#55)")]
        [Tooltip("KILLING this type inflicts the curse - holding fire is the counter. " +
                 "Cursed types pulse a dark aura so the threat is readable.")]
        public Hero.CurseType curseOnDeath = Hero.CurseType.None;

        [Header("Identity (applied to the one pooled prefab on spawn)")]
        public Sprite sprite;
        public Color tint = Color.white;

        [Header("Animation (#116)")]
        [Tooltip("Idle loop frames; frame 0 is the static sprite so spawn and loop " +
                 "wrap are seamless. Empty = static sprite, exactly as before.")]
        public Sprite[] idleFrames = new Sprite[0];
        [Min(0.1f)] public float idleFps = 6f;

        [Header("Movement")]
        public MovementProfile movement = MovementProfile.Descend;
        [Tooltip("Weave only: horizontal amplitude in world units.")]
        [Min(0f)] public float weaveAmplitude = 1.5f;
        [Tooltip("Weave only: oscillations in radians per second.")]
        [Min(0f)] public float weaveFrequency = 2f;

        [Header("Attack")]
        [Tooltip("The Attack Pattern this Enemy type fires (ADR-0003). Null = never shoots.")]
        public Combat.Patterns.AttackPattern attack;
    }
}
