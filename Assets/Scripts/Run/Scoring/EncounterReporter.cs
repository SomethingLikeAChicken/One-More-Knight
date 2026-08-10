using System.Collections.Generic;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace OneMoreKnight.Run.Scoring
{
    /// <summary>
    /// Tells the hosting page the first time each Enemy/Boss type is seen in a Run —
    /// the site's bestiary unlocks from it (#46). Session-deduped, fire-and-forget,
    /// and a silent no-op outside the browser or on pages without the bridge.
    /// </summary>
    public static class EncounterReporter
    {
        private static readonly HashSet<string> seen = new HashSet<string>();

        /// <summary>The Arena sets this: testing a type must not unlock its diary entry.</summary>
        public static bool Suppressed;

        public static void Report(string slug)
        {
            if (Suppressed || string.IsNullOrEmpty(slug) || !seen.Add(slug)) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            OMK_ReportEncounter(slug);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OMK_ReportEncounter(string slug);
#endif
    }
}
