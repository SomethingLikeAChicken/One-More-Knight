using UnityEngine;

namespace OneMoreKnight.Waves
{
    /// <summary>
    /// The Run's entire difficulty curve in one asset (issue #24): how the per-wave
    /// budget grows, which groups the composer may buy, how they are layered in
    /// time, and how enemies scale once the budget is capped.
    ///
    /// Fairness by construction carries over from #16, split in two by the #117 §9.1
    /// decision (#125): "never denser" stays locked — <see cref="maxBudget"/> is a
    /// hard ceiling and density is never the difficulty lever. Post-cap difficulty
    /// instead comes from CONTENT QUALITY: the cost floor retires cheap groups so
    /// late waves are composed of fewer, harder enemies at the same budget, and the
    /// stat multipliers stay hard-capped as a modest secondary lever.
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
        [Tooltip("Damage is the lever with real headroom (#117 §9.1): it raises the " +
                 "stakes without sponges or spent reaction time. 1 = off.")]
        [Min(1f)] public float damageMultiplierStep = 1f;
        [Min(1f)] public float maxDamageMultiplier = 1f;

        [Header("Group retirement (#125) - the primary endless lever")]
        [Tooltip("Cost floor gained per wave past the budget cap: groups cheaper than " +
                 "the floor retire, so late waves spend the same budget on fewer, " +
                 "harder groups. 0 = no retirement.")]
        [Min(0f)] public float costFloorPerWave = 0f;
        [Tooltip("Floor ceiling - keep it at or below several pool costs or late " +
                 "waves starve (the composer then falls back, warning).")]
        [Min(0)] public int maxCostFloor = 0;

        /// <summary>Budget for 1-based wave <paramref name="waveNumber"/>.</summary>
        public int BudgetFor(int waveNumber)
            => Mathf.Min(baseBudget + (waveNumber - 1) * budgetPerWave, maxBudget);

        /// <summary>Minimum group cost the composer may buy at 1-based wave
        /// <paramref name="waveNumber"/> — 0 until the budget caps, then stepped
        /// up to <see cref="maxCostFloor"/> (#125).</summary>
        public int CostFloorFor(int waveNumber)
        {
            int past = Mathf.Max(0, waveNumber - CapWave);
            return Mathf.Min(Mathf.FloorToInt(past * costFloorPerWave), maxCostFloor);
        }

        /// <summary>1-based wave at which the budget reaches <see cref="maxBudget"/>.</summary>
        private int CapWave => budgetPerWave > 0
            ? 1 + Mathf.CeilToInt((maxBudget - baseBudget) / (float)budgetPerWave)
            : 1;

        /// <summary>HP/speed/damage multipliers: 1 until the budget caps, then stepped
        /// per extra wave up to the hard caps.</summary>
        public void MultipliersFor(int waveNumber, out float hpMultiplier,
                                   out float speedMultiplier, out float damageMultiplier)
        {
            int past = Mathf.Max(0, waveNumber - CapWave);
            hpMultiplier = Mathf.Min(Mathf.Pow(hpMultiplierStep, past), maxHpMultiplier);
            speedMultiplier = Mathf.Min(Mathf.Pow(speedMultiplierStep, past), maxSpeedMultiplier);
            damageMultiplier = Mathf.Min(Mathf.Pow(damageMultiplierStep, past), maxDamageMultiplier);
        }
    }
}
