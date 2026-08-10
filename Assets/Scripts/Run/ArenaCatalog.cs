using UnityEngine;
using OneMoreKnight.Enemies;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// Everything the Arena (and the wiki export) can name: the full roster as an
    /// asset, because a build has no AssetDatabase to look types up by name (#46).
    /// Shared, read-only (ADR-0003).
    /// </summary>
    [CreateAssetMenu(menuName = "One More Knight/Arena Catalog", fileName = "ArenaCatalog")]
    public class ArenaCatalog : ScriptableObject
    {
        public EnemyStats[] enemies = new EnemyStats[0];
        public BossStats[] bosses = new BossStats[0];

        public EnemyStats FindEnemy(string slug)
        {
            foreach (var e in enemies) if (e != null && e.name == slug) return e;
            return null;
        }

        public BossStats FindBoss(string slug)
        {
            foreach (var b in bosses) if (b != null && b.name == slug) return b;
            return null;
        }
    }
}
