using UnityEngine;
using UnityEngine.Pool;

namespace OneMoreKnight.Combat
{
    /// <summary>
    /// The single spawn point for every Bullet in the game — the seam AGENTS.md asks to
    /// plant in M1. Nothing calls Instantiate on a Bullet anywhere else, so when the
    /// data-driven AttackPatternEngine arrives in M4 it replaces the <i>callers</i> of
    /// this method, not the pooling and lifetime handling underneath it.
    /// </summary>
    public class BulletSpawner : MonoBehaviour
    {
        [SerializeField] private Bullet prefab;
        [SerializeField] [Min(0.1f)] private float bulletLifetime = 4f;
        [SerializeField] [Min(1)] private int defaultCapacity = 64;
        [SerializeField] [Min(1)] private int maxSize = 512;

        private ObjectPool<Bullet> pool;

        private void Awake()
        {
            pool = new ObjectPool<Bullet>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => { if (b != null) Destroy(b.gameObject); },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        /// <summary>Spawns one Bullet. Actor-agnostic: it takes an origin and a direction,
        /// not a Hero or an Enemy (ADR-0003).</summary>
        public Bullet Spawn(Vector2 origin, Vector2 direction, float speed, int damage, LayerMask hitMask)
        {
            Bullet bullet = pool.Get();
            bullet.Arm(origin, direction, speed, damage, hitMask, bulletLifetime, pool.Release);
            return bullet;
        }
    }
}
