#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace OneMoreKnight.Run.Scoring
{
    /// <summary>
    /// Picks the platform's <see cref="IScoreSubmitter"/>. In a browser the Run is
    /// handed to the hosting page via ScoreBridge.jslib — the page owns the session
    /// and the API call (ADR-0004: the OAuth flow lives outside Unity). Everywhere
    /// else (editor, standalone) submission is a silent no-op.
    /// </summary>
    public static class ScoreSubmitter
    {
        public static IScoreSubmitter Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new BrowserScoreSubmitter();
#else
            return new NullScoreSubmitter();
#endif
        }

        private sealed class NullScoreSubmitter : IScoreSubmitter
        {
            public void Submit(int score, int wave, int bossesDefeated) { }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private sealed class BrowserScoreSubmitter : IScoreSubmitter
        {
            [DllImport("__Internal")]
            private static extern void OMK_SubmitScore(int score, string metaJson);

            public void Submit(int score, int wave, int bossesDefeated)
            {
                // Meta is the ADR-0005 run summary the backend stores for auditing.
                OMK_SubmitScore(score, $"{{\"wave\":{wave},\"bosses\":{bossesDefeated}}}");
            }
        }
#endif
    }
}
