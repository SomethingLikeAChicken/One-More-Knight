using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreKnight.Flow
{
    /// <summary>
    /// Title screen. Throwaway IMGUI like RunHud: uGUI vs UI Toolkit is still an open
    /// decision and this skeleton must not settle it. Do not build on this.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle hint;

        public void StartGame() => SceneFlow.LoadGame();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) StartGame();
        }

        private void EnsureStyles()
        {
            if (title != null) return;

            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 52,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            title.normal.textColor = new Color(0.95f, 0.85f, 0.55f);

            hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            hint.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
        }

        private void OnGUI()
        {
            EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;

            GUI.Label(new Rect(0f, h * 0.35f - 40f, w, 80f), "ONE MORE KNIGHT", title);
            GUI.Label(new Rect(0f, h * 0.55f, w, 30f), "press SPACE to start the Run", hint);
        }
    }
}
