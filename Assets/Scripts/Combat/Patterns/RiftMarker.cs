using UnityEngine;

namespace OneMoreKnight.Combat.Patterns
{
    /// <summary>
    /// The rift telegraph (#70): a pulsing square outline that marks where a
    /// pattern is about to erupt. Fully self-contained — runtime-generated sprite,
    /// static spawn API, self-destroys when the telegraph elapses — so no actor
    /// needs prefab wiring to open one.
    /// </summary>
    public class RiftMarker : MonoBehaviour
    {
        private static Sprite squareSprite;

        private float dieAt;
        private SpriteRenderer sprite;

        public static void Spawn(Vector2 position, float lifetime)
        {
            var go = new GameObject("Rift");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var marker = go.AddComponent<RiftMarker>();
            marker.dieAt = Time.time + lifetime;
        }

        private void Awake()
        {
            sprite = gameObject.AddComponent<SpriteRenderer>();
            sprite.sprite = SquareOutline();
            sprite.color = new Color(0.85f, 0.6f, 1f, 0.9f);
            sprite.sortingOrder = 25;
        }

        private void Update()
        {
            float remaining = dieAt - Time.time;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            // Contracts toward the eruption + strobes: unmistakably a countdown.
            float pulse = 0.75f + 0.25f * Mathf.PingPong(Time.time * 8f, 1f);
            transform.localScale = Vector3.one * (0.8f + remaining * 0.5f);
            var c = sprite.color;
            c.a = pulse;
            sprite.color = c;
            transform.Rotate(0f, 0f, 90f * Time.deltaTime);
        }

        private static Sprite SquareOutline()
        {
            if (squareSprite != null) return squareSprite;
            const int S = 32, T = 3; // size, border thickness
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool border = x < T || x >= S - T || y < T || y >= S - T;
                    tex.SetPixel(x, y, border ? Color.white : (Color)clear);
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            squareSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 32f);
            return squareSprite;
        }
    }
}
