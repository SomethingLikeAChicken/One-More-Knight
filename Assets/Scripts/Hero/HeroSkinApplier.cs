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

        /// <summary>The applied skin's flame color (#105) — HeroController tints
        /// the fire bolts with it. Gold flame until a skin is applied.</summary>
        public Color FireTint { get; private set; } = new Color(1f, 0.8f, 0.35f);

        private void Awake()
        {
            HeroSkin skin = SkinSelection.Selected(catalog);
            if (skin == null) return;
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (skin.sprite != null) spriteRenderer.sprite = skin.sprite;
            spriteRenderer.color = skin.tint;
            FireTint = skin.fireTint;
            // The skin's own walk cycle, when authored (#131) - falls back to the
            // default mage frames otherwise.
            var walk = GetComponent<HeroWalkAnimation>();
            if (walk != null) walk.ApplySkin(skin);
            // HeroHitFlash captures its base color in Start, after this Awake -
            // the flash and curse pulses return to the skin's tint, not white.
        }
    }
}
