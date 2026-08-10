using UnityEngine;
using OneMoreKnight.Hero;

namespace OneMoreKnight.Combat
{
    /// <summary>
    /// A falling powerup (#55). Golden — the hero palette says "touch this";
    /// enemy work is never gold. Drifts down with a sway, applies its type on
    /// hero contact, despawns below the play area. Instantiated per drop
    /// (drops are rare; pooling would be ceremony).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PowerupType type;
        [SerializeField] [Min(0.1f)] private float fallSpeed = 1.3f;
        [SerializeField] private float swayAmplitude = 0.5f;
        [SerializeField] private float swayFrequency = 2.2f;
        [SerializeField] private LayerMask heroMask;

        private float despawnY;
        private float anchorX;
        private float age;

        public void Arm(PowerupType powerupType, Sprite sprite, float despawnLineY, LayerMask mask)
        {
            type = powerupType;
            GetComponent<SpriteRenderer>().sprite = sprite;
            despawnY = despawnLineY;
            heroMask = mask;
            anchorX = transform.position.x;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float y = transform.position.y - fallSpeed * Time.deltaTime;
            float x = anchorX + Mathf.Sin(age * swayFrequency) * swayAmplitude;
            transform.position = new Vector3(x, y, 0f);
            // A soft shimmer so drops read as alive.
            transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(age * 6f));
            if (y < despawnY) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((heroMask.value & (1 << other.gameObject.layer)) == 0) return;
            var upgrades = other.GetComponentInParent<HeroUpgrades>();
            if (upgrades == null) return;
            upgrades.Apply(type);
            Destroy(gameObject);
        }
    }
}
