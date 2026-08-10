using UnityEngine;

namespace OneMoreKnight.Flow
{
    /// <summary>
    /// Entry point of the app. One frame of process-wide setup, then straight to the
    /// Menu. Future init that must happen exactly once (backend session, settings)
    /// belongs here, which is the reason this scene exists at all.
    /// </summary>
    public class BootController : MonoBehaviour
    {
        private void Awake()
        {
            // Keeps the game simulating when the tab/window loses focus. Also required
            // for agent-driven play tests: an unfocused editor otherwise stalls play
            // mode near frame 2 (AGENTS.md gotcha).
            Application.runInBackground = true;
        }

        private void Start()
        {
            // The website's bestiary can boot straight into a duel (#46).
            if (SceneFlow.ArenaTargetFromUrl() != null) SceneFlow.LoadArena();
            else SceneFlow.LoadMenu();
        }
    }
}
