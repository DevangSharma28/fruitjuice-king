using System;
using UnityEngine;
using static JuiceKing.EditorTools.Painter;

namespace JuiceKing.EditorTools
{
    /// <summary>Generates every texture / sprite the game uses (no hand-made art required).</summary>
    public static class ArtGen
    {
        public const string Dir = "Assets/Game/Generated/Textures/";

        static readonly Color Outline = new Color(0.16f, 0.1f, 0.08f, 1f);

        static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        static Color Light(Color c, float k) => Color.Lerp(c, new Color(1f, 1f, 1f, c.a), k);
        static Color Dark(Color c, float k) => Color.Lerp(c, new Color(0f, 0f, 0f, c.a), k);

        public static void GenerateAll()
        {
            UISprites();
            SliceTextures();
            Icons();
            FxTextures();
            WorldTextures();
        }

        // ---------------------------------------------------------------- shape helpers

        /// <summary>Rotate an SDF by <paramref name="deg"/> degrees around (cx, cy).</summary>
        static Func<float, float, float> R(Func<float, float, float> f, float cx, float cy, float deg)
        {
            float a = -deg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            return (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                return f(cx + dx * ca - dy * sa, cy + dx * sa + dy * ca);
            };
        }

        static Func<float, float, float> Move(Func<float, float, float> f, float dx, float dy) => (x, y) => f(x - dx, y - dy);

        /// <summary>
        /// Casual-game icon shape: soft drop shadow, dark outline, vertical gradient body, top-rim highlight and bottom shade.
        /// </summary>
        static void Icon(Painter p, Func<float, float, float> sdf, Color c, float ow = 9f, float y0 = 20f, float y1 = 236f,
            bool shadow = true, bool bevel = true)
        {
            if (shadow) p.FillSoft((x, y) => sdf(x, y + 9f) - ow, C(0f, 0f, 0f, 0.28f), 8f);
            if (ow > 0f) p.Fill((x, y) => sdf(x, y) - ow, Outline);
            Color lo = Dark(c, 0.16f), hi = Light(c, 0.22f);
            p.FillFn(sdf, (x, y) => Color.Lerp(lo, hi, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(y0, y1, y))));
            if (!bevel) return;
            p.Fill((x, y) => Intersect(sdf(x, y) + 3f, -(sdf(x, y + 7f) + 3f)), C(1f, 1f, 1f, 0.42f));
            p.Fill((x, y) => Intersect(sdf(x, y) + 2f, -(sdf(x, y - 8f) + 2f)), C(0f, 0f, 0f, 0.14f));
        }

        /// <summary>Glossy highlight ellipse clipped to the shape.</summary>
        static void Gloss(Painter p, Func<float, float, float> sdf, float cx, float cy, float rx, float ry, float deg = -25f, float a = 0.5f)
        {
            var e = R((x, y) => Ellipse(x, y, cx, cy, rx, ry), cx, cy, deg);
            p.Fill((x, y) => Intersect(e(x, y), sdf(x, y) + 6f), C(1f, 1f, 1f, a));
        }

        static void Dollar(Painter p, float cx, float cy, float s, Color dc)
        {
            // Top arc keeps everything except the lower-right quadrant, bottom arc everything except upper-left: an "S".
            p.Fill((x, y) => Intersect(Ring(x, y, cx, cy + s * 0.55f, s * 0.55f, s * 0.32f), -Intersect(cx - x, y - (cy + s * 0.55f))), dc);
            p.Fill((x, y) => Intersect(Ring(x, y, cx, cy - s * 0.55f, s * 0.55f, s * 0.32f), -Intersect(x - cx, (cy - s * 0.55f) - y)), dc);
            p.Fill((x, y) => Segment(x, y, cx, cy - s * 1.45f, cx, cy + s * 1.45f, s * 0.13f), dc);
        }

        // ---------------------------------------------------------------- UI

        static void UISprites()
        {
            var p = new Painter(128);
            p.Fill((x, y) => RoundBox(x, y, 64, 64, 64, 64, 44), Color.white);
            p.Save(Dir + "ui_round.png", true, new Vector4(46, 46, 46, 46));

            // Button: darker "3D" lip, body, soft top highlight.
            p = new Painter(128);
            p.Fill((x, y) => RoundBox(x, y, 64, 60, 62, 60, 38), C(0.68f, 0.68f, 0.68f));
            p.Fill((x, y) => RoundBox(x, y, 64, 70, 62, 54, 38), Color.white);
            p.Fill((x, y) => Intersect(RoundBox(x, y, 64, 96, 50, 20, 18), RoundBox(x, y, 64, 70, 58, 50, 34)), C(1f, 1f, 1f, 0.35f));
            p.Save(Dir + "ui_button.png", true, new Vector4(44, 48, 44, 44));

            // Panel card: white with a grey lip and a faint inner top rim.
            p = new Painter(128);
            p.Fill((x, y) => RoundBox(x, y, 64, 62, 62, 62, 40), C(0.8f, 0.76f, 0.72f));
            p.Fill((x, y) => RoundBox(x, y, 64, 69, 62, 55, 40), Color.white);
            p.Save(Dir + "ui_panel.png", true, new Vector4(46, 50, 46, 46));

            // Capsule pill.
            p = new Painter(128);
            p.Fill((x, y) => RoundBox(x, y, 64, 64, 62, 44, 44), Color.white);
            p.Save(Dir + "ui_pill.png", true, new Vector4(48, 44, 48, 44));

            p = new Painter(128);
            p.Fill((x, y) => Circle(x, y, 64, 64, 62), Color.white);
            p.Save(Dir + "ui_circle.png", true);

            // Circle button with lip.
            p = new Painter(128);
            p.Fill((x, y) => Circle(x, y, 64, 60, 60), C(0.68f, 0.68f, 0.68f));
            p.Fill((x, y) => Circle(x, y, 64, 67, 57), Color.white);
            p.Fill((x, y) => Intersect(Circle(x, y, 64, 67, 50), -Circle(x, y, 64, 58, 52)), C(1f, 1f, 1f, 0.35f));
            p.Save(Dir + "ui_circle_btn.png", true);

            // 1x1 world-unit white square (bars, bubble tail).
            p = new Painter(8, 8, Color.white);
            p.Save(Dir + "white.png", true, default, false, FilterMode.Bilinear, false, 8f);

            p = new Painter(256);
            p.Fill((x, y) => Circle(x, y, 128, 128, 120), C(1, 1, 1, 0.22f));
            p.Fill((x, y) => Ring(x, y, 128, 128, 116, 10), C(1, 1, 1, 0.9f));
            p.Save(Dir + "ui_ring.png", true);

            // Thick ring for radial timers.
            p = new Painter(256);
            p.Fill((x, y) => Ring(x, y, 128, 128, 112, 22), Color.white);
            p.Save(Dir + "ui_ring_thick.png", true);

            // Diagonal shine streak.
            p = new Painter(128, 256, C(1, 1, 1, 0));
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Abs((x - 64) + (y - 128) * 0.35f) / 40f;
                return C(1, 1, 1, Mathf.Clamp01(1f - d) * 0.55f);
            });
            p.Save(Dir + "ui_shine.png", true);

            // Sunburst rays behind reward icons.
            p = new Painter(256);
            p.ForEach((x, y, c) =>
            {
                float dx = x - 127.5f, dy = y - 127.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 128f;
                float a = Mathf.Atan2(dy, dx);
                float ray = Mathf.SmoothStep(0.1f, 0.6f, Mathf.Sin(a * 12f) * 0.5f + 0.5f);
                float fall = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 3f);
                return C(1, 1, 1, ray * fall * 0.8f);
            });
            p.Save(Dir + "ui_rays.png", true);

            // Soft glow disc.
            p = new Painter(128);
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 63.5f) * (x - 63.5f) + (y - 63.5f) * (y - 63.5f)) / 63f;
                return C(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f));
            });
            p.Save(Dir + "ui_glow.png", true);

            // Soft particle / glow circle.
            p = new Painter(64);
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 31f;
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.55f, 1f, d));
                return new Color(1, 1, 1, a);
            });
            p.Save(Dir + "fx_circle.png", false, default, false, FilterMode.Bilinear, true);
        }

        // ---------------------------------------------------------------- Fruit slice faces

        static void SliceTextures()
        {
            const float c = 128f, R0 = 126f;

            // Orange wheel
            var p = new Painter(256, 256, C(1f, 0.55f, 0.05f));
            p.Fill((x, y) => Circle(x, y, c, c, R0), C(1f, 0.52f, 0.04f));
            p.Fill((x, y) => Circle(x, y, c, c, R0 - 12), C(1f, 0.93f, 0.75f));
            p.FillFn((x, y) => Circle(x, y, c, c, R0 - 18), (x, y) =>
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (R0 - 18);
                return Color.Lerp(C(1f, 0.74f, 0.25f), C(1f, 0.6f, 0.12f), d);
            });
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                float ex = c + Mathf.Cos(a) * (R0 - 18), ey = c + Mathf.Sin(a) * (R0 - 18);
                p.Fill((x, y) => Segment(x, y, c, c, ex, ey, 2.4f), C(1f, 0.93f, 0.75f));
                float ha = a + Mathf.PI / 10f;
                float hx = c + Mathf.Cos(ha) * 70, hy = c + Mathf.Sin(ha) * 70;
                p.Fill((x, y) => Ellipse(x, y, hx, hy, 12, 8), C(1f, 0.86f, 0.5f, 0.55f));
            }
            p.Fill((x, y) => Circle(x, y, c, c, 12), C(1f, 0.93f, 0.75f));
            p.Save(Dir + "slice_orange.png", false);

            // Watermelon round slice
            p = new Painter(256, 256, C(0.15f, 0.45f, 0.18f));
            p.Fill((x, y) => Circle(x, y, c, c, R0), C(0.16f, 0.46f, 0.18f));
            p.Fill((x, y) => Circle(x, y, c, c, R0 - 8), C(0.55f, 0.82f, 0.38f));
            p.Fill((x, y) => Circle(x, y, c, c, R0 - 14), C(0.95f, 0.96f, 0.82f));
            p.FillFn((x, y) => Circle(x, y, c, c, R0 - 19), (x, y) =>
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (R0 - 19);
                return Color.Lerp(C(1f, 0.36f, 0.4f), C(0.94f, 0.24f, 0.3f), d * d);
            });
            var rnd = new System.Random(7);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.2f;
                float rr = (i % 2 == 0) ? 62 : 82;
                float sx = c + Mathf.Cos(a) * rr, sy = c + Mathf.Sin(a) * rr;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                p.Fill((x, y) =>
                {
                    float dx = x - sx, dy = y - sy;
                    float lx = dx * ca + dy * sa, ly = -dx * sa + dy * ca;
                    return Ellipse(lx, ly, 0, 0, 8, 5);
                }, C(0.12f, 0.07f, 0.07f));
            }
            p.Save(Dir + "slice_watermelon.png", false);

            // Pineapple ring
            p = new Painter(256, 256, C(0.72f, 0.48f, 0.14f));
            p.Fill((x, y) => Circle(x, y, c, c, R0), C(0.7f, 0.46f, 0.12f));
            p.Fill((x, y) => Circle(x, y, c, c, R0 - 10), C(1f, 0.84f, 0.28f));
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                float sx = c + Mathf.Cos(a) * 36, sy = c + Mathf.Sin(a) * 36;
                float ex = c + Mathf.Cos(a) * (R0 - 14), ey = c + Mathf.Sin(a) * (R0 - 14);
                p.Fill((x, y) => Segment(x, y, sx, sy, ex, ey, 1.2f), C(0.95f, 0.72f, 0.18f, 0.45f));
            }
            p.Fill((x, y) => Circle(x, y, c, c, 30), C(1f, 0.94f, 0.62f));
            p.Save(Dir + "slice_pineapple.png", false);
        }

        // ---------------------------------------------------------------- Icons

        static void Icons()
        {
            // Orange
            var p = new Painter(256);
            Func<float, float, float> orange = (x, y) => Circle(x, y, 128, 114, 92);
            Icon(p, orange, C(1f, 0.56f, 0.06f));
            var rnd = new System.Random(3);
            for (int i = 0; i < 40; i++)
            {
                float px = 60 + (float)rnd.NextDouble() * 136, py = 40 + (float)rnd.NextDouble() * 150;
                p.Fill((x, y) => Intersect(Circle(x, y, px, py, 2.2f), orange(x, y) + 8), C(0.8f, 0.36f, 0f, 0.22f));
            }
            Gloss(p, orange, 94, 150, 30, 18);
            Icon(p, (x, y) => Segment(x, y, 128, 200, 122, 228, 6), C(0.45f, 0.28f, 0.12f), 5, 190, 230, false, false);
            Icon(p, R((x, y) => Ellipse(x, y, 164, 220, 36, 15), 164, 220, 28f), C(0.3f, 0.75f, 0.28f), 6, 205, 236, false);
            p.Fill(R((x, y) => Segment(x, y, 136, 220, 190, 220, 1.6f), 164, 220, 28f), C(0.2f, 0.5f, 0.2f, 0.7f));
            p.Save(Dir + "icon_orange.png", true);

            // Watermelon wedge (half disc)
            p = new Painter(256);
            Func<float, float, float, float> half = (x, y, r) => Intersect(Circle(x, y, 128, 176, r), y - 176);
            Icon(p, (x, y) => half(x, y, 116), C(0.18f, 0.5f, 0.2f), 9, 60, 176);
            p.Fill((x, y) => half(x, y, 104), C(0.55f, 0.84f, 0.38f));
            p.Fill((x, y) => half(x, y, 96), C(0.96f, 0.97f, 0.85f));
            p.FillFn((x, y) => half(x, y, 88), (x, y) => Color.Lerp(C(0.9f, 0.2f, 0.27f), C(1f, 0.36f, 0.4f), Mathf.InverseLerp(90, 176, y)));
            float[,] seeds = { { 92, 142 }, { 128, 130 }, { 164, 142 }, { 110, 106 }, { 146, 106 } };
            for (int i = 0; i < seeds.GetLength(0); i++)
            {
                float sx = seeds[i, 0], sy = seeds[i, 1];
                p.Fill((x, y) => Ellipse(x, y, sx, sy, 6, 10), C(0.12f, 0.07f, 0.07f));
                p.Fill((x, y) => Circle(x, y, sx - 2, sy + 4, 2f), C(1f, 1f, 1f, 0.45f));
            }
            p.Fill((x, y) => Intersect(Ellipse(x, y, 100, 162, 40, 8), half(x, y, 84)), C(1f, 1f, 1f, 0.35f));
            p.Save(Dir + "icon_watermelon.png", true);

            // Pineapple
            p = new Painter(256);
            for (int i = -2; i <= 2; i++)
            {
                float ang = i * 0.38f;
                float len = i == 0 ? 76 : 62 - Mathf.Abs(i) * 6;
                float tx = 128 + Mathf.Sin(ang) * len, ty = 168 + Mathf.Cos(ang) * len;
                var leaf = new[] { new Vector2(128 + i * 8 - 15, 164), new Vector2(tx, ty), new Vector2(128 + i * 8 + 15, 164) };
                Icon(p, (x, y) => Polygon(x, y, leaf), C(0.28f, 0.7f, 0.3f), 6, 160, 244, i == 0);
            }
            Func<float, float, float> pine = (x, y) => Ellipse(x, y, 128, 98, 70, 86);
            Icon(p, pine, C(1f, 0.72f, 0.18f), 9, 12, 184);
            for (int i = -4; i <= 4; i++)
            {
                float o = i * 30;
                p.Fill((x, y) => Intersect(Segment(x, y, 60 + o, 20, 196 + o, 180, 2.5f), pine(x, y) + 5), C(0.72f, 0.42f, 0.08f, 0.75f));
                p.Fill((x, y) => Intersect(Segment(x, y, 196 + o, 20, 60 + o, 180, 2.5f), pine(x, y) + 5), C(0.72f, 0.42f, 0.08f, 0.75f));
            }
            Gloss(p, pine, 100, 130, 18, 30, -15f, 0.45f);
            p.Save(Dir + "icon_pineapple.png", true);

            // Juice cup
            p = new Painter(256);
            var cup = new[] { new Vector2(82, 22), new Vector2(174, 22), new Vector2(194, 180), new Vector2(62, 180) };
            Icon(p, (x, y) => Segment(x, y, 150, 186, 184, 246, 7), C(0.95f, 0.32f, 0.38f), 5, 186, 246, false);
            Func<float, float, float> cupS = (x, y) => Polygon(x, y, cup);
            Icon(p, cupS, C(1f, 0.6f, 0.1f), 9, 22, 180);
            p.Fill((x, y) => Intersect(RoundBox(x, y, 128, 84, 80, 16, 4), cupS(x, y)), C(1f, 1f, 1f, 0.9f));
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(88, 32), new Vector2(106, 32), new Vector2(116, 168), new Vector2(84, 168) }), C(1f, 1f, 1f, 0.28f));
            Icon(p, (x, y) => RoundBox(x, y, 128, 186, 76, 13, 10), Color.white, 6, 172, 200, false);
            // Orange wheel on the rim.
            Icon(p, (x, y) => Circle(x, y, 190, 176, 30), C(1f, 0.55f, 0.08f), 6, 146, 206, false);
            p.Fill((x, y) => Circle(x, y, 190, 176, 22), C(1f, 0.85f, 0.45f));
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                float ex = 190 + Mathf.Cos(a) * 22, ey = 176 + Mathf.Sin(a) * 22;
                p.Fill((x, y) => Segment(x, y, 190, 176, ex, ey, 1.8f), C(1f, 0.97f, 0.85f));
            }
            p.Save(Dir + "icon_juice.png", true);

            // Money: two stacked bills.
            p = new Painter(256);
            DrawBill(p, 136, 148, 100, 56, 10f);
            DrawBill(p, 124, 112, 104, 58, -8f);
            p.Save(Dir + "icon_money.png", true);

            // Gold coin
            p = new Painter(256);
            DrawCoin(p, 128, 128, 104);
            p.Save(Dir + "icon_coin.png", true);

            // Double coins (2x cash)
            p = new Painter(256);
            DrawCoin(p, 152, 150, 80);
            DrawCoin(p, 104, 104, 84);
            DrawSparkle(p, 214, 214, 26, C(1f, 1f, 0.85f));
            p.Save(Dir + "icon_cash2x.png", true);

            // Money bag (free cash)
            p = new Painter(256);
            Func<float, float, float> sack = (x, y) => Union(Circle(x, y, 128, 96, 82),
                Polygon(x, y, new[] { new Vector2(92, 150), new Vector2(164, 150), new Vector2(150, 196), new Vector2(106, 196) }));
            Icon(p, sack, C(0.86f, 0.62f, 0.32f), 9, 14, 200);
            Icon(p, (x, y) => Union(Circle(x, y, 108, 214, 20), Circle(x, y, 148, 214, 20)), C(0.86f, 0.62f, 0.32f), 7, 194, 234, false);
            Icon(p, (x, y) => RoundBox(x, y, 128, 188, 38, 10, 8), C(0.95f, 0.3f, 0.3f), 6, 178, 198, false);
            Gloss(p, sack, 92, 128, 18, 30, -20f, 0.35f);
            Dollar(p, 128, 92, 30, C(0.2f, 0.55f, 0.25f));
            p.Save(Dir + "icon_moneybag.png", true);

            // Chainsaw
            p = new Painter(256);
            Icon(p, (x, y) => RoundBox(x, y, 170, 118, 70, 22, 20), C(0.76f, 0.79f, 0.84f), 9, 96, 140);
            for (int i = 0; i < 6; i++)
            {
                float tx = 116 + i * 20;
                p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(tx, 140), new Vector2(tx + 14, 140), new Vector2(tx + 7, 152) }), Outline);
                p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(tx, 96), new Vector2(tx + 14, 96), new Vector2(tx + 7, 84) }), Outline);
            }
            Icon(p, (x, y) => RoundBox(x, y, 80, 118, 56, 46, 18), C(1f, 0.5f, 0.1f), 9, 72, 164);
            Icon(p, (x, y) => Ring(x, y, 70, 176, 26, 12), C(0.28f, 0.28f, 0.32f), 4, 150, 202, false);
            p.Fill((x, y) => RoundBox(x, y, 80, 104, 40, 6, 3), C(0.2f, 0.2f, 0.24f, 0.8f));
            p.Save(Dir + "icon_saw.png", true);

            // Backpack
            p = new Painter(256);
            Icon(p, (x, y) => RoundBox(x, y, 128, 112, 74, 88, 30), C(0.62f, 0.36f, 0.2f), 9, 24, 200);
            Icon(p, (x, y) => RoundBox(x, y, 128, 168, 74, 36, 26), C(0.52f, 0.29f, 0.15f), 6, 132, 204);
            Icon(p, (x, y) => RoundBox(x, y, 128, 76, 46, 30, 12), C(0.78f, 0.5f, 0.3f), 6, 46, 106);
            p.Fill((x, y) => Circle(x, y, 128, 140, 9), C(1f, 0.82f, 0.25f));
            p.Save(Dir + "icon_bag.png", true);

            // Lightning (speed)
            p = new Painter(256);
            var bolt = new[] { new Vector2(150, 246), new Vector2(66, 124), new Vector2(120, 124), new Vector2(96, 10), new Vector2(190, 144), new Vector2(134, 144), new Vector2(172, 246) };
            Icon(p, (x, y) => Polygon(x, y, bolt), C(1f, 0.84f, 0.15f), 9, 10, 246);
            p.Save(Dir + "icon_speed.png", true);

            // Sneaker (speed upgrade)
            p = new Painter(256);
            Func<float, float, float> sole = (x, y) => RoundBox(x, y, 128, 62, 104, 18, 16);
            Func<float, float, float> upper = (x, y) => Union(RoundBox(x, y, 118, 106, 86, 40, 36),
                Union(RoundBox(x, y, 84, 138, 46, 50, 30), Circle(x, y, 196, 94, 32)));
            Icon(p, upper, C(0.95f, 0.3f, 0.32f), 9, 60, 190);
            Icon(p, sole, Color.white, 9, 40, 84, false);
            p.Fill((x, y) => RoundBox(x, y, 128, 50, 100, 6, 5), C(0.8f, 0.8f, 0.85f));
            for (int i = 0; i < 3; i++)
            {
                float lx = 110 + i * 22;
                p.Fill((x, y) => Segment(x, y, lx - 8, 118 - i * 4, lx + 10, 132 - i * 4, 4.5f), Color.white);
            }
            p.Fill((x, y) => Circle(x, y, 202, 100, 10), C(1f, 1f, 1f, 0.5f));
            for (int i = 0; i < 3; i++)
            {
                float ly = 90 + i * 34;
                p.FillSoft((x, y) => Segment(x, y, 12, ly, 44 - i * 6, ly, 6f), C(0.45f, 0.8f, 1f, 0.9f), 3f);
            }
            p.Save(Dir + "icon_sneaker.png", true);

            // Price tag (recipe upgrade)
            p = new Painter(256);
            Func<float, float, float> tag = R((x, y) => Polygon(x, y, new[]
            {
                new Vector2(70, 50), new Vector2(186, 50), new Vector2(186, 170), new Vector2(128, 228), new Vector2(70, 170)
            }) - 10f, 128, 128, -35f);
            var hole = R((x, y) => Circle(x, y, 128, 186, 14), 128, 128, -35f);
            Icon(p, (x, y) => Subtract(tag(x, y), hole(x, y)), C(0.98f, 0.36f, 0.45f));
            Dollar(p, 118, 110, 34, C(1f, 1f, 1f, 0.95f));
            p.Save(Dir + "icon_price.png", true);

            // Worker (hire)
            p = new Painter(256);
            Icon(p, (x, y) => RoundBox(x, y, 128, 66, 72, 56, 40), C(0.25f, 0.6f, 0.95f), 9, 10, 122);
            Icon(p, (x, y) => Circle(x, y, 128, 166, 52), C(1f, 0.8f, 0.62f), 9, 114, 218);
            Icon(p, (x, y) => Intersect(Circle(x, y, 128, 176, 56), 184 - y), C(0.95f, 0.35f, 0.3f), 6, 180, 232, false);
            p.Fill((x, y) => Circle(x, y, 108, 160, 6), Outline);
            p.Fill((x, y) => Circle(x, y, 148, 160, 6), Outline);
            p.Fill((x, y) => Intersect(Ring(x, y, 128, 150, 18, 5), y - 146), Outline);
            p.Save(Dir + "icon_worker.png", true);

            // Star (decor)
            p = new Painter(256);
            var star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 112 : 50;
                star[i] = new Vector2(128 + Mathf.Cos(a) * r, 120 + Mathf.Sin(a) * r);
            }
            Func<float, float, float> starS = (x, y) => Polygon(x, y, star) + 4f;
            Icon(p, (x, y) => starS(x, y) - 4f, C(1f, 0.8f, 0.18f), 9, 20, 232);
            Gloss(p, starS, 104, 150, 22, 12, -20f, 0.45f);
            p.Save(Dir + "icon_star.png", true);

            // Heart (happy customer)
            p = new Painter(256);
            Func<float, float, float> heart = (x, y) => Union(Union(Circle(x, y, 90, 158, 56), Circle(x, y, 166, 158, 56)),
                Polygon(x, y, new[] { new Vector2(40, 140), new Vector2(216, 140), new Vector2(128, 26) }));
            p.FillSoft((x, y) => heart(x, y + 8) - 12, C(0f, 0f, 0f, 0.25f), 8f);
            p.Fill((x, y) => heart(x, y) - 12, Color.white);
            Icon(p, heart, C(1f, 0.32f, 0.42f), 0, 30, 214, false);
            Gloss(p, heart, 84, 172, 22, 14, -30f, 0.55f);
            p.Save(Dir + "icon_heart.png", true);

            // Crown (shop level)
            p = new Painter(256);
            Func<float, float, float> crown = (x, y) => Union(
                Polygon(x, y, new[] { new Vector2(40, 70), new Vector2(216, 70), new Vector2(226, 190), new Vector2(172, 130), new Vector2(128, 210), new Vector2(84, 130), new Vector2(30, 190) }),
                RoundBox(x, y, 128, 60, 90, 20, 8));
            Icon(p, crown, C(1f, 0.78f, 0.16f), 9, 40, 210);
            foreach (var tip in new[] { new Vector2(30, 194), new Vector2(128, 214), new Vector2(226, 194) })
            {
                var tp = tip;
                Icon(p, (x, y) => Circle(x, y, tp.x, tp.y, 14), C(1f, 0.85f, 0.3f), 6, tp.y - 14, tp.y + 14, false);
            }
            Icon(p, (x, y) => Circle(x, y, 128, 104, 16), C(0.95f, 0.25f, 0.35f), 5, 88, 120, false);
            Icon(p, (x, y) => Circle(x, y, 76, 96, 11), C(0.3f, 0.65f, 1f), 4, 85, 107, false);
            Icon(p, (x, y) => Circle(x, y, 180, 96, 11), C(0.35f, 0.85f, 0.45f), 4, 85, 107, false);
            p.Save(Dir + "icon_crown.png", true);

            // Trash bin
            p = new Painter(256);
            Func<float, float, float> bin = (x, y) => Polygon(x, y, new[] { new Vector2(66, 16), new Vector2(190, 16), new Vector2(206, 176), new Vector2(50, 176) }) - 6f;
            Icon(p, bin, C(0.3f, 0.7f, 0.42f), 9, 16, 176);
            for (int i = -1; i <= 1; i++)
            {
                float lx = 128 + i * 38;
                p.Fill((x, y) => Segment(x, y, lx - i * 2, 40, lx + i * 3, 150, 6), C(0.2f, 0.5f, 0.3f, 0.7f));
            }
            Icon(p, (x, y) => RoundBox(x, y, 128, 192, 92, 16, 10), C(0.24f, 0.6f, 0.36f), 8, 176, 208);
            Icon(p, (x, y) => RoundBox(x, y, 128, 222, 26, 10, 8), C(0.24f, 0.6f, 0.36f), 6, 212, 232, false);
            p.Save(Dir + "icon_trash.png", true);

            // Rewarded ad badge: a little TV with a play button.
            p = new Painter(256);
            Func<float, float, float> tv = (x, y) => RoundBox(x, y, 128, 118, 108, 84, 30);
            Icon(p, tv, C(1f, 0.45f, 0.2f), 10, 34, 202);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(104, 72), new Vector2(104, 164), new Vector2(180, 118) }) - 6f, Outline);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(104, 72), new Vector2(104, 164), new Vector2(180, 118) }), Color.white);
            p.Save(Dir + "icon_ad.png", true);

            // Rocket (turbo)
            p = new Painter(256);
            const float rot = -40f;
            Func<float, float, float> flame = R((x, y) => Polygon(x, y, new[] { new Vector2(104, 64), new Vector2(152, 64), new Vector2(128, 6) }), 128, 128, rot);
            Icon(p, flame, C(1f, 0.6f, 0.15f), 7, 6, 64, false);
            p.Fill(R((x, y) => Polygon(x, y, new[] { new Vector2(114, 64), new Vector2(142, 64), new Vector2(128, 28) }), 128, 128, rot), C(1f, 0.92f, 0.4f));
            Func<float, float, float> fins = R((x, y) => Union(
                Polygon(x, y, new[] { new Vector2(96, 70), new Vector2(60, 50), new Vector2(72, 120), new Vector2(96, 130) }),
                Polygon(x, y, new[] { new Vector2(160, 70), new Vector2(196, 50), new Vector2(184, 120), new Vector2(160, 130) })), 128, 128, rot);
            Icon(p, fins, C(0.95f, 0.3f, 0.35f), 8, 40, 130);
            Func<float, float, float> hull = R((x, y) => Union(Ellipse(x, y, 128, 130, 40, 96), RoundBox(x, y, 128, 80, 36, 18, 10)), 128, 128, rot);
            Icon(p, hull, C(0.92f, 0.94f, 0.98f), 9, 40, 226);
            Icon(p, R((x, y) => Circle(x, y, 128, 150, 18), 128, 128, rot), C(0.35f, 0.7f, 1f), 6, 130, 170, false);
            p.Save(Dir + "icon_turbo.png", true);

            // Play glyph
            p = new Painter(128);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(34, 18), new Vector2(34, 110), new Vector2(112, 64) }) - 4f, Color.white);
            p.Save(Dir + "icon_play.png", true);

            // Sound on / off
            for (int k = 0; k < 2; k++)
            {
                p = new Painter(256);
                var spk = new[] { new Vector2(34, 96), new Vector2(84, 96), new Vector2(140, 44), new Vector2(140, 212), new Vector2(84, 160), new Vector2(34, 160) };
                Func<float, float, float> glyph;
                if (k == 0)
                    glyph = (x, y) => Union(Polygon(x, y, spk), Union(
                        Intersect(Ring(x, y, 132, 128, 48, 14), 150 - x + Mathf.Abs(y - 128) * 0.9f),
                        Intersect(Ring(x, y, 132, 128, 86, 14), 160 - x + Mathf.Abs(y - 128) * 0.9f)));
                else
                    glyph = (x, y) => Union(Polygon(x, y, spk), Union(Segment(x, y, 160, 90, 224, 166, 9), Segment(x, y, 160, 166, 224, 90, 9)));
                p.FillSoft((x, y) => glyph(x, y + 6f), C(0f, 0f, 0f, 0.3f), 6f);
                p.Fill(glyph, Color.white);
                p.Save(Dir + (k == 0 ? "icon_sound_on.png" : "icon_sound_off.png"), true);
            }
        }

        static void DrawCoin(Painter p, float cx, float cy, float r)
        {
            Func<float, float, float> coin = (x, y) => Circle(x, y, cx, cy, r);
            // Coin edge (thickness) below the face.
            p.FillSoft((x, y) => Circle(x, y, cx, cy - 14, r) - 9f, C(0f, 0f, 0f, 0.25f), 8f);
            p.Fill((x, y) => Circle(x, y, cx, cy - 10, r) - 9f, Outline);
            p.Fill((x, y) => Circle(x, y, cx, cy - 10, r), C(0.85f, 0.55f, 0.08f));
            Icon(p, coin, C(1f, 0.8f, 0.2f), 9, cy - r, cy + r, false);
            p.Fill((x, y) => Ring(x, y, cx, cy, r * 0.78f, r * 0.08f), C(0.85f, 0.55f, 0.08f, 0.8f));
            Dollar(p, cx, cy, r * 0.36f, C(0.85f, 0.55f, 0.08f));
            Gloss(p, coin, cx - r * 0.35f, cy + r * 0.4f, r * 0.28f, r * 0.16f, -35f, 0.55f);
        }

        static void DrawSparkle(Painter p, float cx, float cy, float r, Color c)
        {
            Func<float, float, float> sp = (x, y) =>
            {
                float dx = Mathf.Abs(x - cx), dy = Mathf.Abs(y - cy);
                return Mathf.Min(Ellipse(dx, dy, 0, 0, r * 0.22f, r), Ellipse(dx, dy, 0, 0, r, r * 0.22f));
            };
            p.Fill(sp, c);
        }

        static void DrawBill(Painter p, float cx, float cy, float hw, float hh, float deg)
        {
            Func<float, float, float> body = R((x, y) => RoundBox(x, y, cx, cy, hw, hh, 16), cx, cy, deg);
            Icon(p, body, C(0.36f, 0.76f, 0.4f), 8, cy - hh, cy + hh);
            p.Fill(R((x, y) => Mathf.Abs(RoundBox(x, y, cx, cy, hw - 14, hh - 14, 10)) - 2.5f, cx, cy, deg), C(0.65f, 0.92f, 0.62f));
            p.Fill((x, y) => Circle(x, y, cx, cy, hh * 0.6f), C(0.24f, 0.58f, 0.3f));
            Dollar(p, cx, cy, hh * 0.3f, C(0.78f, 1f, 0.75f));
        }

        // ---------------------------------------------------------------- particle textures

        static void FxTextures()
        {
            var p = new Painter(64);
            var star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 30 : 13;
                star[i] = new Vector2(32 + Mathf.Cos(a) * r, 31 + Mathf.Sin(a) * r);
            }
            p.Fill((x, y) => Polygon(x, y, star) - 1f, Color.white);
            p.Save(Dir + "fx_star.png", false);

            p = new Painter(64);
            Func<float, float, float> heart = (x, y) => Union(Union(Circle(x, y, 22, 40, 14), Circle(x, y, 42, 40, 14)),
                Polygon(x, y, new[] { new Vector2(9, 35), new Vector2(55, 35), new Vector2(32, 6) }));
            p.Fill(heart, C(1f, 0.35f, 0.45f));
            p.Fill((x, y) => Circle(x, y, 20, 44, 5), C(1f, 1f, 1f, 0.6f));
            p.Save(Dir + "fx_heart.png", false);

            p = new Painter(128);
            p.FillSoft((x, y) => Ring(x, y, 64, 64, 56, 6), Color.white, 4f);
            p.Save(Dir + "fx_ring.png", false);

            p = new Painter(64);
            p.FillSoft((x, y) => Circle(x, y, 32, 32, 24), Color.white, 5f);
            p.Fill((x, y) => Circle(x, y, 25, 39, 6), C(1f, 1f, 1f, 1f));
            p.Save(Dir + "fx_drop.png", false);

            p = new Painter(64);
            Func<float, float, float> leaf = R((x, y) => Ellipse(x, y, 32, 32, 26, 12), 32, 32, 35f);
            p.Fill(leaf, Color.white);
            var vein = R((x, y) => Segment(x, y, 10, 32, 54, 32, 1.2f), 32, 32, 35f);
            p.Fill((x, y) => Intersect(vein(x, y), leaf(x, y)), C(0.75f, 0.75f, 0.75f, 1f));
            p.Save(Dir + "fx_leaf.png", false);

            p = new Painter(64);
            p.FillSoft((x, y) => Circle(x, y, 32, 32, 8), C(1, 1, 1, 0.6f), 14f);
            p.Fill((x, y) =>
            {
                float dx = Mathf.Abs(x - 32), dy = Mathf.Abs(y - 32);
                return Mathf.Min(Ellipse(dx, dy, 0, 0, 4, 30), Ellipse(dx, dy, 0, 0, 30, 4));
            }, Color.white);
            p.Save(Dir + "fx_sparkle.png", false);

            p = new Painter(64);
            p.Fill((x, y) => Circle(x, y, 32, 32, 28), C(0.85f, 0.55f, 0.08f));
            p.Fill((x, y) => Circle(x, y, 32, 34, 25), C(1f, 0.82f, 0.22f));
            p.Fill((x, y) => Ring(x, y, 32, 34, 18, 3), C(0.9f, 0.62f, 0.1f));
            p.Fill((x, y) => Ellipse(x, y, 24, 44, 7, 4), C(1f, 1f, 0.9f, 0.8f));
            p.Save(Dir + "fx_coin.png", false);

            // Juice splat: blobby centre + satellite drops.
            p = new Painter(128);
            var rnd = new System.Random(5);
            Func<float, float, float> blob = (x, y) =>
            {
                float dx = x - 64, dy = y - 64;
                float a = Mathf.Atan2(dy, dx);
                float r = 34 + Mathf.Sin(a * 5f) * 5f + Mathf.Sin(a * 3f + 1f) * 4f;
                return Mathf.Sqrt(dx * dx + dy * dy) - r;
            };
            p.Fill(blob, C(1, 1, 1, 0.85f));
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float d = 44 + (float)rnd.NextDouble() * 14f;
                float r = 3 + (float)rnd.NextDouble() * 5f;
                float sx = 64 + Mathf.Cos(a) * d, sy = 64 + Mathf.Sin(a) * d;
                p.Fill((x, y) => Circle(x, y, sx, sy, r), C(1, 1, 1, 0.85f));
            }
            p.Fill((x, y) => Circle(x, y, 54, 74, 10), C(1, 1, 1, 0.25f));
            p.Save(Dir + "fx_splat.png", false);
        }

        // ---------------------------------------------------------------- World textures

        static void WorldTextures()
        {
            // Zone pads: translucent fill with a soft inner glow and a bold rim.
            var p = new Painter(256);
            p.FillFn((x, y) => RoundBox(x, y, 128, 128, 120, 120, 40), (x, y) =>
            {
                float d = -RoundBox(x, y, 128, 128, 120, 120, 40);
                return C(1, 1, 1, Mathf.Lerp(0.5f, 0.18f, Mathf.Clamp01(d / 36f)));
            });
            p.Fill((x, y) => Mathf.Abs(RoundBox(x, y, 128, 128, 112, 112, 34)) - 8f, Color.white);
            p.Save(Dir + "pad_solid.png", false);

            p = new Painter(256);
            p.Fill((x, y) => RoundBox(x, y, 128, 128, 120, 120, 40), C(1, 1, 1, 0.3f));
            p.Fill((x, y) =>
            {
                float d = Mathf.Abs(RoundBox(x, y, 128, 128, 114, 114, 36)) - 7f;
                float t = Mathf.Abs(x - 128) > Mathf.Abs(y - 128) ? y : x;
                float dash = Mathf.Repeat(t + 10f, 36f) < 22f ? -1f : 1f;
                return Mathf.Max(d, dash * 2f);
            }, Color.white);
            p.Save(Dir + "pad_dashed.png", false);

            // Station pad (atlas style): tinted fill, soft inner glow, bold white dashed rim.
            p = new Painter(256);
            p.FillFn((x, y) => RoundBox(x, y, 128, 128, 122, 122, 46), (x, y) =>
            {
                float d = -RoundBox(x, y, 128, 128, 122, 122, 46);
                return C(1, 1, 1, Mathf.Lerp(0.62f, 0.3f, Mathf.Clamp01(d / 40f)));
            });
            p.Fill((x, y) =>
            {
                float d = Mathf.Abs(RoundBox(x, y, 128, 128, 108, 108, 34)) - 7f;
                float a = Mathf.Atan2(y - 128, x - 128);
                float dash = Mathf.Repeat(a / (Mathf.PI * 2f) * 28f, 1f) < 0.62f ? -1f : 1f;
                return Mathf.Max(d, dash * 3f);
            }, Color.white);
            p.Save(Dir + "pad_dash.png", false);

            // Unlock tile: hazard-striped frame around a light inner panel.
            p = new Painter(512);
            p.FillSoft((x, y) => RoundBox(x, y, 256, 248, 246, 246, 70), C(0f, 0f, 0f, 0.35f), 14f);
            p.Fill((x, y) => RoundBox(x, y, 256, 256, 244, 244, 66), C(0.16f, 0.12f, 0.1f));
            p.FillFn((x, y) => RoundBox(x, y, 256, 256, 234, 234, 58), (x, y) =>
                Mathf.Repeat((x + y) / 64f, 1f) < 0.5f ? C(1f, 0.8f, 0.12f) : C(0.18f, 0.14f, 0.12f));
            p.Fill((x, y) => RoundBox(x, y, 256, 256, 186, 186, 34) - 6f, C(0.16f, 0.12f, 0.1f));
            p.FillFn((x, y) => RoundBox(x, y, 256, 256, 186, 186, 34), (x, y) =>
                Color.Lerp(C(1f, 0.95f, 0.82f, 0.55f), C(1f, 0.99f, 0.92f, 0.7f), y / 512f));
            p.Fill((x, y) => Intersect(RoundBox(x, y, 256, 256, 234, 234, 58) + 4f, -(RoundBox(x, y, 256, 248, 234, 234, 58) + 4f)), C(1, 1, 1, 0.35f));
            p.Save(Dir + "pad_hazard.png", false);

            // Ground chevron pointer: two bold yellow chevrons with a dark outline and a soft shadow (reads on grass and stone).
            p = new Painter(256);
            Func<float, float, float> chev1 = (x, y) => Polygon(x, y, new[] { new Vector2(128, 246), new Vector2(236, 138), new Vector2(188, 138), new Vector2(128, 198), new Vector2(68, 138), new Vector2(20, 138) });
            Func<float, float, float> chev2 = (x, y) => Polygon(x, y, new[] { new Vector2(128, 150), new Vector2(236, 42), new Vector2(188, 42), new Vector2(128, 102), new Vector2(68, 42), new Vector2(20, 42) });
            Func<float, float, float> chevs = (x, y) => Union(chev1(x, y), chev2(x, y));
            p.FillSoft((x, y) => chevs(x, y + 8f) - 10f, C(0f, 0f, 0f, 0.45f), 10f);
            p.Fill((x, y) => chevs(x, y) - 9f, C(0.25f, 0.16f, 0.05f));
            p.FillFn(chev1, (x, y) => Color.Lerp(C(1f, 0.72f, 0.05f), C(1f, 0.95f, 0.35f), Mathf.InverseLerp(138, 246, y)));
            p.FillFn(chev2, (x, y) => Color.Lerp(C(1f, 0.72f, 0.05f), C(1f, 0.95f, 0.35f), Mathf.InverseLerp(42, 150, y)) * new Color(1, 1, 1, 0.9f));
            p.Save(Dir + "pointer.png", false);

            // Station pad tile: strong tinted fill, dark rim for contrast on light stone, bright dashed inner border.
            p = new Painter(256);
            p.Fill((x, y) => RoundBox(x, y, 128, 128, 124, 124, 48), C(0.12f, 0.1f, 0.1f, 0.75f));
            p.FillFn((x, y) => RoundBox(x, y, 128, 128, 116, 116, 42), (x, y) =>
            {
                float d = -RoundBox(x, y, 128, 128, 116, 116, 42);
                // Grey fill so the tint reads darker than the white dashes on top.
                return C(0.68f, 0.68f, 0.68f, Mathf.Lerp(0.92f, 0.74f, Mathf.Clamp01(d / 30f)));
            });
            p.Fill((x, y) =>
            {
                float d = Mathf.Abs(RoundBox(x, y, 128, 128, 98, 98, 30)) - 6f;
                float a = Mathf.Atan2(y - 128, x - 128);
                float dash = Mathf.Repeat(a / (Mathf.PI * 2f) * 24f, 1f) < 0.62f ? -1f : 1f;
                return Mathf.Max(d, dash * 3f);
            }, C(1f, 1f, 1f, 1f));
            // Top sheen.
            p.Fill((x, y) => Intersect(RoundBox(x, y, 128, 128, 116, 116, 42) + 3f, -(RoundBox(x, y, 128, 120, 116, 116, 42) + 3f)), C(1, 1, 1, 0.45f));
            p.Save(Dir + "pad_tile.png", false);

            Grass();
            Cobbles();
            Soil();
            DirtPath();
            Water();

            // Road
            p = new Painter(256, 256, C(0.38f, 0.4f, 0.45f));
            p.Noise(0.06f, 9);
            p.Save(Dir + "ground_road.png", false, default, true);

            // Awning stripes with a scalloped shade line.
            p = new Painter(256, 64, Color.white);
            p.ForEach((x, y, c) => (x / 32) % 2 == 0 ? C(1f, 0.4f, 0.36f) : C(1f, 0.97f, 0.92f));
            p.Save(Dir + "awning.png", false, default, true);

            // Wood planks
            p = new Painter(256, 256, C(0.78f, 0.55f, 0.34f));
            p.ForEach((x, y, c) =>
            {
                int plank = y / 32;
                float v = ((plank * 37) % 7) / 7f * 0.08f - 0.04f;
                float grain = Mathf.Sin((x + plank * 53) * 0.08f + Mathf.Sin(y * 0.3f) * 2f) * 0.025f;
                bool gap = y % 32 < 2;
                return gap ? C(0.5f, 0.33f, 0.2f) : C(0.8f + v + grain, 0.57f + v + grain, 0.35f + v * 0.5f);
            });
            p.Save(Dir + "wood.png", false, default, true);

            // Chainsaw chain
            p = new Painter(128, 32, C(0.22f, 0.22f, 0.25f));
            for (int i = 0; i < 8; i++)
            {
                float x0 = i * 16 + 3;
                p.Fill((x, y) => RoundBox(x, y, x0 + 5, 16, 5, 12, 2), C(0.72f, 0.74f, 0.78f));
            }
            p.Save(Dir + "chain.png", false, default, true);

            // Money bill face
            p = new Painter(256, 128, C(0.36f, 0.76f, 0.4f));
            p.Fill((x, y) => Mathf.Abs(RoundBox(x, y, 128, 64, 114, 50, 10)) - 2.5f, C(0.65f, 0.92f, 0.62f));
            p.Fill((x, y) => Circle(x, y, 128, 64, 38), C(0.24f, 0.58f, 0.3f));
            Dollar(p, 128, 64, 19, C(0.78f, 1f, 0.75f));
            p.Save(Dir + "bill.png", false);

            // Soft round shadow
            p = new Painter(128);
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 63.5f) * (x - 63.5f) + (y - 63.5f) * (y - 63.5f)) / 63f;
                return new Color(0, 0, 0, Mathf.Clamp01(1f - d) * 0.55f);
            });
            p.Save(Dir + "blob_shadow.png", false);

            // Cloud shadow: several soft lobes.
            p = new Painter(256);
            p.ForEach((x, y, c) =>
            {
                float a = 0f;
                float[,] lobes = { { 100, 128, 70 }, { 150, 140, 62 }, { 190, 118, 48 }, { 64, 118, 44 }, { 130, 100, 54 } };
                for (int i = 0; i < lobes.GetLength(0); i++)
                {
                    float dx = x - lobes[i, 0], dy = y - lobes[i, 1];
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / lobes[i, 2];
                    a = Mathf.Max(a, Mathf.Clamp01(1f - d));
                }
                return new Color(0, 0, 0, Mathf.SmoothStep(0f, 1f, a) * 0.5f);
            });
            p.Save(Dir + "cloud_shadow.png", false);
        }

        static void Grass()
        {
            var rnd = new System.Random(11);
            var baseC = C(0.5f, 0.78f, 0.34f);
            var p = new Painter(512, 512, baseC);
            // Large soft light / dark patches.
            for (int i = 0; i < 70; i++)
            {
                float x0 = (float)rnd.NextDouble() * 512, y0 = (float)rnd.NextDouble() * 512, r = 40 + (float)rnd.NextDouble() * 70;
                float v = ((float)rnd.NextDouble() - 0.5f) * 0.09f;
                var col = C(0.5f + v * 0.8f, 0.78f + v, 0.34f + v * 0.3f, 0.35f);
                Wrap(512, x0, y0, r, (cx, cy) => p.FillSoft((x, y) => Circle(x, y, cx, cy, r * 0.6f), col, r * 0.8f, cx - r, cy - r, cx + r, cy + r));
            }
            // Clover-ish blotches.
            for (int i = 0; i < 220; i++)
            {
                float x0 = (float)rnd.NextDouble() * 512, y0 = (float)rnd.NextDouble() * 512, r = 6 + (float)rnd.NextDouble() * 16;
                float v = ((float)rnd.NextDouble() - 0.5f) * 0.1f;
                var col = C(0.46f + v, 0.74f + v * 1.3f, 0.3f + v * 0.5f, 0.45f);
                Wrap(512, x0, y0, r, (cx, cy) => p.Fill((x, y) => Circle(x, y, cx, cy, r), col, cx - r, cy - r, cx + r, cy + r));
            }
            // Grass blades.
            for (int i = 0; i < 1400; i++)
            {
                float x0 = 6 + (float)rnd.NextDouble() * 500, y0 = 4 + (float)rnd.NextDouble() * 496;
                float lean = ((float)rnd.NextDouble() - 0.5f) * 6f;
                bool light = rnd.NextDouble() < 0.4;
                var col = light ? C(0.62f, 0.88f, 0.42f, 0.55f) : C(0.36f, 0.62f, 0.26f, 0.5f);
                p.Fill((x, y) => Segment(x, y, x0, y0, x0 + lean, y0 + 8, 1.1f), col, x0 - 9, y0 - 2, x0 + 9, y0 + 11);
            }
            // Tiny flower specks.
            for (int i = 0; i < 90; i++)
            {
                float x0 = 5 + (float)rnd.NextDouble() * 502, y0 = 5 + (float)rnd.NextDouble() * 502;
                var col = rnd.NextDouble() < 0.6 ? C(1f, 1f, 0.95f, 0.9f) : C(1f, 0.9f, 0.35f, 0.9f);
                p.Fill((x, y) => Circle(x, y, x0, y0, 2.2f), col, x0 - 4, y0 - 4, x0 + 4, y0 + 4);
            }
            p.Save(Dir + "ground_grass.png", false, default, true);
        }

        /// <summary>Warm, slightly irregular cobblestones in offset rows. Tiles seamlessly.</summary>
        static void Cobbles()
        {
            const int S = 512, rowH = 64;
            var rnd = new System.Random(21);
            var p = new Painter(S, S, C(0.74f, 0.64f, 0.52f));
            Color[] tones = { C(0.95f, 0.88f, 0.75f), C(0.93f, 0.84f, 0.7f), C(0.97f, 0.9f, 0.8f), C(0.92f, 0.8f, 0.68f), C(0.95f, 0.86f, 0.78f) };
            for (int row = 0; row < S / rowH; row++)
            {
                float y0 = row * rowH;
                float x = (float)rnd.NextDouble() * 60f;
                float start = x;
                bool last = false;
                while (!last)
                {
                    float w = 70f + (float)rnd.NextDouble() * 50f;
                    // Final stone of the row closes the loop exactly (avoids float creep never reaching the end).
                    if (x + w > start + S - 40f)
                    {
                        w = start + S - x;
                        last = true;
                    }
                    float cx = x + w * 0.5f, cy = y0 + rowH * 0.5f + ((float)rnd.NextDouble() - 0.5f) * 4f;
                    float hw = w * 0.5f - 4f, hh = rowH * 0.5f - 4f;
                    var tone = tones[rnd.Next(tones.Length)];
                    float v = ((float)rnd.NextDouble() - 0.5f) * 0.04f;
                    tone = C(tone.r + v, tone.g + v, tone.b + v);
                    float rr = 12f + (float)rnd.NextDouble() * 8f;
                    for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        float ccx = Mathf.Repeat(cx, S) + ox * S, ccy = cy + oy * S;
                        if (ccx + hw < -2 || ccx - hw > S + 2 || ccy + hh < -2 || ccy - hh > S + 2) continue;
                        var top = Light(tone, 0.25f);
                        p.FillFn((px, py) => RoundBox(px, py, ccx, ccy, hw, hh, rr),
                            (px, py) => Color.Lerp(Dark(tone, 0.06f), top, Mathf.InverseLerp(ccy - hh, ccy + hh, py) * 0.6f),
                            ccx - hw - 2, ccy - hh - 2, ccx + hw + 2, ccy + hh + 2);
                    }
                    x += w;
                }
            }
            p.Noise(0.035f, 3);
            p.Save(Dir + "ground_tiles.png", false, default, true);
        }

        static void Soil()
        {
            var p = new Painter(256, 256, C(0.56f, 0.37f, 0.22f));
            p.ForEach((x, y, c) =>
            {
                float f = Mathf.Sin(y / 256f * Mathf.PI * 2f * 6f) * 0.5f + 0.5f;
                float v = Mathf.Lerp(-0.07f, 0.06f, f * f);
                return C(c.r + v, c.g + v * 0.8f, c.b + v * 0.6f);
            });
            var rnd = new System.Random(17);
            for (int i = 0; i < 60; i++)
            {
                float x0 = 4 + (float)rnd.NextDouble() * 248, y0 = 4 + (float)rnd.NextDouble() * 248, r = 1.5f + (float)rnd.NextDouble() * 2.5f;
                p.Fill((x, y) => Circle(x, y, x0, y0, r), C(0.68f, 0.52f, 0.36f, 0.8f), x0 - 5, y0 - 5, x0 + 5, y0 + 5);
            }
            p.Noise(0.07f, 5);
            p.Save(Dir + "ground_soil.png", false, default, true);
        }

        static void DirtPath()
        {
            var rnd = new System.Random(31);
            var p = new Painter(256, 256, C(0.86f, 0.74f, 0.55f));
            for (int i = 0; i < 40; i++)
            {
                float x0 = (float)rnd.NextDouble() * 256, y0 = (float)rnd.NextDouble() * 256, r = 14 + (float)rnd.NextDouble() * 26;
                float v = ((float)rnd.NextDouble() - 0.5f) * 0.08f;
                var col = C(0.86f + v, 0.74f + v, 0.55f + v * 0.8f, 0.5f);
                Wrap(256, x0, y0, r, (cx, cy) => p.FillSoft((x, y) => Circle(x, y, cx, cy, r * 0.5f), col, r, cx - r, cy - r, cx + r, cy + r));
            }
            for (int i = 0; i < 70; i++)
            {
                float x0 = 4 + (float)rnd.NextDouble() * 248, y0 = 4 + (float)rnd.NextDouble() * 248, r = 1.5f + (float)rnd.NextDouble() * 3f;
                var col = rnd.NextDouble() < 0.5 ? C(0.72f, 0.62f, 0.5f) : C(0.95f, 0.88f, 0.75f);
                p.Fill((x, y) => Circle(x, y, x0, y0, r), col, x0 - 5, y0 - 5, x0 + 5, y0 + 5);
            }
            p.Noise(0.03f, 8);
            p.Save(Dir + "ground_path.png", false, default, true);
        }

        static void Water()
        {
            var p = new Painter(256, 256, C(0.35f, 0.72f, 0.92f));
            p.ForEach((x, y, c) =>
            {
                float u = x / 256f, v = y / 256f;
                float w = Mathf.Sin(2f * Mathf.PI * (2f * u + 1f * v)) + Mathf.Sin(2f * Mathf.PI * (3f * u - 2f * v) + 1.3f) * 0.8f
                          + Mathf.Sin(2f * Mathf.PI * (1f * u + 4f * v) + 2.1f) * 0.6f;
                float line = Mathf.Clamp01(1f - Mathf.Abs(w) * 3.2f);
                var deep = C(0.3f, 0.66f, 0.9f);
                var shallow = C(0.46f, 0.82f, 0.96f);
                var col = Color.Lerp(deep, shallow, w * 0.2f + 0.5f);
                return Color.Lerp(col, C(0.9f, 0.98f, 1f), line * 0.65f);
            });
            p.Save(Dir + "ground_water.png", false, default, true);
        }

        /// <summary>Invoke <paramref name="draw"/> for every tile-wrapped copy of a circle that touches the texture.</summary>
        static void Wrap(int size, float x0, float y0, float r, Action<float, float> draw)
        {
            for (int ox = -1; ox <= 1; ox++)
            for (int oy = -1; oy <= 1; oy++)
            {
                float cx = x0 + ox * size, cy = y0 + oy * size;
                if (cx + r < 0 || cx - r > size || cy + r < 0 || cy - r > size) continue;
                draw(cx, cy);
            }
        }
    }
}
