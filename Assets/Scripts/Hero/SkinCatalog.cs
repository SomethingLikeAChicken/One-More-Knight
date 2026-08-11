using UnityEngine;

namespace OneMoreKnight.Hero
{
    /// <summary>The Wardrobe's contents (#89), in display order. First entry is the
    /// default Knight and must stay always-unlocked.</summary>
    [CreateAssetMenu(menuName = "One More Knight/Skin Catalog", fileName = "SkinCatalog")]
    public class SkinCatalog : ScriptableObject
    {
        public HeroSkin[] skins = new HeroSkin[0];
    }
}
