using System;
using UnityEngine;
using OneMoreKnight.Enemies;

namespace OneMoreKnight.Waves
{
    public enum GroupFormation
    {
        /// <summary>Spread evenly across the play area's width.</summary>
        Line,
        /// <summary>A wedge: the first Enemy leads at the anchor, pairs fan out behind.</summary>
        Vee,
        /// <summary>One lane at the anchor, marching down in sequence.</summary>
        Column
    }

    /// <summary>
    /// One slot inside a group: its own Enemy type, count, shape and pacing — so a
    /// single group can mix types (a Sentinel screened by Descenders, say).
    /// </summary>
    [Serializable]
    public class GroupSlot
    {
        public EnemyStats type;
        [Min(1)] public int count = 4;
        public GroupFormation formation = GroupFormation.Line;
        [Tooltip("Horizontal anchor, -1 = left edge, 0 = centre, 1 = right edge. Line ignores it.")]
        [Range(-1f, 1f)] public float anchor;
        [Tooltip("World units between neighbouring slots (Vee).")]
        [Min(0.1f)] public float spacing = 1.3f;
        [Tooltip("Seconds between two spawns inside this slot.")]
        [Min(0f)] public float spawnInterval = 0.3f;
        [Tooltip("Seconds after the previous slot before this one starts.")]
        [Min(0f)] public float delayBeforeSlot;
    }

    /// <summary>
    /// One reusable choreographed Enemy group with a difficulty cost — the currency
    /// of the budget wave system (issue #24). The composer buys groups until the
    /// wave's budget is spent; minWave gates content so early Runs stay gentle.
    /// Shared, read-only asset (ADR-0003 rules apply).
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Enemy Group", fileName = "Group")]
    public class GroupDefinition : ScriptableObject
    {
        [Tooltip("What this group costs against a wave's difficulty budget.")]
        [Min(1)] public int difficulty = 5;
        [Tooltip("Earliest wave this group may appear in.")]
        [Min(1)] public int minWave = 1;
        public GroupSlot[] slots = new GroupSlot[0];
    }
}
