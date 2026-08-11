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
        [Tooltip("Optional mobile input (#97) - additive next to the action bindings.")]
        [SerializeField] private TouchControls touch;

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
        private HeroSkinApplier skinApplier;
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
            skinApplier = GetComponent<HeroSkinApplier>(); // optional (#105)
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
            bool firePressed = attackAction.IsPressed();
            // Touch (#97) adds on top of the bindings; the clamp keeps a keyboard +
            // thumb combination from doubling the speed.
            if (touch != null)
            {
                input += touch.Move;
                firePressed |= touch.Firing;
            }
            if (input.sqrMagnitude > 1f) input.Normalize();
            Vector2 target = (Vector2)transform.position + input * (moveSpeed * speedMult * Time.deltaTime);
            transform.position = playArea.Clamp(target);

            if (firePressed && Time.time >= nextFireTime) Fire();
        }

        private void Fire()
        {
            float cooldownMult = upgrades != null ? upgrades.FireCooldownMultiplier : 1f;
            int damage = Mathf.Max(1, bulletDamage + (upgrades != null ? upgrades.BonusDamage : 0));
            nextFireTime = Time.time + fireCooldown * cooldownMult;
            // The mage's fire carries the skin's flame color (#105) - the tint seam
            // has existed since #48; the Hero side finally uses it.
            bulletSpawner.Spawn(
                (Vector2)transform.position + muzzleOffset,
                Vector2.up,
                bulletSpeed,
                damage,
                bulletHitMask,
                skinApplier != null ? skinApplier.FireTint : (Color?)null);
        }

        private void OnDied(Health _)
        {
            acceptsInput = false;
        }
    }
}
