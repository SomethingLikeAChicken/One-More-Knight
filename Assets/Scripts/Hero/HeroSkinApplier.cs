using UnityEngine;

namespace OneMoreKnight.Hero
{
    /// <summary>
    /// Dresses the Hero in the selected Wardrobe skin at Run start (#89) — sprite
    /// override and tint only. The collider is deliberately NOT refit: the Hero's
    /// small hitbox is a fairness rule and no skin may change it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroSkinApplier : MonoBehaviour
    {
        [SerializeField] private SkinCatalog catalog;

        private void Awake()
        {
            HeroSkin skin = SkinSelection.Selected(catalog);
            if (skin == null) return;
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (skin.sprite != null) spriteRenderer.sprite = skin.sprite;
            spriteRenderer.color = skin.tint;
            // HeroHitFlash captures its base color in Start, after this Awake -
            // the flash and curse pulses return to the skin's tint, not white.
        }
    }
}
