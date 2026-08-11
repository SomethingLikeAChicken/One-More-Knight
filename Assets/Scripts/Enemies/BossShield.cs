using UnityEngine;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// The visible ward of a shield Boss (#79): the shield sprite bobbing in front of
    /// (below) the Boss while <see cref="Combat.Health.Shield"/> holds, shattering with
    /// a flash when it breaks. Runtime-built child of the Boss like RiftMarker — no
    /// prefab or scene wiring. Purely visual: damage routing lives in Health.
    /// </summary>
    public class BossShield : MonoBehaviour
    {
        private static readonly Color WardBlue = new Color(0.45f, 0.72f, 1f, 0.95f);

        private SpriteRenderer spriteRenderer;
        private Vector3 basePosition;
        private float bobT;
        private float breakingUntil;
        private bool breaking;

        public static BossShield Spawn(Transform boss, Sprite sprite)
        {
            var go = new GameObject("BossShield");
            go.transform.SetParent(boss, false);
            // In front = toward the Hero: below the Boss centre, scaled with the body.
            go.transform.localPosition = new Vector3(0f, -0.55f, 0f);

            var shield = go.AddComponent<BossShield>();
            shield.spriteRenderer = go.AddComponent<SpriteRenderer>();
            shield.spriteRenderer.sprite = sprite;
            shield.spriteRenderer.color = WardBlue;
            // Above the Boss body so the ward reads as "in front".
            shield.spriteRenderer.sortingOrder = 12;
            shield.basePosition = go.transform.localPosition;
            return shield;
        }

        /// <summary>Shatter flash, then the object removes itself.</summary>
        public void Break()
        {
            if (breaking) return;
            breaking = true;
            breakingUntil = Time.time + 0.35f;
        }

        private void Update()
        {
            if (breaking)
            {
                float remaining = breakingUntil - Time.time;
                if (remaining <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
                float t = remaining / 0.35f; // 1 -> 0
                spriteRenderer.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.white, t);
                transform.localScale = Vector3.one * (1f + (1f - t) * 0.8f);
                return;
            }

            // Idle bob + slow strobe so the ward reads as active, not decoration.
            bobT += Time.deltaTime;
            transform.localPosition = basePosition + new Vector3(0f, Mathf.Sin(bobT * 2.2f) * 0.07f, 0f);
            float pulse = 0.85f + Mathf.Sin(bobT * 3.4f) * 0.1f;
            spriteRenderer.color = new Color(WardBlue.r, WardBlue.g, WardBlue.b, pulse);
        }
    }
}
