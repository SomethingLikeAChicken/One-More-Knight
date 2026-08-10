using UnityEngine;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// Shifts the Game camera's background color after every Boss defeat — the
    /// visible "we reached a point" cue (issue #30). The palette index follows
    /// <see cref="BossDirector.BossesDefeated"/> and clamps at the last entry;
    /// the transition lerps over a few seconds, tick-driven.
    /// </summary>
    public class BackdropController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BossDirector bossDirector;
        [Tooltip("Index 0 is the Run's starting color; each Boss defeat advances one entry.")]
        [SerializeField] private Color[] palette = new Color[0];
        [SerializeField] [Min(0.1f)] private float transitionSeconds = 2.5f;

        private Color from;
        private Color to;
        private float transitionT = 1f;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (palette.Length > 0) targetCamera.backgroundColor = palette[0];
            from = to = targetCamera.backgroundColor;
            bossDirector.BossDefeated += OnBossDefeated;
        }

        private void OnDestroy()
        {
            if (bossDirector != null) bossDirector.BossDefeated -= OnBossDefeated;
        }

        private void OnBossDefeated()
        {
            if (palette.Length == 0) return;
            int index = Mathf.Min(bossDirector.BossesDefeated, palette.Length - 1);
            from = targetCamera.backgroundColor;
            to = palette[index];
            transitionT = 0f;
        }

        private void Update()
        {
            if (transitionT >= 1f) return;
            transitionT = Mathf.Min(1f, transitionT + Time.deltaTime / transitionSeconds);
            targetCamera.backgroundColor = Color.Lerp(from, to, transitionT);
        }
    }
}
