// The Scripts folder is one assembly (OneMoreKnight.asmdef), so the Editor/
// special-folder rule does not apply - this file must exclude itself from
// player builds explicitly.
#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using OneMoreKnight.Enemies;
using OneMoreKnight.Run;

namespace OneMoreKnight.EditorTools
{
    /// <summary>
    /// Exports the bestiary for the website (#46): one wiki.json plus a PNG per
    /// entry, straight from the ArenaCatalog so the wiki and the Arena can never
    /// disagree about the roster. Output goes to Builds/WikiExport (gitignored);
    /// the web repo copies it into public/wiki/.
    /// </summary>
    public static class WikiExport
    {
        private const string CatalogPath = "Assets/Settings/ArenaCatalog.asset";
        private const string OutDir = "Builds/WikiExport";

        [MenuItem("One More Knight/Export Wiki Data")]
        public static void Export()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ArenaCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError("WikiExport: no catalog at " + CatalogPath);
                return;
            }
            Directory.CreateDirectory(OutDir);

            var json = new StringBuilder();
            json.Append("{\"entries\":[");
            bool first = true;

            foreach (var e in catalog.enemies)
            {
                if (e == null) continue;
                if (!first) json.Append(',');
                first = false;
                json.Append("{\"slug\":\"").Append(e.name)
                    .Append("\",\"kind\":\"enemy\",\"name\":\"").Append(e.name)
                    .Append("\",\"hp\":").Append(e.maxHealth)
                    .Append(",\"speed\":").Append(e.moveSpeed.ToString("F1", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(",\"score\":").Append(e.scoreValue)
                    .Append(",\"contact\":").Append(e.contactDamage)
                    .Append(",\"shoots\":").Append(e.attack != null ? "true" : "false")
                    .Append(",\"description\":").Append(Quote(e.description))
                    .Append(",\"sprite\":\"").Append(e.name).Append(".png\"}");
                WriteSprite(e.sprite, e.name);
            }
            foreach (var b in catalog.bosses)
            {
                if (b == null) continue;
                if (!first) json.Append(',');
                first = false;
                string display = b.name.StartsWith("Boss") ? b.name.Substring(4) : b.name;
                json.Append("{\"slug\":\"").Append(b.name)
                    .Append("\",\"kind\":\"boss\",\"name\":\"").Append(display)
                    .Append("\",\"hp\":").Append(b.maxHealth)
                    .Append(",\"difficulty\":").Append(b.difficulty)
                    .Append(",\"reward\":").Append(b.scoreReward)
                    .Append(",\"contact\":").Append(b.contactDamage)
                    .Append(",\"phases\":").Append(b.phases.Length)
                    .Append(",\"description\":").Append(Quote(b.description))
                    .Append(",\"sprite\":\"").Append(b.name).Append(".png\"}");
                WriteSprite(b.sprite, b.name);
            }
            json.Append("]}");
            File.WriteAllText(Path.Combine(OutDir, "wiki.json"), json.ToString());
            Debug.Log("WikiExport: wrote " + OutDir + "/wiki.json ("
                + catalog.enemies.Length + " enemies, " + catalog.bosses.Length + " bosses)");
        }

        private static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                          .Replace("\n", " ").Replace("\r", "") + "\"";
        }

        /// <summary>Blit through a RenderTexture so non-readable textures export too.</summary>
        private static void WriteSprite(Sprite sprite, string slug)
        {
            if (sprite == null) return;
            var tex = sprite.texture;
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(OutDir, slug + ".png"), readable.EncodeToPNG());
            Object.DestroyImmediate(readable);
        }
    }
}

#endif
