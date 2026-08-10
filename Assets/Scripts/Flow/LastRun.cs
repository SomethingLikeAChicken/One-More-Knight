namespace OneMoreKnight.Flow
{
    /// <summary>
    /// The finished Run's final readout, carried from the Game scene to the GameOver
    /// scene. Deliberately not CONTEXT.md's "Run Summary" — that is the post-MVP
    /// backend payload; this is just the two numbers the GameOver screen shows.
    /// </summary>
    public static class LastRun
    {
        public static int Score;
        public static int Wave;
    }
}
