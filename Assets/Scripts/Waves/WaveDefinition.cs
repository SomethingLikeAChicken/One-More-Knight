using System;
using UnityEngine;
using OneMoreKnight.Enemies;

namespace OneMoreKnight.Waves
{
    public enum GroupFormation
    {
        /// <summary>Spread evenly across the play area's width.</summary>
        Line,
        /// <summary>A wedge: the first Enemy leads at the anchor, pairs fan out and
        /// behind it — reads as a V descending.</summary>
        Vee,
        /// <summary>One lane at the anchor, marching down in sequence.</summary>
        Column
    }

    /// <summary>
    /// One choreographed group inside a Wave: which Enemy type enters, how many,
    /// in what shape, where, and how quickly they follow each other.
    /// </summary>
    [Serializable]
    public class EnemyGroup
    {
        public EnemyStats type;
        [Min(1)] public int count = 4;
        public GroupFormation formation = GroupFormation.Line;
        [Tooltip("Horizontal anchor, -1 = left edge, 0 = centre, 1 = right edge. Line ignores it.")]
        [Range(-1f, 1f)] public float anchor;
        [Tooltip("World units between neighbouring slots (Vee); Line spreads, Column stacks.")]
        [Min(0.1f)] public float spacing = 1.3f;
        [Tooltip("Seconds between two spawns inside this group.")]
        [Min(0f)] public float spawnInterval = 0.3f;
        [Tooltip("Seconds after the previous group before this one starts.")]
        [Min(0f)] public float delayBeforeGroup;
    }

    /// <summary>
    /// One authored Wave (CONTEXT.md): an ordered set of Enemy groups. A Wave is
    /// choreography — shape and pacing — not just a quantity (issue #16).
    /// Shared, read-only asset (ADR-0003 rules apply).
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Wave", fileName = "Wave")]
    public class WaveDefinition : ScriptableObject
    {
        public EnemyGroup[] groups = new EnemyGroup[0];
    }
}
