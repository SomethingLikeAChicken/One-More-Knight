using UnityEngine;

namespace OneMoreKnight.Waves
{
    /// <summary>
    /// The Run's entire difficulty curve in one asset (issue #24): how the per-wave
    /// budget grows, which groups the composer may buy, how they are layered in
    /// time, and how HP/speed scale once the budget is capped.
    ///
    /// Fairness by construction carries over from #16: past the budget cap, waves
    /// get harder only through hard-capped multipliers — never denser.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Difficulty Progression", fileName = "DifficultyProgression")]
    public class DifficultyProgression : ScriptableObject
    {
        [Header("Budget curve")]
        [Min(1)] public int baseBudget = 8;
        [Min(0)] public int budgetPerWave = 3;
        [Min(1)] public int maxBudget = 30;

        [Header("Group pool")]
        public GroupDefinition[] pool = new GroupDefinition[0];

        [Header("Pacing")]
        [Tooltip("Seconds between two groups of the same wave - the layering guarantee.")]
        [Min(0f)] public float delayBetweenGroups = 2.2f;
        [Min(0f)] public float intermission = 1.75f;

        [Header("Post-cap escalation (per wave past the budget cap)")]
        [Min(1f)] public float hpMultiplierStep = 1.04f;
        [Min(1f)] public float speedMultiplierStep = 1.01f;
        [Min(1f)] public float maxHpMultiplier = 4f;
        [Min(1f)] public float maxSpeedMultiplier = 1.5f;

        /// <summary>Budget for 1-based wave <paramref name="waveNumber"/>.</summary>
        public int BudgetFor(int waveNumber)
            => Mathf.Min(baseBudget + (waveNumber - 1) * budgetPerWave, maxBudget);

        /// <summary>HP/speed multipliers: 1 until the budget caps, then stepped per
        /// extra wave up to the hard caps.</summary>
        public void MultipliersFor(int waveNumber, out float hpMultiplier, out float speedMultiplier)
        {
            int capWave = budgetPerWave > 0
                ? 1 + Mathf.CeilToInt((maxBudget - baseBudget) / (float)budgetPerWave)
                : 1;
            int past = Mathf.Max(0, waveNumber - capWave);
            hpMultiplier = Mathf.Min(Mathf.Pow(hpMultiplierStep, past), maxHpMultiplier);
            speedMultiplier = Mathf.Min(Mathf.Pow(speedMultiplierStep, past), maxSpeedMultiplier);
        }
    }
}
