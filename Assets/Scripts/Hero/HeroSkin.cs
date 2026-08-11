using UnityEngine;

namespace OneMoreKnight.Hero
{
    /// <summary>
    /// One Wardrobe skin (#89): presentation-only identity for the Hero. Shared,
    /// read-only asset (ADR-0003 rules). The Hero's HITBOX never changes with the
    /// skin — hitbox-smaller-than-sprite is a fairness rule, not a look.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Hero Skin", fileName = "HeroSkin")]
    public class HeroSkin : ScriptableObject
    {
        [Tooltip("Shown on the Wardrobe tile.")]
        public string displayName = "Knight";
        [Tooltip("Sprite override; null keeps the scene's default Hero sprite.")]
        public Sprite sprite;
        public Color tint = Color.white;
        [Tooltip("Achievement slug (site vocabulary, #63) that unlocks this skin. " +
                 "Empty = always unlocked.")]
        public string unlockAchievement;
        [Tooltip("Shown on a locked tile - tell the player what deed is missing.")]
        public string lockHint;
    }
}
