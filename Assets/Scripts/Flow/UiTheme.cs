using UnityEngine;

namespace OneMoreKnight.Flow
{
    /// <summary>
    /// The PixelLab UI skin (#115): one asset holding the generated font and the
    /// sliced panel/button/bar sprites, loaded from Resources so the code-built
    /// screens (Menu, HUD, GameOver) can theme themselves without scene wiring.
    /// Every consumer must survive a null Instance — the game ran themeless for
    /// fourteen releases and must still boot if the asset goes missing.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Ui Theme", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        public Font font;
        public Sprite panel;
        public Sprite buttonWood;
        public Sprite buttonDark;
        public Sprite roundEmblem;
        public Sprite bossBarFrame;
        public Sprite logo;

        private static UiTheme cached;
        private static bool searched;

        public static UiTheme Instance
        {
            get
            {
                if (!searched)
                {
                    cached = Resources.Load<UiTheme>("UiTheme");
                    searched = true;
                }
                return cached;
            }
        }
    }
}
