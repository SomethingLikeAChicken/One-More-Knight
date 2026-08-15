using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// The Run's pause (#135): Esc or the on-screen button freezes the game behind
    /// a themed overlay; Esc, tap, or RESUME unfreezes. Code-built like the other
    /// overlays (#89 pattern). The Pact offer takes precedence — pausing is
    /// unavailable while an offer is open, and an Esc the offer consumed to refuse
    /// never also pauses (<see cref="PactOfferPanel.EscapeConsumedFrame"/>).
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private PactDirector pactDirector;

        private PactOfferPanel offerPanel;
        private GameObject pausePanel;
        private GameObject pauseButton;

        public bool IsPaused { get; private set; }

        private void Start()
        {
            if (pactDirector != null) offerPanel = pactDirector.GetComponentInChildren<PactOfferPanel>(true);
            Build();
        }

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1f;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (Time.frameCount == PactOfferPanel.EscapeConsumedFrame) return;
            Toggle();
        }

        private void Toggle()
        {
            if (runManager != null && runManager.IsOver) return;
            if (offerPanel != null && offerPanel.IsOpen) return; // the offer owns the freeze
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            pausePanel.SetActive(IsPaused);
        }

        private void Build()
        {
            var theme = Flow.UiTheme.Instance;
            Font font = theme != null && theme.font != null
                ? theme.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = new GameObject("PauseUi");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55; // above HUD, below the Pact offer (60)
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // Always-visible corner button - the touch path to pausing (#135).
            var buttonImage = NewImage(root.transform, "PauseButton", new Color(0.12f, 0.1f, 0.16f, 0.85f));
            if (theme != null && theme.buttonDark != null)
            {
                buttonImage.sprite = theme.buttonDark;
                buttonImage.type = Image.Type.Sliced;
                buttonImage.color = Color.white;
            }
            var buttonRt = buttonImage.rectTransform;
            buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(1f, 1f);
            buttonRt.sizeDelta = new Vector2(40, 34);
            buttonRt.anchoredPosition = new Vector2(-30, -24);
            pauseButton = buttonImage.gameObject;
            var button = pauseButton.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(Toggle);
            Text glyph = NewText(pauseButton.transform, "Glyph", font, 15, new Color(0.85f, 0.82f, 0.88f));
            glyph.text = "II";
            Fill(glyph.rectTransform);

            // The frozen overlay.
            pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(root.transform, false);
            var panelRt = pausePanel.AddComponent<RectTransform>();
            Fill(panelRt);
            Image dim = NewImage(pausePanel.transform, "Dim", new Color(0.02f, 0.02f, 0.05f, 0.8f));
            Fill(dim.rectTransform);
            dim.raycastTarget = true;
            Text title = NewText(pausePanel.transform, "Title", font, 30, new Color(0.93f, 0.85f, 0.6f));
            title.text = "PAUSED";
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.62f);
            titleRt.sizeDelta = new Vector2(400, 50);

            Image resume = NewImage(pausePanel.transform, "Resume", new Color(0.2f, 0.16f, 0.2f, 0.95f));
            if (theme != null && theme.buttonWood != null)
            {
                resume.sprite = theme.buttonWood;
                resume.type = Image.Type.Sliced;
                resume.color = Color.white;
            }
            var resumeRt = resume.rectTransform;
            resumeRt.anchorMin = resumeRt.anchorMax = new Vector2(0.5f, 0.44f);
            resumeRt.sizeDelta = new Vector2(240, 46);
            var resumeButton = resume.gameObject.AddComponent<Button>();
            resumeButton.targetGraphic = resume;
            resumeButton.onClick.AddListener(Toggle);
            Text resumeText = NewText(resume.transform, "Label", font, 16, new Color(0.93f, 0.88f, 0.75f));
            resumeText.text = "RESUME  (ESC)";
            Fill(resumeText.rectTransform);

            pausePanel.SetActive(false);
        }

        private static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text NewText(Transform parent, string name, Font font, int size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
