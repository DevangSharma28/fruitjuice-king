using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Tiny anti-aliased SDF rasteriser used to generate the game's textures, icons and UI sprites.
    /// Coordinates are in pixels, origin bottom-left.
    /// </summary>
    public class Painter
    {
        public readonly int W, H;
        readonly Color[] _px;

        public Painter(int w, int h, Color bg)
        {
            W = w;
            H = h;
            _px = new Color[w * h];
            for (int i = 0; i < _px.Length; i++) _px[i] = bg;
        }

        public Painter(int size) : this(size, size, new Color(1f, 1f, 1f, 0f)) { }

        public Color Get(int x, int y) => _px[Mathf.Clamp(y, 0, H - 1) * W + Mathf.Clamp(x, 0, W - 1)];

        public void Blend(int x, int y, Color c, float cov)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || cov <= 0f) return;
            int i = y * W + x;
            Color d = _px[i];
            float sa = c.a * Mathf.Clamp01(cov);
            float oa = sa + d.a * (1f - sa);
            if (oa <= 0.0001f)
            {
                _px[i] = new Color(0, 0, 0, 0);
                return;
            }
            float r = (c.r * sa + d.r * d.a * (1f - sa)) / oa;
            float g = (c.g * sa + d.g * d.a * (1f - sa)) / oa;
            float b = (c.b * sa + d.b * d.a * (1f - sa)) / oa;
            _px[i] = new Color(r, g, b, oa);
        }

        /// <summary>Fill a signed distance field (negative inside). Bounds limit the scan area.</summary>
        public void Fill(Func<float, float, float> sdf, Color c, float x0 = 0, float y0 = 0, float x1 = -1, float y1 = -1)
        {
            if (x1 < 0) x1 = W;
            if (y1 < 0) y1 = H;
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(x0) - 2), iy0 = Mathf.Max(0, Mathf.FloorToInt(y0) - 2);
            int ix1 = Mathf.Min(W, Mathf.CeilToInt(x1) + 2), iy1 = Mathf.Min(H, Mathf.CeilToInt(y1) + 2);
            for (int y = iy0; y < iy1; y++)
            for (int x = ix0; x < ix1; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float cov = Mathf.Clamp01(0.5f - d);
                if (cov > 0f) Blend(x, y, c, cov);
            }
        }

        /// <summary>Fill with an outline drawn first (outline grows the shape by width px).</summary>
        public void FillOutlined(Func<float, float, float> sdf, Color fill, Color outline, float width)
        {
            Fill((x, y) => sdf(x, y) - width, outline);
            Fill(sdf, fill);
        }

        public void Noise(float amount, int seed, float scale = 1f)
        {
            var rnd = new System.Random(seed);
            for (int i = 0; i < _px.Length; i++)
            {
                float n = ((float)rnd.NextDouble() - 0.5f) * amount;
                var c = _px[i];
                _px[i] = new Color(Mathf.Clamp01(c.r + n * scale), Mathf.Clamp01(c.g + n * scale), Mathf.Clamp01(c.b + n * scale), c.a);
            }
        }

        public void ForEach(Func<int, int, Color, Color> fn)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                _px[y * W + x] = fn(x, y, _px[y * W + x]);
        }

        // ------------------------------------------------------------------ SDF helpers

        public static float Circle(float x, float y, float cx, float cy, float r) =>
            Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        public static float Ring(float x, float y, float cx, float cy, float r, float thickness) =>
            Mathf.Abs(Circle(x, y, cx, cy, r)) - thickness * 0.5f;

        public static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            // Approximate distance: scale space, then re-scale by the smaller radius.
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            float k = Mathf.Sqrt(dx * dx + dy * dy);
            return (k - 1f) * Mathf.Min(rx, ry);
        }

        public static float RoundBox(float x, float y, float cx, float cy, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x - cx) - hw + r, qy = Mathf.Abs(y - cy) - hh + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        public static float Segment(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        /// <summary>Signed distance to a polygon (points in pixel coords, any winding).</summary>
        public static float Polygon(float x, float y, Vector2[] v)
        {
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = new Vector2(x, y) - v[i];
                float h = Mathf.Clamp01(Vector2.Dot(w, e) / e.sqrMagnitude);
                d = Mathf.Min(d, (w - e * h).sqrMagnitude);
                bool c1 = y >= v[i].y, c2 = y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) inside = !inside;
            }
            return (inside ? -1f : 1f) * Mathf.Sqrt(d);
        }

        public static float Union(float a, float b) => Mathf.Min(a, b);
        public static float Subtract(float a, float b) => Mathf.Max(a, -b);
        public static float Intersect(float a, float b) => Mathf.Max(a, b);

        // ------------------------------------------------------------------ output

        public Texture2D ToTexture()
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.SetPixels(_px);
            t.Apply();
            return t;
        }

        /// <summary>Write PNG and configure import settings. Returns the asset path.</summary>
        public string Save(string assetPath, bool sprite, Vector4 border = default, bool repeat = false,
            FilterMode filter = FilterMode.Bilinear, bool mipmaps = true, float pixelsPerUnit = 100f)
        {
            var tex = ToTexture();
            var bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            imp.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = !sprite && mipmaps;
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.filterMode = filter;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            if (sprite)
            {
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spriteBorder = border;
                imp.spritePixelsPerUnit = pixelsPerUnit;
                var settings = new TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(settings);
            }
            imp.SaveAndReimport();
            return assetPath;
        }
    }
}
