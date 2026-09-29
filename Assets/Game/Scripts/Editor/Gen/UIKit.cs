using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Which sprites are cut from which atlas. Seeds are pixel coordinates (top-left origin) of a point on the item.
    /// Sources: the user's Assets/Game/UI/Atlas1-3 and the 2D Mobile Game UI Kit sheets (coords given at 1600 px width).
    /// </summary>
    public static class UIKit
    {
        const string A1 = "Assets/Game/UI/Atlas1.png";
        const string A2 = "Assets/Game/UI/Atlas2.png";
        const string A3 = "Assets/Game/UI/Atlas3.png";
        const string K1 = "Assets/ThirdParty/300Mind/UI-pack_Sprite_1.png";
        const string K2 = "Assets/ThirdParty/300Mind/UI-pack_Sprite_2.png";
        const float KitScale = 3118f / 1600f;

        enum Kind { Plain, Blank }

        struct Spec
        {
            public string atlas, name;
            public int x, y;
            public Kind kind;
            public Vector4 border;
        }

        static readonly List<Spec> Specs = new List<Spec>();

        static void P(string atlas, string name, int x, int y, Vector4 border = default)
        {
            if (atlas == K1 || atlas == K2)
            {
                x = Mathf.RoundToInt(x * KitScale);
                y = Mathf.RoundToInt(y * KitScale);
            }
            Specs.Add(new Spec { atlas = atlas, name = name, x = x, y = y, kind = Kind.Plain, border = border });
        }

        static void Blank(string atlas, string name, int x, int y) => Specs.Add(new Spec { atlas = atlas, name = name, x = x, y = y, kind = Kind.Blank });

        static readonly Vector4 Auto = UIAtlasCutter.AutoBorder;

        static void Define()
        {
            Specs.Clear();
            // ---- characters, tools and props (Atlas1)
            P(A1, "icon_farmer", 145, 360);
            P(A1, "icon_girl", 335, 360);
            P(A1, "icon_worker_blue", 435, 360);
            P(A1, "icon_chainsaw", 1165, 370);
            P(A1, "icon_machine", 65, 575);
            P(A1, "icon_register", 445, 575);
            P(A1, "icon_table", 540, 578);
            P(A1, "icon_windmill", 1068, 575);
            P(A1, "icon_recycle", 1072, 360);
            P(A1, "icon_lock", 260, 680);
            P(A1, "icon_tools", 905, 950);
            P(A1, "icon_gift", 355, 950);
            P(A1, "icon_star_old", 980, 245);
            P(A1, "pin_up", 443, 845);
            P(A1, "icon_excl", 1210, 678);

            // ---- money, boosts and badges (Atlas2)
            P(A2, "icon_coin", 75, 95);
            P(A2, "icon_coins", 195, 105);
            P(A2, "icon_coinpile", 335, 100);
            P(A2, "icon_moneybag", 478, 100);
            P(A2, "icon_cash", 615, 100);
            P(A2, "icon_cashpile", 905, 120);
            P(A2, "icon_chest", 1065, 110);
            P(A2, "icon_coinsack", 85, 245);
            P(A2, "icon_lightning", 75, 405);
            P(A2, "icon_lightning_orange", 205, 405);
            P(A2, "icon_rocket", 615, 395);
            P(A2, "icon_fastforward", 755, 400);
            P(A2, "icon_stopwatch", 895, 395);
            P(A2, "icon_videoad", 1415, 400);
            P(A2, "badge_ad", 1450, 245);
            P(A2, "btn_x", 765, 540);
            P(A2, "btn_play_round", 1150, 540);
            P(A2, "icon_gear", 82, 785);
            P(A2, "icon_crown", 230, 785);
            P(A2, "icon_backpack", 378, 785);
            P(A2, "icon_recipe", 515, 780);
            P(A2, "icon_builder", 665, 780);
            P(A2, "icon_chef", 825, 785);
            P(A2, "icon_trashcan", 1075, 780);
            P(A2, "icon_crate", 1210, 785);
            P(A2, "icon_arrow_up", 80, 925);
            P(A2, "icon_star", 725, 925);
            P(A2, "icon_trophy", 975, 925);
            P(A2, "icon_warning", 1425, 925);

            // ---- panels, ribbons, buttons and bubbles (Atlas3)
            P(A3, "panel_awning", 225, 190);
            P(A3, "panel_plain", 870, 200);
            P(A3, "panel_gold", 1400, 200);
            P(A3, "plank_sign", 155, 390);
            P(A3, "plank_small", 1405, 395);
            P(A3, "ribbon_red", 430, 392, Auto);
            P(A3, "ribbon_yellow", 680, 392, Auto);
            P(A3, "ribbon_blue", 930, 392, Auto);
            P(A3, "ribbon_purple", 1180, 392, Auto);
            P(A3, "btn_green", 115, 483, Auto);
            P(A3, "btn_orange", 318, 483, Auto);
            P(A3, "btn_blue", 515, 483, Auto);
            P(A3, "btn_red", 722, 483, Auto);
            P(A3, "btn_purple", 925, 483, Auto);
            P(A3, "btn_dark", 115, 555, Auto);
            P(A3, "btn_brown", 318, 555, Auto);
            P(A3, "btn_cream", 515, 555, Auto);
            P(A3, "toggle_on", 1338, 483);
            P(A3, "toggle_off", 1338, 555);
            P(A3, "sq_blue", 72, 745, Auto);
            P(A3, "sq_green", 182, 745, Auto);
            P(A3, "sq_orange", 290, 745, Auto);
            P(A3, "sq_wood", 398, 745, Auto);
            P(A3, "sq_stone", 510, 745, Auto);
            P(A3, "btn_check", 940, 718, Auto);
            P(A3, "btn_cross", 1098, 718, Auto);
            P(A3, "btn_play", 1260, 718, Auto);
            P(A3, "tile_hazard", 920, 808);
            P(A3, "bubble_white", 262, 850);
            P(A3, "bubble_cream", 615, 855, Auto);
            P(A3, "pill_cream", 345, 960, Auto);
            P(A3, "panel_crown", 1355, 890);

            // ---- hi-res hero art (2D Mobile Game UI Kit)
            P(K1, "hero_coins", 1150, 55);
            P(K1, "hero_coins_big", 1310, 62);
            P(K1, "hero_moneybag", 1155, 180);
            P(K1, "hero_coinbox", 1310, 180);
        }

        /// <summary>Painted close buttons: seed on the red of each panel's X (top of the circle, between the strokes).</summary>
        static readonly Dictionary<string, (string atlas, Vector2Int panel, Vector2Int x)> CloseButtons = new Dictionary<string, (string, Vector2Int, Vector2Int)>
        {
            { "panel_awning", (A3, new Vector2Int(225, 190), new Vector2Int(411, 80)) },
            { "panel_plain", (A3, new Vector2Int(870, 200), new Vector2Int(985, 58)) },
            { "panel_gold", (A3, new Vector2Int(1400, 200), new Vector2Int(1505, 58)) },
        };

        /// <summary>UV (bottom-left origin) of a panel's painted X inside its cut sprite.</summary>
        public static Vector2 CloseUV(string panel)
        {
            if (!CloseButtons.TryGetValue(panel, out var c)) return new Vector2(0.92f, 0.9f);
            var uv = UIAtlasCutter.LocateChild(c.atlas, c.panel, c.x);
            UIAtlasCutter.ClearCache();
            return uv;
        }

        /// <summary>Hand-tuned 9-slice borders for the sprites that stretch (applied after cutting, in output px).</summary>
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "panel_awning", new Vector4(110, 90, 95, 112) },
            { "panel_plain", new Vector4(50, 45, 70, 92) },
            { "panel_gold", new Vector4(46, 44, 64, 96) },
            { "plank_sign", new Vector4(76, 34, 76, 34) },
            { "plank_small", new Vector4(60, 26, 60, 26) },
            { "panel_crown", new Vector4(50, 40, 50, 80) },
        };

        public static void BuildAll()
        {
            Define();
            UIAtlasCutter.ClearCache();
            // Start clean so sprites dropped from the list do not linger.
            if (Directory.Exists(UIAtlasCutter.OutDir))
                foreach (var f in Directory.GetFiles(UIAtlasCutter.OutDir, "*.png"))
                    AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            var sizes = new List<string>();
            foreach (var s in Specs)
            {
                if (!File.Exists(s.atlas))
                {
                    Debug.LogWarning("[UIKit] Missing atlas " + s.atlas);
                    continue;
                }
                Vector2Int size = s.kind == Kind.Blank
                    ? UIAtlasCutter.CutBlankButton(s.atlas, s.name, s.x, s.y)
                    : UIAtlasCutter.Cut(s.atlas, s.name, s.x, s.y, Borders.TryGetValue(s.name, out var b) ? b : s.border);
                sizes.Add(s.name + " " + size.x + "x" + size.y);
            }
            UIAtlasCutter.ClearCache();
            AssetDatabase.Refresh();
            Debug.Log("[UIKit] Cut " + sizes.Count + " sprites: " + string.Join(", ", sizes));
        }

        public static Sprite Get(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(UIAtlasCutter.OutDir + name + ".png");
            if (s == null) Debug.LogWarning("[UIKit] Missing sprite " + name);
            return s;
        }

        /// <summary>Debug: grid of every cut sprite in spec order, written to the given path.</summary>
        public static string ContactSheet(string path, int cell = 128, int cols = 10)
        {
            Define();
            int rows = Mathf.CeilToInt(Specs.Count / (float)cols);
            var sheet = new Texture2D(cols * cell, rows * cell, TextureFormat.RGBA32, false);
            var bg = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < bg.Length; i++)
            {
                int x = i % sheet.width, y = i / sheet.width;
                bool dark = ((x / 16) + (y / 16)) % 2 == 0;
                bg[i] = dark ? new Color32(70, 70, 80, 255) : new Color32(90, 90, 100, 255);
            }
            sheet.SetPixels32(bg);
            for (int i = 0; i < Specs.Count; i++)
            {
                string p = UIAtlasCutter.OutDir + Specs[i].name + ".png";
                if (!File.Exists(p)) continue;
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(p));
                int cx = (i % cols) * cell, cy = (rows - 1 - i / cols) * cell;
                float k = Mathf.Min((cell - 8f) / t.width, (cell - 8f) / t.height);
                int tw = Mathf.Max(1, (int)(t.width * k)), th = Mathf.Max(1, (int)(t.height * k));
                for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    var c = t.GetPixelBilinear((x + 0.5f) / tw, (y + 0.5f) / th);
                    int px = cx + 4 + x + (cell - 8 - tw) / 2, py = cy + 4 + y + (cell - 8 - th) / 2;
                    var d = sheet.GetPixel(px, py);
                    sheet.SetPixel(px, py, Color.Lerp(d, c, c.a));
                }
                Object.DestroyImmediate(t);
            }
            sheet.Apply();
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            return path + " (" + Specs.Count + " sprites, " + cols + " per row)";
        }
    }
}
