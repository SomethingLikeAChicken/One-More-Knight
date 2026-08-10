namespace OneMoreKnight.Run.Scoring
{
    /// <summary>
    /// The single swappable seam for end-of-Run Score submission (ADR-0004). The game
    /// is fully playable offline; whether a Run leaves the machine is decided entirely
    /// by which implementation <see cref="ScoreSubmitter.Create"/> hands out.
    /// </summary>
    public interface IScoreSubmitter
    {
        /// <summary>Fire-and-forget: report a finished Run. Never throws, never blocks.</summary>
        void Submit(int score, int wave, int bossesDefeated);
    }
}
