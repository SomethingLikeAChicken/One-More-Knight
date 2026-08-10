using UnityEngine;
using UnityEngine.InputSystem;
using OneMoreKnight.Combat;
using OneMoreKnight.Run;

namespace OneMoreKnight.Hero
{
    /// <summary>
    /// The player-controlled Hero (CONTEXT.md). Full 2D movement constrained to the
    /// PlayArea — deliberately not Galaga's horizontal-only rail, because RotMG-style
    /// patterns need dodging on both axes (AGENTS.md, locked design decision).
    ///
    /// Bindings come from Assets/Settings/InputSystem_Actions.inputactions rather than
    /// raw key polling, so rebinding and gamepad support are already a data change.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HeroController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayArea playArea;
        [SerializeField] private BulletSpawner bulletSpawner;
        [SerializeField] private Health health;

        [Header("Movement")]
        [SerializeField] [Min(0f)] private float moveSpeed = 8f;

        [Header("Attack")]
        [SerializeField] [Min(0.01f)] private float fireCooldown = 0.14f;
        [SerializeField] [Min(0f)] private float bulletSpeed = 16f;
        [SerializeField] [Min(1)] private int bulletDamage = 1;
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0f, 0.45f);
        [SerializeField] private LayerMask bulletHitMask;

        private InputAction moveAction;
        private InputAction attackAction;
        private HeroUpgrades upgrades;
        private float nextFireTime;
        private bool acceptsInput = true;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            health.Died += OnDied;

            InputActionMap player = inputActions.FindActionMap("Player", true);
            moveAction = player.FindAction("Move", true);
            attackAction = player.FindAction("Attack", true);
            upgrades = GetComponent<HeroUpgrades>(); // optional (#55)
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            attackAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            attackAction?.Disable();
        }

        private void Update()
        {
            if (!acceptsInput) return;

            float speedMult = upgrades != null ? upgrades.MoveSpeedMultiplier : 1f;
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector2 target = (Vector2)transform.position + input * (moveSpeed * speedMult * Time.deltaTime);
            transform.position = playArea.Clamp(target);

            if (attackAction.IsPressed() && Time.time >= nextFireTime) Fire();
        }

        private void Fire()
        {
            float cooldownMult = upgrades != null ? upgrades.FireCooldownMultiplier : 1f;
            int damage = Mathf.Max(1, bulletDamage + (upgrades != null ? upgrades.BonusDamage : 0));
            nextFireTime = Time.time + fireCooldown * cooldownMult;
            bulletSpawner.Spawn(
                (Vector2)transform.position + muzzleOffset,
                Vector2.up,
                bulletSpeed,
                damage,
                bulletHitMask);
        }

        private void OnDied(Health _)
        {
            acceptsInput = false;
        }
    }
}
