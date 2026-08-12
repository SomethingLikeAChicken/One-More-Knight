using UnityEngine;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// The endless march (#115): two leapfrogging biome panels scroll down behind
    /// the arena so the host appears to advance. Each biome PNG loops seamlessly
    /// (composed from a PixelLab Wang tileset with periodic noise), and every
    /// slain MAIN boss advances to the next biome — applied only to the panel
    /// that respawns off-screen, so the land changes the way land actually does:
    /// at the horizon, never under your feet.
    /// </summary>
    public class ScrollingBackground : MonoBehaviour
    {
        [Tooltip("Biome loop, in travel order. Panel sprites must tile vertically.")]
        [SerializeField] private Sprite[] biomes;
        [Tooltip("World units per second the ground moves DOWN.")]
        [SerializeField] private float scrollSpeed = 1.6f;
        [Tooltip("Multiplied onto the panels so gameplay stays readable on top.")]
        [SerializeField] private Color dim = new Color(0.34f, 0.34f, 0.42f, 1f);
        [SerializeField] private BossDirector bossDirector;

        private readonly SpriteRenderer[] panels = new SpriteRenderer[2];
        private float panelHeight;
        private int biomeIndex;
        private int pendingBiomeIndex;

        private void Awake()
        {
            if (biomes == null || biomes.Length == 0) { enabled = false; return; }
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("BackgroundPanel" + i);
                go.transform.SetParent(transform, false);
                panels[i] = go.AddComponent<SpriteRenderer>();
                panels[i].sprite = biomes[0];
                panels[i].sortingOrder = -100;
                panels[i].color = dim;
            }
            panelHeight = biomes[0].bounds.size.y;
            panels[0].transform.localPosition = Vector3.zero;
            panels[1].transform.localPosition = new Vector3(0f, panelHeight, 0f);

            if (bossDirector == null) bossDirector = FindAnyObjectByType<BossDirector>();
            if (bossDirector != null) bossDirector.BossDefeated += AdvanceBiome;
        }

        private void OnDestroy()
        {
            if (bossDirector != null) bossDirector.BossDefeated -= AdvanceBiome;
        }

        private void AdvanceBiome()
        {
            pendingBiomeIndex = (pendingBiomeIndex + 1) % biomes.Length;
        }

        private void Update()
        {
            transform.position += Vector3.down * (scrollSpeed * Time.deltaTime);
            foreach (var panel in panels)
            {
                // camera sits at the origin; a panel is gone once its top edge
                // has scrolled a full half-height below it
                if (panel.transform.position.y < -panelHeight)
                {
                    panel.transform.position += Vector3.up * (2f * panelHeight);
                    if (pendingBiomeIndex != biomeIndex)
                    {
                        biomeIndex = pendingBiomeIndex;
                        panel.sprite = biomes[biomeIndex];
                    }
                    else
                    {
                        panel.sprite = biomes[biomeIndex];
                    }
                }
            }
        }
    }
}
