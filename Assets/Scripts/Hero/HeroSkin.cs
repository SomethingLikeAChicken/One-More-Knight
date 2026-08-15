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
        [Tooltip("The Hero's fire (#105): tints the flame bolts. Stays in the hero " +
                 "gold/blue color-law family - never the enemy red/violet/green.")]
        public Color fireTint = new Color(1f, 0.8f, 0.35f);
        [Tooltip("Achievement slug (site vocabulary, #63) that unlocks this skin. " +
                 "Empty = always unlocked.")]
        public string unlockAchievement;
        [Tooltip("Shown on a locked tile - tell the player what deed is missing.")]
        public string lockHint;

        [Header("Walk cycle (#131) — empty arrays fall back to the default mage frames")]
        public Sprite[] walkEast = new Sprite[0];
        public Sprite[] walkNorthEast = new Sprite[0];
        public Sprite[] walkNorth = new Sprite[0];
        public Sprite[] walkNorthWest = new Sprite[0];
        public Sprite[] walkWest = new Sprite[0];
        public Sprite[] walkSouthWest = new Sprite[0];
        public Sprite[] walkSouth = new Sprite[0];
        public Sprite[] walkSouthEast = new Sprite[0];

        /// <summary>True when every octant is authored — partial data must never
        /// half-replace the default set.</summary>
        public bool HasWalkFrames =>
            walkEast.Length > 0 && walkNorthEast.Length > 0 && walkNorth.Length > 0
            && walkNorthWest.Length > 0 && walkWest.Length > 0 && walkSouthWest.Length > 0
            && walkSouth.Length > 0 && walkSouthEast.Length > 0;
    }
}
