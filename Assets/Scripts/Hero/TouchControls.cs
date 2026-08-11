using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OneMoreKnight.Hero
{
    /// <summary>
    /// Mobile input (#97): the LEFT half of the screen is a floating virtual
    /// joystick — it anchors wherever the thumb lands — and holding anywhere on the
    /// RIGHT half fires. Feeds <see cref="HeroController"/> additively next to the
    /// Input System actions, so desktop play is untouched. Visuals are code-built
    /// (ring + knob) and only exist while a joystick touch is live.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] [Min(0.02f)] private float radiusScreenFraction = 0.11f;
        [SerializeField] [Range(0f, 0.5f)] private float deadzone = 0.12f;

        /// <summary>Normalized move vector from the joystick; zero when untouched.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>True while a fire-side touch is held.</summary>
        public bool Firing { get; private set; }

        private int joystickId = -1;
        private int fireId = -1;
        private Vector2 joystickOrigin;

        private Canvas canvas;
        private RectTransform ring;
        private RectTransform knob;
        private static Sprite ringSprite;
        private static Sprite discSprite;

        private void Update()
        {
            var screen = Touchscreen.current;
            if (screen == null)
            {
                Move = Vector2.zero;
                Firing = false;
                return;
            }

            bool joystickSeen = false;
            bool fireSeen = false;
            float radius = Screen.height * radiusScreenFraction;

            foreach (var touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;
                int id = touch.touchId.ReadValue();
                Vector2 pos = touch.position.ReadValue();

                if (id == joystickId) joystickSeen = true;
                else if (id == fireId) fireSeen = true;
                else if (joystickId == -1 && pos.x < Screen.width * 0.5f)
                {
                    joystickId = id;
                    // The device's tracked start, not first-seen: a fast flick's early
                    // travel counts even if we notice the touch a frame late.
                    joystickOrigin = touch.startPosition.ReadValue();
                    joystickSeen = true;
                }
                else if (fireId == -1 && pos.x >= Screen.width * 0.5f)
                {
                    fireId = id;
                    fireSeen = true;
                }
                else continue;

                if (id == joystickId)
                {
                    Vector2 offset = (pos - joystickOrigin) / radius;
                    float magnitude = offset.magnitude;
                    Move = magnitude < deadzone ? Vector2.zero
                        : Vector2.ClampMagnitude(offset, 1f);
                    UpdateVisuals(radius, Vector2.ClampMagnitude(pos - joystickOrigin, radius));
                }
            }

            if (!joystickSeen)
            {
                joystickId = -1;
                Move = Vector2.zero;
                if (canvas != null) canvas.enabled = false;
            }
            if (!fireSeen) fireId = -1;
            Firing = fireSeen;
        }

        private void UpdateVisuals(float radius, Vector2 knobOffset)
        {
            if (canvas == null) BuildVisuals();
            canvas.enabled = true;
            ring.position = joystickOrigin;
            ring.sizeDelta = Vector2.one * (radius * 2f);
            knob.position = joystickOrigin + knobOffset;
            knob.sizeDelta = Vector2.one * (radius * 0.55f);
        }

        private void BuildVisuals()
        {
            var go = new GameObject("TouchJoystickCanvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            ring = MakeImage(go.transform, "Ring", RingSprite(), new Color(1f, 1f, 1f, 0.25f));
            knob = MakeImage(go.transform, "Knob", DiscSprite(), new Color(0.95f, 0.85f, 0.55f, 0.55f));
        }

        private static RectTransform MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image.rectTransform;
        }

        private static Sprite RingSprite() => ringSprite != null ? ringSprite
            : ringSprite = CircleSprite(64, 29f, 32f);

        private static Sprite DiscSprite() => discSprite != null ? discSprite
            : discSprite = CircleSprite(64, 0f, 32f);

        private static Sprite CircleSprite(int size, float innerRadius, float outerRadius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                    pixels[y * size + x] = d <= outerRadius && d >= innerRadius
                        ? (Color32)Color.white : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
