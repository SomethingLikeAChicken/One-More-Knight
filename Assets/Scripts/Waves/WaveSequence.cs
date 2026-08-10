using UnityEngine;

namespace OneMoreKnight.Waves
{
    /// <summary>
    /// The Run's Wave programme: the authored list plus the endless rule. After the
    /// last authored Wave the sequence loops from <see cref="loopStartIndex"/>, and
    /// each completed loop raises HP/speed multipliers up to hard caps.
    ///
    /// Fairness by construction (issue #16): loops never add Enemies — density is
    /// bounded by whatever the authored Waves contain, so escalation can make the
    /// Run end, but never by an unavoidable bullet wall.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Wave Sequence", fileName = "WaveSequence")]
    public class WaveSequence : ScriptableObject
    {
        public WaveDefinition[] waves = new WaveDefinition[0];

        [Header("Endless rule")]
        [Tooltip("After the last authored Wave, loop from this index.")]
        [Min(0)] public int loopStartIndex;
        [Min(1f)] public float hpMultiplierPerLoop = 1.3f;
        [Min(1f)] public float speedMultiplierPerLoop = 1.06f;
        [Min(1f)] public float maxHpMultiplier = 4f;
        [Min(1f)] public float maxSpeedMultiplier = 1.5f;
        [Min(0f)] public float intermission = 1.75f;

        /// <summary>Resolves overall Wave number <paramref name="index"/> (0-based)
        /// to a definition and its loop multipliers.</summary>
        public WaveDefinition Resolve(int index, out float hpMultiplier, out float speedMultiplier)
        {
            hpMultiplier = 1f;
            speedMultiplier = 1f;
            if (waves.Length == 0) return null;

            if (index < waves.Length) return waves[index];

            int loopLength = Mathf.Max(1, waves.Length - Mathf.Min(loopStartIndex, waves.Length - 1));
            int past = index - waves.Length;
            int cycle = 1 + past / loopLength;
            hpMultiplier = Mathf.Min(Mathf.Pow(hpMultiplierPerLoop, cycle), maxHpMultiplier);
            speedMultiplier = Mathf.Min(Mathf.Pow(speedMultiplierPerLoop, cycle), maxSpeedMultiplier);
            return waves[loopStartIndex + past % loopLength];
        }
    }
}
