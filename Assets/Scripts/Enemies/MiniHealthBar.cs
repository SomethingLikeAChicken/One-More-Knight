using UnityEngine;
using OneMoreKnight.Combat;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// The miniboss HP readout (#53): a slim world-space bar floating over the
    /// sprite — deliberately not the Boss's screen-wide bar. Built from two
    /// primitive sprites at runtime (no canvas), shown only while the owning
    /// Enemy's stats flag it as a miniboss.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class MiniHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] [Min(0.1f)] private float width = 1.2f;
        [SerializeField] [Min(0.01f)] private float height = 0.12f;
        [SerializeField] private float yOffset = 0.55f;

        private static readonly Color BackColor = new Color(0.1f, 0.05f, 0.08f, 0.85f);
        private static readonly Color FillColor = new Color(0.95f, 0.3f, 0.35f);

        private Transform barRoot;
        private Transform fill;
        private SpriteRenderer ownerSprite;
        private static Sprite whiteSprite;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            ownerSprite = GetComponent<SpriteRenderer>();
            BuildBar();
            health.Changed += OnHealthChanged;
        }

        private void OnDestroy()
        {
            if (health != null) health.Changed -= OnHealthChanged;
        }

        /// <summary>Per-life switch — Enemy.Spawn calls this with the type's flag.</summary>
        public void SetVisible(bool visible)
        {
            barRoot.gameObject.SetActive(visible);
            if (visible) UpdateFill();
        }

        private void BuildBar()
        {
            if (whiteSprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            barRoot = new GameObject("MiniHpBar").transform;
            barRoot.SetParent(transform, false);

            var back = new GameObject("Back").AddComponent<SpriteRenderer>();
            back.transform.SetParent(barRoot, false);
            back.sprite = whiteSprite;
            back.color = BackColor;
            back.sortingOrder = 40;
            back.transform.localScale = new Vector3(width, height, 1f);

            var fillGo = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fillGo.transform.SetParent(barRoot, false);
            fillGo.sprite = whiteSprite;
            fillGo.color = FillColor;
            fillGo.sortingOrder = 41;
            fill = fillGo.transform;

            barRoot.gameObject.SetActive(false);
        }

        private void OnHealthChanged(Health _)
        {
            if (barRoot.gameObject.activeSelf) UpdateFill();
        }

        private void LateUpdate()
        {
            if (!barRoot.gameObject.activeSelf) return;
            // Above the sprite, unrotated and unscaled by the parent's flip/scale.
            float top = ownerSprite != null && ownerSprite.sprite != null
                ? ownerSprite.sprite.bounds.extents.y * transform.localScale.y
                : 0.5f;
            barRoot.position = transform.position + new Vector3(0f, top + yOffset, 0f);
            barRoot.rotation = Quaternion.identity;
        }

        private void UpdateFill()
        {
            float fraction = health.Max > 0 ? Mathf.Clamp01((float)health.Current / health.Max) : 0f;
            fill.localScale = new Vector3(width * fraction, height * 0.7f, 1f);
            fill.localPosition = new Vector3(-width * 0.5f * (1f - fraction), 0f, 0f);
        }
    }
}
