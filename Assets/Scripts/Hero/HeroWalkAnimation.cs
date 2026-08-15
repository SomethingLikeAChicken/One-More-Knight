using UnityEngine;

namespace OneMoreKnight.Hero
{
    /// <summary>
    /// The PixelLab walk cycle (#115): 8-direction, 8-frame, generated from the
    /// hero sprite itself (create_character v3 rotation + template walk). Movement
    /// is inferred from the transform so no input plumbing is touched; standing
    /// still restores whatever sprite the skin applier chose. The serialized
    /// arrays are the DEFAULT mage's frames; a skin with its own authored cycle
    /// overrides them through <see cref="ApplySkin"/> (#131).
    /// </summary>
    public class HeroWalkAnimation : MonoBehaviour
    {
        [Tooltip("Octant order: E, NE, N, NW, W, SW, S, SE — 8 frames each.")]
        [SerializeField] private Sprite[] east;
        [SerializeField] private Sprite[] northEast;
        [SerializeField] private Sprite[] north;
        [SerializeField] private Sprite[] northWest;
        [SerializeField] private Sprite[] west;
        [SerializeField] private Sprite[] southWest;
        [SerializeField] private Sprite[] south;
        [SerializeField] private Sprite[] southEast;
        [SerializeField] private float framesPerSecond = 10f;
        [Tooltip("World units/second below which the hero counts as standing.")]
        [SerializeField] private float idleSpeed = 0.4f;

        private SpriteRenderer spriteRenderer;
        private Sprite idleSprite;
        private Vector3 lastPosition;
        private float clock;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private void Start()
        {
            // captured AFTER HeroSkinApplier.Awake so the skin survives idling
            idleSprite = spriteRenderer.sprite;
            lastPosition = transform.position;
        }

        /// <summary>Swaps in a skin's own walk cycle (#131). All-or-nothing: a skin
        /// with any un-authored octant keeps the default frames, so partial data
        /// never mixes two robes mid-turn.</summary>
        public void ApplySkin(HeroSkin skin)
        {
            if (skin == null || !skin.HasWalkFrames) return;
            east = skin.walkEast;
            northEast = skin.walkNorthEast;
            north = skin.walkNorth;
            northWest = skin.walkNorthWest;
            west = skin.walkWest;
            southWest = skin.walkSouthWest;
            south = skin.walkSouth;
            southEast = skin.walkSouthEast;
        }

        private void LateUpdate()
        {
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
            if (speed < idleSpeed)
            {
                clock = 0f;
                if (idleSprite != null && spriteRenderer.sprite != idleSprite)
                    spriteRenderer.sprite = idleSprite;
                return;
            }
            Sprite[] set = PickOctant(delta);
            if (set == null || set.Length == 0) return;
            clock += Time.deltaTime * framesPerSecond;
            spriteRenderer.sprite = set[(int)clock % set.Length];
        }

        private Sprite[] PickOctant(Vector3 delta)
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            int octant = (Mathf.RoundToInt(angle / 45f) + 8) % 8;
            switch (octant)
            {
                case 0: return east;
                case 1: return northEast;
                case 2: return north;
                case 3: return northWest;
                case 4: return west;
                case 5: return southWest;
                case 6: return south;
                default: return southEast;
            }
        }
    }
}
