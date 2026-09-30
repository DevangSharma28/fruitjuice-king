using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Which sprites are cut from which atlas. Seeds are pixel coordinates (top-left origin) of a point on the item.
    /// Sources: the user's Assets/Game/UI/Atlas1-5 (Atlas4 = the Berry Blast theme, names prefixed "b_"; Atlas5 = the
    /// background panels, prefixed "p_") and the 2D Mobile Game UI Kit sheets (coords given at 1600 px width).
    /// </summary>
    public static class UIKit
    {
        const string A1 = "Assets/Game/UI/Atlas1.png";
        const string A2 = "Assets/Game/UI/Atlas2.png";
        const string A3 = "Assets/Game/UI/Atlas3.png";
        const string A4 = "Assets/Game/UI/Atlas4.png";
        const string A5 = "Assets/Game/UI/Atlas5.png";
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

        /// <summary>Atlas items whose decorations touch a neighbour: keep their flood fill inside a box.</summary>
        static void RegisterClips()
        {
            UIAtlasCutter.Clip(A4, 190, 150, new RectInt(0, 0, 382, 243));
            UIAtlasCutter.Clip(A4, 140, 385, new RectInt(0, 243, 282, 190));
            UIAtlasCutter.Clip(A5, 180, 180, new RectInt(0, 0, 362, 306));
            UIAtlasCutter.Clip(A5, 378, 450, new RectInt(258, 306, 240, 264));
            UIAtlasCutter.Clip(A5, 755, 845, new RectInt(612, 798, 288, 79));
            UIAtlasCutter.Clip(A5, 740, 955, new RectInt(550, 886, 380, 138));
        }

        static void Define()
        {
            RegisterClips();
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

            // ---- Berry Blast theme (Atlas4): panels, ribbons, buttons and icons used by world 2's UI
            P(A4, "b_panel_awning", 190, 150);
            P(A4, "b_panel_red", 735, 150);
            P(A4, "b_panel_blue", 1195, 150);
            P(A4, "b_panel_wood", 1405, 150);
            P(A4, "b_panel_fox", 140, 385);
            P(A4, "b_plank", 580, 300);
            P(A4, "b_ribbon_red", 810, 305);
            P(A4, "b_ribbon_blue", 1015, 305);
            P(A4, "b_ribbon_yellow", 1180, 305);
            P(A4, "b_btn_green", 375, 380, Auto);
            P(A4, "b_btn_yellow", 560, 380, Auto);
            P(A4, "b_btn_blue", 722, 380, Auto);
            P(A4, "b_btn_red", 878, 380, Auto);
            P(A4, "b_btn_purple", 1027, 380, Auto);
            P(A4, "b_btn_orange", 375, 452, Auto);
            P(A4, "b_btn_dark", 560, 452, Auto);
            P(A4, "b_btn_cream", 722, 452, Auto);
            P(A4, "b_btn_check", 1195, 578, Auto);
            P(A4, "b_btn_cross", 1310, 578, Auto);
            P(A4, "b_icon_apple", 55, 645);
            P(A4, "b_icon_strawberry", 1068, 655);
            P(A4, "b_icon_raspberry", 1130, 655);
            P(A4, "b_icon_blueberry", 1190, 665);
            P(A4, "b_icon_cranberry", 1250, 655);
            P(A4, "b_icon_videoad", 822, 800);
            P(A4, "b_icon_lock", 675, 800);
            P(A4, "b_icon_timer", 1225, 722);
            P(A4, "b_bubble_cream", 170, 970);

            // ---- background panels (Atlas5, hi-res): every popup / card / plank in the game, names prefixed "p_"
            P(A5, "p_awning", 180, 180);
            P(A5, "p_rope", 485, 190);
            P(A5, "p_red", 740, 190);
            P(A5, "p_gold", 995, 190);
            P(A5, "p_awning_wide", 1325, 180);
            P(A5, "p_plank", 135, 460);
            P(A5, "p_list", 378, 450);
            P(A5, "p_grid", 630, 450);
            P(A5, "p_gold2", 880, 450);
            P(A5, "p_awning_small", 1130, 460);
            P(A5, "p_vine", 1392, 500);
            P(A5, "p_banner_red", 185, 648);
            P(A5, "p_sign_flowers", 505, 625);
            P(A5, "p_pill_flowers", 800, 630);
            P(A5, "p_sign_wood", 1100, 625);
            P(A5, "p_plank_small", 1118, 705);
            P(A5, "p_card", 112, 770);
            P(A5, "p_card_leafy", 200, 848);
            P(A5, "p_card_wood", 755, 845);
            P(A5, "p_crown", 1340, 835);
            P(A5, "p_sign_hang", 740, 955);
            P(A5, "p_scroll", 285, 955);

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
            { "b_panel_wood", (A4, new Vector2Int(1405, 150), new Vector2Int(1492, 35)) },
            { "b_panel_fox", (A4, new Vector2Int(140, 385), new Vector2Int(245, 259)) },
        };

        /// <summary>UV (bottom-left origin) of a panel's painted X inside its cut sprite.</summary>
        public static Vector2 CloseUV(string panel)
        {
            if (!CloseButtons.TryGetValue(panel, out var c)) return new Vector2(0.92f, 0.9f);
            RegisterClips();
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
            // Berry theme (Atlas4). The fox panel's left border covers the fox so only the plain strip right of it stretches.
            { "b_panel_awning", new Vector4(100, 66, 110, 128) },
            { "b_panel_wood", new Vector4(84, 74, 88, 86) },
            { "b_panel_fox", new Vector4(158, 26, 77, 112) },
            { "b_ribbon_red", new Vector4(62, 22, 62, 58) },
            // Background panels (Atlas5): corners hold the berry / leaf clusters and the header, the middle is plain cream.
            { "p_awning", new Vector4(95, 72, 112, 118) },
            { "p_red", new Vector4(76, 66, 80, 108) },
            { "p_plank", new Vector4(54, 50, 56, 84) },
            { "p_rope", new Vector4(66, 50, 64, 90) },
            { "p_gold", new Vector4(52, 44, 52, 88) },
            { "p_card_wood", new Vector4(30, 28, 30, 28) },
            { "p_card", new Vector4(28, 24, 28, 24) },
            { "p_sign_wood", new Vector4(66, 40, 68, 40) },
        };

        /// <summary>True when the panel has a painted close X (see CloseButtons); otherwise the builder adds a visible one.</summary>
        public static bool HasCloseButton(string panel) => CloseButtons.ContainsKey(panel);

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
