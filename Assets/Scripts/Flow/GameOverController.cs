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
        // The title is the game's thesis: a death is an invitation, not an ending.
        // One random line per Game Over (#49).
        private static readonly string[] Quotes =
        {
            "EVERY KNIGHT FALLS. THE GOOD ONES GET UP.",
            "THE BOSS REMEMBERS NOTHING. YOU REMEMBER EVERYTHING.",
            "THAT WALL HAS A GAP. YOU'VE SEEN IT NOW.",
            "DEATH IS JUST THE LOADING SCREEN FOR REVENGE.",
            "ONE MORE KNIGHT. IT'S IN THE NAME.",
            "YOU DIDN'T LOSE. YOU SCOUTED.",
            "THE CROWN IS STILL UP THERE, WAITING.",
            "DODGING IS LEARNED. YOU JUST PAID THE TUITION.",
            "THE ENDLESS HOST BLINKED FIRST LAST TIME? MAKE IT BLINK.",
            "SWORDS DULL. STUBBORNNESS DOESN'T.",
            "DU BIST GUT GENUUUUUG." // maintainer's easter egg - keep verbatim
        };

        private GUIStyle banner;
        private GUIStyle readout;
        private GUIStyle hint;
        private string quote;

        private void Awake() => quote = Quotes[Random.Range(0, Quotes.Length)];

        public void PlayAgain() => SceneFlow.LoadGame();

        public void BackToMenu() => SceneFlow.LoadMenu();

        private void Update()
        {
            // A tap is "one more run" on touch devices (#97).
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                PlayAgain();
                return;
            }

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
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
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

            GUI.Label(new Rect(w * 0.08f, h * 0.5f - 110f, w * 0.84f, 80f), quote, banner);
            GUI.Label(new Rect(0f, h * 0.5f - 20f, w, 30f), $"Score {LastRun.Score:n0}   ·   Wave {LastRun.Wave}", readout);
            GUI.Label(new Rect(0f, h * 0.5f + 24f, w, 30f), "SPACE / R / TAP — one more run", hint);
            GUI.Label(new Rect(0f, h * 0.5f + 52f, w, 30f), "ESC / M — back to the menu", hint);
        }
    }
}
