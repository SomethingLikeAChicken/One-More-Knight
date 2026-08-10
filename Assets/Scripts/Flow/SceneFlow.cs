using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreKnight.Flow
{
    /// <summary>
    /// The scene skeleton in one place: Boot → Menu → Game → GameOver. Controllers go
    /// through these helpers so scene names are not scattered as magic strings.
    /// </summary>
    public static class SceneFlow
    {
        public const string Boot = "Boot";
        public const string Menu = "Menu";
        public const string Game = "Game";
        public const string GameOver = "GameOver";
        public const string Arena = "Arena";

        public static void LoadMenu() => SceneManager.LoadScene(Menu);
        public static void LoadGame() => SceneManager.LoadScene(Game);
        public static void LoadGameOver() => SceneManager.LoadScene(GameOver);
        public static void LoadArena() => SceneManager.LoadScene(Arena);

        /// <summary>The `arena=&lt;Slug&gt;` value in the hosting page's URL, or null.
        /// This is how the website's bestiary opens a duel against one type (#46).</summary>
        public static string ArenaTargetFromUrl()
        {
            string url = Application.absoluteURL;
            if (string.IsNullOrEmpty(url)) return null;
            int i = url.IndexOf("arena=", System.StringComparison.Ordinal);
            if (i < 0) return null;
            int start = i + "arena=".Length;
            int end = url.IndexOfAny(new[] { '&', '#' }, start);
            string slug = end < 0 ? url.Substring(start) : url.Substring(start, end - start);
            return string.IsNullOrEmpty(slug) ? null : System.Uri.UnescapeDataString(slug);
        }
    }
}
