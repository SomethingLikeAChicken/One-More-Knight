using UnityEngine;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// Plays a looping Sprite flipbook on the actor's SpriteRenderer (#116). The
    /// frames come from the actor's stats asset — identity is data, like the sprite
    /// itself. Tick-driven, no coroutines (ADR-0003 invariant): pooled actors call
    /// <see cref="PlayLoop"/> on every (re)spawn, so a recycled instance never
    /// inherits the previous life's clock.
    ///
    /// Writes only <c>.sprite</c> — the colour channels (curse pulse, telegraph
    /// tint, Boss Phase tint) keep working on top of the animation untouched.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteLoopAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private float framesPerSecond;
        private float clock;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        /// <summary>Starts the loop from frame 0. Fewer than two frames means
        /// "static sprite" — the animator goes idle and whatever the spawner set
        /// on the renderer stays.</summary>
        public void PlayLoop(Sprite[] loopFrames, float fps)
        {
            frames = loopFrames != null && loopFrames.Length > 1 ? loopFrames : null;
            framesPerSecond = fps;
            clock = 0f;
        }

        /// <summary>Goes idle without touching the renderer.</summary>
        public void Stop() => frames = null;

        private void Update()
        {
            if (frames == null) return;
            clock += Time.deltaTime * framesPerSecond;
            spriteRenderer.sprite = frames[(int)clock % frames.Length];
        }
    }
}
