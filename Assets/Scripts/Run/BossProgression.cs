using System;
using UnityEngine;
using OneMoreKnight.Enemies;

namespace OneMoreKnight.Run
{
    /// <summary>One boss stage: the Score that triggers it and the difficulty band
    /// it may produce. The band's lower bound (#121) is what turns the ladder into
    /// a ladder — without it every stage re-offers the whole easy pool.</summary>
    [Serializable]
    public class BossStage
    {
        [Min(1)] public int scoreThreshold = 1000;
        [Min(1)] public int minDifficulty = 1;
        [Min(1)] public int maxDifficulty = 1;
    }

    /// <summary>
    /// The Run's Boss programme (issue #30), mirroring the wave-budget philosophy:
    /// the difficulty curve is fixed (stages with caps), the encounter is not (a
    /// random eligible Boss from the pool). After the last authored stage the Run
    /// keeps producing Bosses every <see cref="endlessScoreStep"/> Score at
    /// <see cref="endlessMaxDifficulty"/> — the game is endless, so are its Bosses.
    /// Shared, read-only asset (ADR-0003 rules).
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Boss Progression", fileName = "BossProgression")]
    public class BossProgression : ScriptableObject
    {
        public BossStage[] stages = new BossStage[0];
        public BossStats[] pool = new BossStats[0];

        [Header("Endless rule")]
        [Min(100)] public int endlessScoreStep = 6000;
        [Min(1)] public int endlessMinDifficulty = 1;
        [Min(1)] public int endlessMaxDifficulty = 4;

        /// <summary>Threshold + difficulty band for 0-based stage
        /// <paramref name="index"/> — authored first, then the endless continuation.</summary>
        public void GetStage(int index, out int scoreThreshold,
                             out int minDifficulty, out int maxDifficulty)
        {
            if (stages.Length == 0)
            {
                scoreThreshold = int.MaxValue;
                minDifficulty = endlessMinDifficulty;
                maxDifficulty = endlessMaxDifficulty;
                return;
            }
            if (index < stages.Length)
            {
                scoreThreshold = stages[index].scoreThreshold;
                minDifficulty = stages[index].minDifficulty;
                maxDifficulty = stages[index].maxDifficulty;
                return;
            }
            int past = index - stages.Length + 1;
            scoreThreshold = stages[stages.Length - 1].scoreThreshold + past * endlessScoreStep;
            minDifficulty = endlessMinDifficulty;
            maxDifficulty = endlessMaxDifficulty;
        }
    }
}
