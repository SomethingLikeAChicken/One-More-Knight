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

        public static void LoadMenu() => SceneManager.LoadScene(Menu);
        public static void LoadGame() => SceneManager.LoadScene(Game);
        public static void LoadGameOver() => SceneManager.LoadScene(GameOver);
    }
}
