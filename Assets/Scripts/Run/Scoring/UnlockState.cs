using System.Collections.Generic;
using UnityEngine;

namespace OneMoreKnight.Run.Scoring
{
    /// <summary>
    /// The earned-achievement slugs the site has told us about (#89). The website is
    /// the source of truth (achievements are evaluated there, #63); this is a cached
    /// mirror in PlayerPrefs — the project's first persistent state, IndexedDB-backed
    /// on WebGL — so unlocks survive offline boots. In the editor everything counts
    /// as unlocked so skins stay testable.
    /// </summary>
    public static class UnlockState
    {
        private const string PrefsKey = "omk-unlocks";
        private static HashSet<string> earned;

        private static HashSet<string> Earned
        {
            get
            {
                if (earned == null)
                {
                    earned = new HashSet<string>();
                    string cached = PlayerPrefs.GetString(PrefsKey, "");
                    foreach (string slug in cached.Split(','))
                        if (!string.IsNullOrEmpty(slug)) earned.Add(slug);
                }
                return earned;
            }
        }

        /// <summary>Replaces the earned set with the site's CSV push and caches it.</summary>
        public static void Set(string csv)
        {
            earned = new HashSet<string>();
            foreach (string slug in (csv ?? "").Split(','))
                if (!string.IsNullOrEmpty(slug)) earned.Add(slug.Trim());
            PlayerPrefs.SetString(PrefsKey, string.Join(",", earned));
            PlayerPrefs.Save();
        }

        public static bool Has(string slug)
        {
#if UNITY_EDITOR
            return true;
#else
            return string.IsNullOrEmpty(slug) || Earned.Contains(slug);
#endif
        }
    }
}
