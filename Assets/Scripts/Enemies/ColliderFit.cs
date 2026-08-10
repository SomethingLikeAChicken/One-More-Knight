using UnityEngine;

namespace OneMoreKnight.Enemies
{
    /// <summary>
    /// Sizes an actor's circle collider from the sprite it was just assigned. One
    /// prefab plays every Enemy/Boss type (identity is data), so the collider must
    /// follow the sprite instead of staying at whatever the prefab was authored for.
    /// </summary>
    public static class ColliderFit
    {
        /// <summary>Kept slightly inside the visual: hitboxes are never larger than
        /// sprites (readability rule), so grazing a rendered edge is always safe.</summary>
        private const float Inset = 0.9f;

        public static void FitCircle(CircleCollider2D collider, Sprite sprite)
        {
            if (collider == null || sprite == null) return;
            Vector3 extents = sprite.bounds.extents;
            collider.radius = Mathf.Max(extents.x, extents.y) * Inset;
            collider.offset = sprite.bounds.center;
        }
    }
}
