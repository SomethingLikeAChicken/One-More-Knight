using UnityEngine;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// The rectangle a Run is played inside, derived from the orthographic camera.
    /// Hero movement is clamped to it; Enemies spawn above it and despawn below it.
    /// Recalculated every frame so a browser window resize cannot leave it stale —
    /// WebGL is the delivery target (ADR-0002), and there the viewport does change.
    /// </summary>
    public class PlayArea : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private float horizontalMargin = 0.5f;
        [SerializeField] private float verticalMargin = 0.5f;
        [SerializeField] private float offscreenBand = 1.5f;

        public Rect Bounds { get; private set; }

        /// <summary>Just above the visible area — where Enemies enter.</summary>
        public float SpawnLineY => Bounds.yMax + offscreenBand;

        /// <summary>Just below the visible area — where Enemies give up.</summary>
        public float DespawnLineY => Bounds.yMin - offscreenBand;

        private void Awake() => Recalculate();

        private void Update() => Recalculate();

        public void Recalculate()
        {
            if (view == null) view = Camera.main;
            if (view == null)
            {
                Bounds = Rect.MinMaxRect(-8f, -4.5f, 8f, 4.5f);
                return;
            }

            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            Vector3 centre = view.transform.position;

            Bounds = Rect.MinMaxRect(
                centre.x - halfWidth + horizontalMargin,
                centre.y - halfHeight + verticalMargin,
                centre.x + halfWidth - horizontalMargin,
                centre.y + halfHeight - verticalMargin);
        }

        public Vector2 Clamp(Vector2 position) => new Vector2(
            Mathf.Clamp(position.x, Bounds.xMin, Bounds.xMax),
            Mathf.Clamp(position.y, Bounds.yMin, Bounds.yMax));
    }
}
