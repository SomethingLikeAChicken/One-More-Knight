using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreKnight.Flow
{
    /// <summary>
    /// End-of-Run screen: final Score and Wave from LastRun, play again or back to the
    /// Menu. Throwaway IMGUI like RunHud — do not build on this.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        private GUIStyle banner;
        private GUIStyle readout;
        private GUIStyle hint;

        public void PlayAgain() => SceneFlow.LoadGame();

        public void BackToMenu() => SceneFlow.LoadMenu();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame) PlayAgain();
            else if (keyboard.escapeKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame) BackToMenu();
        }

        private void EnsureStyles()
        {
            if (banner != null) return;

            banner = new GUIStyle(GUI.skin.label)
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            banner.normal.textColor = new Color(1f, 0.42f, 0.42f);

            readout = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter
            };
            readout.normal.textColor = Color.white;

            hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            hint.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
        }

        private void OnGUI()
        {
            EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;

            GUI.Label(new Rect(0f, h * 0.5f - 90f, w, 60f), "THE RUN ENDS", banner);
            GUI.Label(new Rect(0f, h * 0.5f - 20f, w, 30f), $"Score {LastRun.Score:n0}   ·   Wave {LastRun.Wave}", readout);
            GUI.Label(new Rect(0f, h * 0.5f + 24f, w, 30f), "SPACE / R — one more run", hint);
            GUI.Label(new Rect(0f, h * 0.5f + 52f, w, 30f), "ESC / M — back to the menu", hint);
        }
    }
}
