namespace OneMoreKnight.Waves
{
    /// <summary>
    /// Late-Run wave spice (#57): from wave 16 a wave may carry ONE of these,
    /// announced on the HUD. Effects end with the wave; the fairness caps on the
    /// underlying curve stay untouched — modifiers are spikes, not creep.
    /// </summary>
    public enum WaveModifier
    {
        None,
        /// <summary>Enemies move 25% faster.</summary>
        Haste,
        /// <summary>Enemies carry 35% more HP.</summary>
        Ironclad,
        /// <summary>Enemy attack cooldowns ×0.75.</summary>
        Frenzy,
        /// <summary>Kills score ×1.5.</summary>
        Gilded,
        /// <summary>The wave's budget +30%.</summary>
        Swarm
    }
}
