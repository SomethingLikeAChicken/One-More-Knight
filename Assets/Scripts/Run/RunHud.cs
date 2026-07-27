using UnityEngine;
using OneMoreKnight.Combat;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// Throwaway HUD drawn with IMGUI.
    ///
    /// This is deliberate: AGENTS.md still lists uGUI vs UI Toolkit as an open decision
    /// for M2, and a mockup should not quietly settle it. IMGUI has zero setup, ships
    /// nothing to the real UI layer, and is trivial to delete once the choice is made.
    /// Do not build on this.
    /// </summary>
    public class RunHud : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private Health heroHealth;

        private GUIStyle readout;
        private GUIStyle banner;
        private GUIStyle hint;

        private void EnsureStyles()
        {
            if (readout != null) return;

            readout = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            readout.normal.textColor = Color.white;

            banner = new GUIStyle(GUI.skin.label)
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            banner.normal.textColor = new Color(1f, 0.42f, 0.42f);

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

            GUILayout.BeginArea(new Rect(16f, 12f, 320f, 120f));
            GUILayout.Label($"SCORE  {runManager.Score:n0}", readout);
            GUILayout.Label($"WAVE   {runManager.Wave}", readout);
            GUILayout.Label($"HEALTH {new string('#', heroHealth.Current)}{new string('.', Mathf.Max(0, heroHealth.Max - heroHealth.Current))}", readout);
            GUILayout.EndArea();

            if (!runManager.IsOver) return;

            float w = Screen.width;
            float h = Screen.height;

            GUI.Label(new Rect(0f, h * 0.5f - 70f, w, 60f), "THE RUN ENDS", banner);
            GUI.Label(new Rect(0f, h * 0.5f - 8f, w, 30f), $"Score {runManager.Score:n0}   ·   Wave {runManager.Wave}", hint);
            GUI.Label(new Rect(0f, h * 0.5f + 28f, w, 30f), "press SPACE or R to run again", hint);
        }
    }
}
