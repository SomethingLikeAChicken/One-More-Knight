using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// The Pact offer screen (#129): a code-built uGUI overlay (the #89 Menu
    /// pattern — no scene or prefab wiring) with one card per tier and a refusal
    /// row. Shown under timeScale 0; buttons, taps and the 1/2/3/Esc keys all
    /// resolve through <see cref="PactDirector.Resolve"/>. Survives a missing
    /// UiTheme like every themed screen must.
    /// </summary>
    public class PactOfferPanel : MonoBehaviour
    {
        private PactDirector director;
        private GameObject root;
        private readonly Pact[] slots = new Pact[3];
        private Text activeLine;

        private static readonly Color DimColor = new Color(0.02f, 0.02f, 0.05f, 0.82f);
        private static readonly Color CardColor = new Color(0.12f, 0.1f, 0.16f, 0.97f);
        private static readonly Color[] TierColors =
        {
            new Color(0.55f, 0.8f, 0.5f),   // easy - green
            new Color(0.95f, 0.8f, 0.35f),  // medium - gold
            new Color(0.95f, 0.4f, 0.4f)    // hard - red
        };

        public static PactOfferPanel Create(PactDirector owner)
        {
            var go = new GameObject("PactOfferPanel");
            go.transform.SetParent(owner.transform, false);
            var panel = go.AddComponent<PactOfferPanel>();
            panel.director = owner;
            return panel;
        }

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Frame on which this panel consumed an Escape press (#135) — the
        /// PauseController checks it so one Esc never both refuses and pauses.</summary>
        public static int EscapeConsumedFrame { get; private set; } = -1;

        public void Show(Pact easy, Pact medium, Pact hard,
                         float easyMult, float mediumMult, float hardMult, Pact active)
        {
            if (root == null) Build();
            slots[0] = easy; slots[1] = medium; slots[2] = hard;
            float[] mults = { easyMult, mediumMult, hardMult };
            string[] tierNames = { "EASY", "MEDIUM", "HARD" };

            for (int i = 0; i < 3; i++)
            {
                Transform card = root.transform.Find("Cards/Card" + i);
                card.gameObject.SetActive(slots[i] != null);
                if (slots[i] == null) continue;
                card.Find("Tier").GetComponent<Text>().text =
                    $"{i + 1} · {tierNames[i]}  ×{mults[i]:0.0#}";
                card.Find("Name").GetComponent<Text>().text = slots[i].displayName.ToUpperInvariant();
                card.Find("Desc").GetComponent<Text>().text = slots[i].description;
            }

            activeLine.text = active == null
                ? "No Pact holds. Refusing keeps ×1."
                : $"Breaking the {active.displayName} — choose anew or refuse to ×1.";
            root.SetActive(true);
        }

        private void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame && slots[0] != null) Pick(0);
            else if (kb.digit2Key.wasPressedThisFrame && slots[1] != null) Pick(1);
            else if (kb.digit3Key.wasPressedThisFrame && slots[2] != null) Pick(2);
            else if (kb.escapeKey.wasPressedThisFrame)
            {
                EscapeConsumedFrame = Time.frameCount;
                Refuse();
            }
        }

        private void Pick(int slot)
        {
            root.SetActive(false);
            director.Resolve(slots[slot]);
        }

        private void Refuse()
        {
            root.SetActive(false);
            director.Resolve(null);
        }

        private void Build()
        {
            var theme = Flow.UiTheme.Instance;
            Font font = theme != null && theme.font != null
                ? theme.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // uGUI needs an EventSystem for clicks; the Game scene has no menus of
            // its own, so bring one (new Input System module) if none exists.
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            root = new GameObject("Root");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // above the HUD, below nothing else
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            Image dim = NewImage(root.transform, "Dim", DimColor);
            Stretch(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.raycastTarget = true; // swallow clicks aimed at the frozen game

            Text title = NewText(root.transform, "Title", font, 26, TextAnchor.MiddleCenter,
                                 new Color(0.93f, 0.85f, 0.6f));
            title.text = "A PACT IS OFFERED";
            Place(title.rectTransform, 0.5f, 0.88f, 600, 40);

            activeLine = NewText(root.transform, "ActiveLine", font, 13, TextAnchor.MiddleCenter,
                                 new Color(0.75f, 0.72f, 0.8f));
            Place(activeLine.rectTransform, 0.5f, 0.8f, 640, 26);

            var cards = new GameObject("Cards");
            cards.transform.SetParent(root.transform, false);
            var cardsRt = cards.AddComponent<RectTransform>();
            Place(cardsRt, 0.5f, 0.5f, 900, 240);

            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                Image card = NewImage(cards.transform, "Card" + i, CardColor);
                if (theme != null && theme.panel != null)
                {
                    card.sprite = theme.panel;
                    card.type = Image.Type.Sliced;
                    card.color = Color.white;
                }
                Place(card.rectTransform, 0.5f + (i - 1) * 0.34f, 0.5f, 270, 230);
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.onClick.AddListener(() => Pick(slot));

                Text tier = NewText(card.transform, "Tier", font, 14, TextAnchor.MiddleCenter, TierColors[i]);
                Place(tier.rectTransform, 0.5f, 0.85f, 240, 24);
                Text name = NewText(card.transform, "Name", font, 17, TextAnchor.MiddleCenter, Color.white);
                Place(name.rectTransform, 0.5f, 0.66f, 240, 30);
                Text desc = NewText(card.transform, "Desc", font, 12, TextAnchor.UpperCenter,
                                    new Color(0.82f, 0.8f, 0.86f));
                Place(desc.rectTransform, 0.5f, 0.3f, 232, 110);
            }

            Image refuse = NewImage(root.transform, "Refuse", new Color(0.2f, 0.16f, 0.2f, 0.95f));
            if (theme != null && theme.buttonDark != null)
            {
                refuse.sprite = theme.buttonDark;
                refuse.type = Image.Type.Sliced;
                refuse.color = Color.white;
            }
            Place(refuse.rectTransform, 0.5f, 0.12f, 300, 44);
            var refuseButton = refuse.gameObject.AddComponent<Button>();
            refuseButton.targetGraphic = refuse;
            refuseButton.onClick.AddListener(Refuse);
            Text refuseText = NewText(refuse.transform, "Label", font, 15, TextAnchor.MiddleCenter,
                                      new Color(0.85f, 0.82f, 0.88f));
            refuseText.text = "REFUSE — FIGHT UNBOUND  ×1  (ESC)";
            Stretch(refuseText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            root.SetActive(false);
        }

        private static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text NewText(Transform parent, string name, Font font, int size,
                                    TextAnchor anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rt, float anchorX, float anchorY,
                                  float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
