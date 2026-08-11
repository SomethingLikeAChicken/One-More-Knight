using UnityEngine;

namespace OneMoreKnight.Hero
{
    /// <summary>Which skin the Player wears (#89) — PlayerPrefs-persisted by asset
    /// name, falling back to the catalog's first (default) entry.</summary>
    public static class SkinSelection
    {
        private const string PrefsKey = "omk-skin";

        public static HeroSkin Selected(SkinCatalog catalog)
        {
            if (catalog == null || catalog.skins.Length == 0) return null;
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            foreach (HeroSkin skin in catalog.skins)
                if (skin != null && skin.name == saved
                    && Run.Scoring.UnlockState.Has(skin.unlockAchievement))
                    return skin;
            return catalog.skins[0];
        }

        public static void Select(HeroSkin skin)
        {
            if (skin == null) return;
            PlayerPrefs.SetString(PrefsKey, skin.name);
            PlayerPrefs.Save();
        }
    }
}
