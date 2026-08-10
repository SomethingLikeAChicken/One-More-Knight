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
        [SerializeField] private BossDirector bossDirector;

        private GUIStyle readout;

        private void EnsureStyles()
        {
            if (readout != null) return;

            readout = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            readout.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(16f, 12f, 320f, 120f));
            GUILayout.Label($"SCORE  {runManager.Score:n0}", readout);
            GUILayout.Label($"WAVE   {runManager.Wave}", readout);
            GUILayout.Label($"HEALTH {new string('#', heroHealth.Current)}{new string('.', Mathf.Max(0, heroHealth.Max - heroHealth.Current))}", readout);
            GUILayout.EndArea();

            DrawBossBar();
        }

        private void DrawBossBar()
        {
            var boss = bossDirector != null ? bossDirector.ActiveBoss : null;
            if (boss == null || !boss.Health.IsAlive) return;

            float w = Screen.width * 0.6f;
            var back = new Rect((Screen.width - w) * 0.5f, 14f, w, 18f);
            float fraction = Mathf.Clamp01((float)boss.Health.Current / boss.Health.Max);

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.27f, 0.94f); // boss violet, per the color coding
            GUI.DrawTexture(new Rect(back.x + 2f, back.y + 2f, (back.width - 4f) * fraction, back.height - 4f),
                            Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
