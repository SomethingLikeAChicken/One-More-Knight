using UnityEngine;

namespace OneMoreKnight.Hero
{
    /// <summary>
    /// The visible Aegis ward (#83): a blue ring around the Hero while
    /// <see cref="Combat.Health.Shield"/> holds, flashing out when the ward is spent
    /// or expires. Runtime-built like RiftMarker — the ring sprite is generated once,
    /// no art pipeline, no prefab wiring.
    /// </summary>
    public class AegisAura : MonoBehaviour
    {
        private static readonly Color WardBlue = new Color(0.45f, 0.72f, 1f, 0.85f);
        private static Sprite ringSprite;

        private Combat.Health health;
        private SpriteRenderer spriteRenderer;
        private float age;
        private float fadingUntil;
        private bool fading;

        public static AegisAura Attach(Transform hero, Combat.Health health)
        {
            var go = new GameObject("AegisAura");
            go.transform.SetParent(hero, false);

            var aura = go.AddComponent<AegisAura>();
            aura.health = health;
            aura.spriteRenderer = go.AddComponent<SpriteRenderer>();
            aura.spriteRenderer.sprite = RingSprite();
            aura.spriteRenderer.color = WardBlue;
            aura.spriteRenderer.sortingOrder = 14;
            return aura;
        }

        private static Sprite RingSprite()
        {
            if (ringSprite != null) return ringSprite;
            const int size = 64;
            const float outer = 30f, inner = 26f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            var pixels = new Color32[size * size];
            var half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                    pixels[y * size + x] = d <= outer && d >= inner
                        ? (Color32)Color.white : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            // 32 PPU matches the art pipeline: the ring spans two world units around the Hero.
            ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                                       new Vector2(0.5f, 0.5f), 32f);
            return ringSprite;
        }

        private void Update()
        {
            if (fading)
            {
                float remaining = fadingUntil - Time.time;
                if (remaining <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
                float t = remaining / 0.25f; // 1 -> 0
                spriteRenderer.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.white, t);
                transform.localScale = Vector3.one * (1f + (1f - t) * 0.6f);
                return;
            }

            if (health == null || health.Shield <= 0)
            {
                fading = true;
                fadingUntil = Time.time + 0.25f;
                return;
            }

            // Slow spin + soft strobe: active protection, not decoration.
            age += Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0f, 0f, age * 40f);
            float pulse = 0.7f + Mathf.Sin(age * 4f) * 0.15f;
            spriteRenderer.color = new Color(WardBlue.r, WardBlue.g, WardBlue.b, pulse);
        }
    }
}
