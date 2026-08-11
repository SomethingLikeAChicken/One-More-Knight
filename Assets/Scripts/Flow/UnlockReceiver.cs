using UnityEngine;

namespace OneMoreKnight.Flow
{
    /// <summary>
    /// The site→game half of the unlock flow (#89). Lives on a GameObject named
    /// "Unlocks" in the Menu scene: the play page pushes earned achievement slugs via
    /// <c>window.__unity.SendMessage("Unlocks", "OMK_SetUnlocks", csv)</c>, and this
    /// component also PULLS on every Menu load (covers the boot race where the push
    /// fires before the scene is ready, and refreshes after each Run).
    /// </summary>
    public class UnlockReceiver : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void OMK_RequestUnlocks();
#endif

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OMK_RequestUnlocks();
#endif
        }

        /// <summary>Called by the hosting page with a comma-separated slug list.</summary>
        public void OMK_SetUnlocks(string csv) => Run.Scoring.UnlockState.Set(csv);
    }
}
