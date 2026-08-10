using UnityEngine;
using OneMoreKnight.Combat;

namespace OneMoreKnight.Hero
{
    /// <summary>
    /// Strobes the Hero sprite toward red for a beat after losing HP (#40) — the
    /// on-actor half of the hit feedback; the HUD vignette is the screen half.
    /// Tick-driven, no coroutine, so scene reloads can never strand a stale tween.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroHitFlash : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] [Min(0.05f)] private float flashDuration = 0.4f;
        [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f);

        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private int lastHp = int.MaxValue;
        private float flashUntil;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
            if (health == null) health = GetComponent<Health>();
            health.Changed += OnHealthChanged;
            lastHp = health.Current;
        }

        private void OnDestroy()
        {
            if (health != null) health.Changed -= OnHealthChanged;
        }

        private void OnHealthChanged(Health source)
        {
            // Only a LOSS is a hit — heals and debug resets stay silent.
            if (source.Current < lastHp) flashUntil = Time.time + flashDuration;
            lastHp = source.Current;
        }

        private static readonly Color CurseColor = new Color(0.6f, 0.3f, 0.9f);
        private HeroUpgrades upgrades;

        private void Update()
        {
            if (upgrades == null) upgrades = GetComponent<HeroUpgrades>();

            if (Time.time < flashUntil)
            {
                float pulse = Mathf.PingPong(Time.time * 14f, 1f);
                spriteRenderer.color = Color.Lerp(baseColor, flashColor, pulse);
            }
            else if (upgrades != null && upgrades.ActiveCurse != CurseType.None)
            {
                // Cursed (#55): a slower violet pulse - clearly not the red hit strobe.
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                spriteRenderer.color = Color.Lerp(baseColor, CurseColor, pulse * 0.7f);
            }
            else if (spriteRenderer.color != baseColor)
            {
                spriteRenderer.color = baseColor;
            }
        }
    }
}
