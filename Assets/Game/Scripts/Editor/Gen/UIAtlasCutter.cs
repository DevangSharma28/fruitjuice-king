using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Cuts individual sprites out of packed UI atlases (the art in Assets/Game/UI and the 2D Mobile Game UI Kit sheets).
    /// Items are located by a seed point (top-left pixel coordinates, as seen in an image viewer) and isolated by
    /// flood-filling opaque pixels, so no manual rectangles are needed. Text-baked buttons can be rebuilt as blank,
    /// 9-sliceable buttons by stretching a text-free column.
    /// </summary>
    public static class UIAtlasCutter
    {
        public const string OutDir = "Assets/Game/Generated/UI/";

        class Src
        {
            public Color32[] px;
            public int w, h;
            public Color32 Get(int x, int y) => px[(h - 1 - y) * w + x]; // top-left origin
            public byte A(int x, int y) => px[(h - 1 - y) * w + x].a;
        }

        static readonly Dictionary<string, Src> Cache = new Dictionary<string, Src>();

        static Src Load(string path)
        {
            if (Cache.TryGetValue(path, out var s)) return s;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(path));
            s = new Src { px = t.GetPixels32(), w = t.width, h = t.height };
            Object.DestroyImmediate(t);
            Cache[path] = s;
            return s;
        }

        public static void ClearCache() => Cache.Clear();

        /// <summary>Flood-fill the opaque item around a seed. Returns its bounding box (top-left coords) and mask.</summary>
        static bool Isolate(Src s, int cx, int cy, int thresh, out RectInt box, out bool[] mask)
        {
            box = default;
            mask = null;
            // Nearest opaque pixel to the seed.
            int sx = -1, sy = -1;
            for (int r = 0; r <= 40 && sx < 0; r++)
            for (int dy = -r; dy <= r && sx < 0; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                int x = cx + dx, y = cy + dy;
                if (x < 0 || y < 0 || x >= s.w || y >= s.h || s.A(x, y) < thresh) continue;
                sx = x;
                sy = y;
                break;
            }
            if (sx < 0) return false;

            var seen = new bool[s.w * s.h];
            var q = new Queue<int>();
            q.Enqueue(sy * s.w + sx);
            seen[sy * s.w + sx] = true;
            int x0 = sx, x1 = sx, y0 = sy, y1 = sy;
            var cells = new List<int>(4096);
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                cells.Add(i);
                int x = i % s.w, y = i / s.w;
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
                if (y < y0) y0 = y;
                if (y > y1) y1 = y;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s.w || ny >= s.h) continue;
                    int ni = ny * s.w + nx;
                    if (seen[ni] || s.A(nx, ny) < thresh) continue;
                    seen[ni] = true;
                    q.Enqueue(ni);
                }
            }
            box = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            mask = new bool[box.width * box.height];
            foreach (int i in cells) mask[(i / s.w - y0) * box.width + (i % s.w - x0)] = true;
            return true;
        }

        /// <summary>Copy the item plus a soft halo of <paramref name="pad"/> px, everything else transparent. Output is bottom-left origin.</summary>
        static Color32[] Extract(Src s, RectInt box, bool[] mask, int pad, out int w, out int h)
        {
            w = box.width + pad * 2;
            h = box.height + pad * 2;
            var outPx = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int sx = box.x - pad + x, sy = box.y - pad + y;
                if (sx < 0 || sy < 0 || sx >= s.w || sy >= s.h) continue;
                bool near = false;
                for (int dy = -pad; dy <= pad && !near; dy++)
                for (int dx = -pad; dx <= pad; dx++)
                {
                    int mx = x - pad + dx, my = y - pad + dy;
                    if (mx < 0 || my < 0 || mx >= box.width || my >= box.height) continue;
                    if (mask[my * box.width + mx])
                    {
                        near = true;
                        break;
                    }
                }
                if (near) outPx[(h - 1 - y) * w + x] = s.Get(sx, sy);
            }
            return outPx;
        }

        static Sprite Save(string name, Color32[] px, int w, int h, Vector4 border)
        {
            Directory.CreateDirectory(OutDir);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply();
            string path = OutDir + name + ".png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.spriteBorder = border;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Marker border meaning "work the 9-slice border out from the shape's rounded corners".</summary>
        public static readonly Vector4 AutoBorder = new Vector4(-1, -1, -1, -1);

        /// <summary>Cut one item. Border is in output pixels (left, bottom, right, top); 0 = not sliced, AutoBorder = detect.</summary>
        public static Vector2Int Cut(string atlas, string name, int cx, int cy, Vector4 border = default, int thresh = 90, int pad = 3)
        {
            var s = Load(atlas);
            if (!Isolate(s, cx, cy, thresh, out var box, out var mask))
            {
                Debug.LogWarning($"[UIAtlasCutter] Nothing opaque near ({cx},{cy}) for {name}");
                return Vector2Int.zero;
            }
            var px = Extract(s, box, mask, pad, out int w, out int h);
            if (border == AutoBorder) border = DetectBorder(px, w, h);
            Save(name, px, w, h, border);
            return new Vector2Int(w, h);
        }

        /// <summary>
        /// 9-slice border from a rounded shape: how far in from each side the body reaches (almost) full extent, plus a margin.
        /// </summary>
        static Vector4 DetectBorder(Color32[] px, int w, int h)
        {
            int Col(int x)
            {
                int n = 0;
                for (int y = 0; y < h; y++) if (px[y * w + x].a > 128) n++;
                return n;
            }
            int Row(int y)
            {
                int n = 0;
                for (int x = 0; x < w; x++) if (px[y * w + x].a > 128) n++;
                return n;
            }
            int maxC = 0, maxR = 0;
            for (int x = 0; x < w; x++) maxC = Mathf.Max(maxC, Col(x));
            for (int y = 0; y < h; y++) maxR = Mathf.Max(maxR, Row(y));
            int l = 0, r = w - 1, b = 0, t = h - 1;
            while (l < w / 2 && Col(l) < maxC * 0.96f) l++;
            while (r > w / 2 && Col(r) < maxC * 0.96f) r--;
            while (b < h / 2 && Row(b) < maxR * 0.96f) b++;
            while (t > h / 2 && Row(t) < maxR * 0.96f) t--;
            const int margin = 4;
            return new Vector4(Mathf.Min(l + margin, w / 2 - 1), Mathf.Min(b + margin, h / 2 - 1),
                Mathf.Min(w - 1 - r + margin, w / 2 - 1), Mathf.Min(h - 1 - t + margin, h / 2 - 1));
        }

        /// <summary>
        /// Centre of a sub-item (e.g. a panel's painted close button) as UV inside the item cut from <paramref name="parentSeed"/>.
        /// Both are isolated with the same flood fill, so the result matches the saved sprite (which adds <paramref name="pad"/>).
        /// </summary>
        public static Vector2 LocateChild(string atlas, Vector2Int parentSeed, Vector2Int childSeed, int childThresh = 200, int thresh = 90, int pad = 3)
        {
            var s = Load(atlas);
            if (!Isolate(s, parentSeed.x, parentSeed.y, thresh, out var box, out _)) return new Vector2(0.9f, 0.9f);
            // The child is part of the parent's opaque region; isolate it by colour: flood only through similar pixels.
            var seed = s.Get(childSeed.x, childSeed.y);
            var seen = new HashSet<int>();
            var q = new Queue<Vector2Int>();
            q.Enqueue(childSeed);
            seen.Add(childSeed.y * s.w + childSeed.x);
            int x0 = childSeed.x, x1 = childSeed.x, y0 = childSeed.y, y1 = childSeed.y;
            while (q.Count > 0 && seen.Count < 40000)
            {
                var p = q.Dequeue();
                x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x);
                y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = p.x + dx, ny = p.y + dy;
                    if (nx < box.xMin || ny < box.yMin || nx >= box.xMax || ny >= box.yMax) continue;
                    int key = ny * s.w + nx;
                    if (seen.Contains(key)) continue;
                    var c = s.Get(nx, ny);
                    int d = Mathf.Abs(c.r - seed.r) + Mathf.Abs(c.g - seed.g) + Mathf.Abs(c.b - seed.b);
                    if (c.a < childThresh || d > 120) continue;
                    seen.Add(key);
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            float cxp = (x0 + x1) * 0.5f - box.x + pad, cyp = (y0 + y1) * 0.5f - box.y + pad;
            float w = box.width + pad * 2, h = box.height + pad * 2;
            // Sprite UV has a bottom-left origin.
            return new Vector2(cxp / w, 1f - cyp / h);
        }

        /// <summary>
        /// Rebuild a text-baked pill/rounded button as a blank 9-slice button: left cap + one repeated clean column + right cap.
        /// </summary>
        public static Vector2Int CutBlankButton(string atlas, string name, int cx, int cy, int thresh = 90, int pad = 3)
        {
            var s = Load(atlas);
            if (!Isolate(s, cx, cy, thresh, out var box, out var mask))
            {
                Debug.LogWarning($"[UIAtlasCutter] Nothing opaque near ({cx},{cy}) for {name}");
                return Vector2Int.zero;
            }
            var px = Extract(s, box, mask, pad, out int w, out int h);

            // Opaque height of each column (bottom-left origin output).
            int ColumnHeight(int x)
            {
                int n = 0;
                for (int y = 0; y < h; y++)
                    if (px[y * w + x].a > 128) n++;
                return n;
            }
            int maxH = 0;
            for (int x = 0; x < w; x++) maxH = Mathf.Max(maxH, ColumnHeight(x));
            // First columns (from each side) past the rounded corner, where the body is (almost) full height.
            int left = 0, right = w - 1;
            while (left < w / 2 && ColumnHeight(left) < maxH * 0.97f) left++;
            while (right > w / 2 && ColumnHeight(right) < maxH * 0.97f) right--;
            left = Mathf.Min(left + 3, w / 2 - 1);
            right = Mathf.Max(right - 3, w / 2 + 1);

            const int mid = 6;
            int ow = left + mid + (w - right);
            var o = new Color32[ow * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < left; x++) o[y * ow + x] = px[y * w + x];
                for (int x = 0; x < mid; x++) o[y * ow + left + x] = px[y * w + left];
                for (int x = right; x < w; x++) o[y * ow + left + mid + (x - right)] = px[y * w + x];
            }
            int vb = Mathf.Max(1, h / 2 - 3);
            Save(name, o, ow, h, new Vector4(left, vb, w - right, vb));
            return new Vector2Int(ow, h);
        }
    }
}
