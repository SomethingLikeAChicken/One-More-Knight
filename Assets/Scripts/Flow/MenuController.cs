using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OneMoreKnight.Flow
{
    /// <summary>
    /// Title screen, uGUI (#89 — the #28 HUD decision finally reaches the Menu).
    /// The canvas is code-built like RiftMarker and friends: deterministic, no scene
    /// surgery, one serialized ref (the SkinCatalog). Hosts the Wardrobe — skin tiles
    /// unlocked by achievements, selection persisted via SkinSelection.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private Hero.SkinCatalog catalog;

        private static readonly Color Gold = new Color(0.95f, 0.85f, 0.55f);
        private static readonly Color Parchment = new Color(0.85f, 0.85f, 0.9f);
        private static readonly Color Muted = new Color(0.6f, 0.6f, 0.7f);
        private static readonly Color TileBg = new Color(0.12f, 0.12f, 0.18f, 0.9f);

        private Font font;
        private Text feedbackText;
        private readonly System.Collections.Generic.List<(Hero.HeroSkin skin, Outline outline, Image preview)> tiles
            = new System.Collections.Generic.List<(Hero.HeroSkin, Outline, Image)>();

        public void StartGame() => SceneFlow.LoadGame();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) StartGame();
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("MenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 600f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // FindAnyObjectByType: FindFirst is obsolete-as-warning in the BUILD
            // compile (zero-warnings gate) even though the editor accepts it.
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            Label(canvasGo.transform, "Title", "ONE MORE KNIGHT", 52, Gold, FontStyle.Bold,
                  new Vector2(0.5f, 0.78f), new Vector2(800f, 70f));
            Label(canvasGo.transform, "Hint", "press SPACE — or tap below", 20, Parchment, FontStyle.Normal,
                  new Vector2(0.5f, 0.66f), new Vector2(600f, 30f));
            BuildStartButton(canvasGo.transform);
            var version = Label(canvasGo.transform, "Version", GameVersion.Current, 14, Muted, FontStyle.Normal,
                  new Vector2(1f, 0f), new Vector2(300f, 24f));
            version.alignment = TextAnchor.LowerRight;
            version.rectTransform.anchoredPosition = new Vector2(-160f, 16f);

            BuildWardrobe(canvasGo.transform);
        }

        private Text Label(Transform parent, string name, string text, int size, Color color,
                           FontStyle style, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            return t;
        }

        /// <summary>The mouse/touch way in (#97) — Space and Enter keep working.</summary>
        private void BuildStartButton(Transform parent)
        {
            var go = new GameObject("StartButton");
            go.transform.SetParent(parent, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.15f, 0.22f, 0.95f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.56f);
            rt.sizeDelta = new Vector2(280f, 54f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, 2f);

            var label = Label(go.transform, "Label", "BEGIN THE RUN", 22, Gold, FontStyle.Bold,
                              new Vector2(0.5f, 0.5f), new Vector2(280f, 54f));
            label.raycastTarget = false;

            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(StartGame);
        }

        private void BuildWardrobe(Transform parent)
        {
            if (catalog == null || catalog.skins.Length == 0) return;

            Label(parent, "WardrobeTitle", "— WARDROBE —", 18, Gold, FontStyle.Bold,
                  new Vector2(0.5f, 0.45f), new Vector2(400f, 26f));

            var row = new GameObject("WardrobeRow");
            row.transform.SetParent(parent, false);
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.31f);
            rowRt.sizeDelta = new Vector2(catalog.skins.Length * 92f, 120f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            feedbackText = Label(parent, "WardrobeFeedback", "", 15, Parchment, FontStyle.Italic,
                  new Vector2(0.5f, 0.16f), new Vector2(700f, 26f));

            foreach (Hero.HeroSkin skin in catalog.skins)
            {
                if (skin == null) continue;
                BuildTile(row.transform, skin);
            }
            RefreshSelection();
        }

        private void BuildTile(Transform row, Hero.HeroSkin skin)
        {
            bool unlocked = Run.Scoring.UnlockState.Has(skin.unlockAchievement);

            var tile = new GameObject("Tile" + skin.name);
            tile.transform.SetParent(row, false);
            var bg = tile.AddComponent<Image>();
            bg.color = TileBg;
            var rt = tile.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(82f, 112f);
            var outline = tile.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, 2f);
            outline.enabled = false;

            var previewGo = new GameObject("Preview");
            previewGo.transform.SetParent(tile.transform, false);
            var preview = previewGo.AddComponent<Image>();
            preview.sprite = skin.sprite;
            preview.preserveAspect = true;
            preview.color = unlocked ? skin.tint : new Color(0.18f, 0.18f, 0.22f, 1f);
            var pRt = preview.rectTransform;
            pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.62f);
            pRt.sizeDelta = new Vector2(52f, 52f);

            var nameLabel = Label(tile.transform, "Name",
                unlocked ? skin.displayName : "???", 12,
                unlocked ? Parchment : Muted, FontStyle.Normal,
                new Vector2(0.5f, 0.18f), new Vector2(80f, 30f));
            nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            var button = tile.AddComponent<Button>();
            button.targetGraphic = bg;
            Hero.HeroSkin captured = skin;
            button.onClick.AddListener(() => OnTileClicked(captured));

            tiles.Add((skin, outline, preview));
        }

        private void OnTileClicked(Hero.HeroSkin skin)
        {
            if (!Run.Scoring.UnlockState.Has(skin.unlockAchievement))
            {
                feedbackText.text = string.IsNullOrEmpty(skin.lockHint)
                    ? "Locked — a deed still undone."
                    : "Locked — " + skin.lockHint;
                feedbackText.color = Muted;
                return;
            }
            Hero.SkinSelection.Select(skin);
            feedbackText.text = skin.displayName + " donned.";
            feedbackText.color = Gold;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            Hero.HeroSkin selected = Hero.SkinSelection.Selected(catalog);
            foreach (var (skin, outline, _) in tiles)
                outline.enabled = skin == selected;
        }
    }
}
