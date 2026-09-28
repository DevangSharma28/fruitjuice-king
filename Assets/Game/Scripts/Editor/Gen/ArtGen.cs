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

        public static void GenerateAll()
        {
            UISprites();
            SliceTextures();
            Icons();
            WorldTextures();
        }

        // ---------------------------------------------------------------- UI

        static void UISprites()
        {
            var p = new Painter(128);
            p.Fill((x, y) => RoundBox(x, y, 64, 64, 64, 64, 44), Color.white);
            p.Save(Dir + "ui_round.png", true, new Vector4(46, 46, 46, 46));

            // Button with darker "3D" bottom edge.
            p = new Painter(128);
            p.Fill((x, y) => RoundBox(x, y, 64, 64, 64, 64, 40), C(0.72f, 0.72f, 0.72f));
            p.Fill((x, y) => RoundBox(x, y, 64, 72, 64, 56, 40), Color.white);
            p.Save(Dir + "ui_button.png", true, new Vector4(44, 48, 44, 44));

            p = new Painter(128);
            p.Fill((x, y) => Circle(x, y, 64, 64, 62), Color.white);
            p.Save(Dir + "ui_circle.png", true);

            // 1x1 world-unit white square (bars, bubble tail).
            p = new Painter(8, 8, Color.white);
            p.Save(Dir + "white.png", true, default, false, FilterMode.Bilinear, false, 8f);

            p = new Painter(256);
            p.Fill((x, y) => Circle(x, y, 128, 128, 120), C(1, 1, 1, 0.22f));
            p.Fill((x, y) => Ring(x, y, 128, 128, 116, 10), C(1, 1, 1, 0.9f));
            p.Save(Dir + "ui_ring.png", true);

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
            const float c = 128f, R = 126f;

            // Orange wheel
            var p = new Painter(256, 256, C(1f, 0.55f, 0.05f));
            p.Fill((x, y) => Circle(x, y, c, c, R), C(1f, 0.52f, 0.04f));
            p.Fill((x, y) => Circle(x, y, c, c, R - 12), C(1f, 0.93f, 0.75f));
            p.Fill((x, y) => Circle(x, y, c, c, R - 18), C(1f, 0.66f, 0.18f));
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                float ex = c + Mathf.Cos(a) * (R - 18), ey = c + Mathf.Sin(a) * (R - 18);
                p.Fill((x, y) => Segment(x, y, c, c, ex, ey, 2.2f), C(1f, 0.93f, 0.75f));
                // juicy highlight inside each segment
                float ha = a + Mathf.PI / 10f;
                float hx = c + Mathf.Cos(ha) * 70, hy = c + Mathf.Sin(ha) * 70;
                p.Fill((x, y) => Circle(x, y, hx, hy, 10), C(1f, 0.82f, 0.45f, 0.6f));
            }
            p.Fill((x, y) => Circle(x, y, c, c, 12), C(1f, 0.93f, 0.75f));
            p.Save(Dir + "slice_orange.png", false);

            // Watermelon round slice
            p = new Painter(256, 256, C(0.15f, 0.45f, 0.18f));
            p.Fill((x, y) => Circle(x, y, c, c, R), C(0.16f, 0.46f, 0.18f));
            p.Fill((x, y) => Circle(x, y, c, c, R - 8), C(0.55f, 0.82f, 0.38f));
            p.Fill((x, y) => Circle(x, y, c, c, R - 14), C(0.95f, 0.96f, 0.82f));
            p.Fill((x, y) => Circle(x, y, c, c, R - 19), C(0.96f, 0.26f, 0.32f));
            p.Fill((x, y) => Circle(x, y, c, c, R - 50), C(0.98f, 0.33f, 0.38f));
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
            p.Fill((x, y) => Circle(x, y, c, c, R), C(0.7f, 0.46f, 0.12f));
            p.Fill((x, y) => Circle(x, y, c, c, R - 10), C(1f, 0.84f, 0.28f));
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                float sx = c + Mathf.Cos(a) * 36, sy = c + Mathf.Sin(a) * 36;
                float ex = c + Mathf.Cos(a) * (R - 14), ey = c + Mathf.Sin(a) * (R - 14);
                p.Fill((x, y) => Segment(x, y, sx, sy, ex, ey, 1.2f), C(0.95f, 0.72f, 0.18f, 0.45f));
            }
            p.Fill((x, y) => Circle(x, y, c, c, 30), C(1f, 0.94f, 0.62f));
            p.Save(Dir + "slice_pineapple.png", false);
        }

        // ---------------------------------------------------------------- Icons

        static void Icons()
        {
            const float ow = 9f;

            // Orange
            var p = new Painter(256);
            p.FillOutlined((x, y) => Circle(x, y, 128, 118, 96), C(1f, 0.58f, 0.08f), Outline, ow);
            p.Fill((x, y) => Circle(x, y, 96, 150, 26), C(1f, 0.85f, 0.5f, 0.55f));
            p.FillOutlined((x, y) => Segment(x, y, 128, 205, 124, 232, 6), C(0.45f, 0.28f, 0.12f), Outline, 5);
            p.FillOutlined((x, y) =>
            {
                float dx = x - 162, dy = y - 222;
                float ca = Mathf.Cos(0.5f), sa = Mathf.Sin(0.5f);
                return Ellipse(dx * ca + dy * sa, -dx * sa + dy * ca, 0, 0, 34, 16);
            }, C(0.3f, 0.75f, 0.28f), Outline, 6);
            p.Save(Dir + "icon_orange.png", true);

            // Watermelon wedge (half disc)
            p = new Painter(256);
            Func<float, float, float, float> half = (x, y, r) => Intersect(Circle(x, y, 128, 178, r), y - 178);
            p.Fill((x, y) => half(x, y, 116) - ow, Outline);
            p.Fill((x, y) => half(x, y, 116), C(0.18f, 0.5f, 0.2f));
            p.Fill((x, y) => half(x, y, 104), C(0.55f, 0.84f, 0.38f));
            p.Fill((x, y) => half(x, y, 96), C(0.96f, 0.97f, 0.85f));
            p.Fill((x, y) => half(x, y, 88), C(0.97f, 0.27f, 0.33f));
            float[,] seeds = { { 92, 140 }, { 128, 128 }, { 164, 140 }, { 110, 105 }, { 146, 105 } };
            for (int i = 0; i < seeds.GetLength(0); i++)
            {
                float sx = seeds[i, 0], sy = seeds[i, 1];
                p.Fill((x, y) => Ellipse(x, y, sx, sy, 6, 10), C(0.12f, 0.07f, 0.07f));
            }
            p.Save(Dir + "icon_watermelon.png", true);

            // Pineapple
            p = new Painter(256);
            for (int i = -2; i <= 2; i++)
            {
                float ang = i * 0.38f;
                float len = i == 0 ? 76 : 62 - Mathf.Abs(i) * 6;
                float tx = 128 + Mathf.Sin(ang) * len, ty = 170 + Mathf.Cos(ang) * len;
                var leaf = new[] { new Vector2(128 + i * 8 - 14, 168), new Vector2(tx, ty), new Vector2(128 + i * 8 + 14, 168) };
                p.FillOutlined((x, y) => Polygon(x, y, leaf), C(0.25f, 0.68f, 0.3f), Outline, 6);
            }
            p.FillOutlined((x, y) => Ellipse(x, y, 128, 100, 70, 88), C(1f, 0.72f, 0.18f), Outline, ow);
            for (int i = -4; i <= 4; i++)
            {
                float o = i * 30;
                p.Fill((x, y) => Intersect(Segment(x, y, 60 + o, 20, 196 + o, 180, 2.5f), Ellipse(x, y, 128, 100, 66, 84)), C(0.75f, 0.45f, 0.1f, 0.8f));
                p.Fill((x, y) => Intersect(Segment(x, y, 196 + o, 20, 60 + o, 180, 2.5f), Ellipse(x, y, 128, 100, 66, 84)), C(0.75f, 0.45f, 0.1f, 0.8f));
            }
            p.Save(Dir + "icon_pineapple.png", true);

            // Juice cup
            p = new Painter(256);
            var cup = new[] { new Vector2(82, 22), new Vector2(174, 22), new Vector2(194, 186), new Vector2(62, 186) };
            p.FillOutlined((x, y) => Segment(x, y, 150, 190, 180, 248, 7), C(0.95f, 0.3f, 0.35f), Outline, 5);
            p.FillOutlined((x, y) => Polygon(x, y, cup), C(1f, 0.62f, 0.12f), Outline, ow);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(86, 30), new Vector2(110, 30), new Vector2(118, 170), new Vector2(80, 170) }), C(1f, 1f, 1f, 0.3f));
            p.FillOutlined((x, y) => RoundBox(x, y, 128, 190, 74, 14, 10), Color.white, Outline, 6);
            p.Save(Dir + "icon_juice.png", true);

            // Money bill icon
            p = new Painter(256);
            DrawBill(p, 128, 128, 112, 66, true);
            p.Save(Dir + "icon_money.png", true);

            // Chainsaw
            p = new Painter(256);
            p.FillOutlined((x, y) => RoundBox(x, y, 170, 118, 70, 22, 20), C(0.75f, 0.78f, 0.82f), Outline, ow);
            for (int i = 0; i < 6; i++)
            {
                float tx = 116 + i * 20;
                p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(tx, 140), new Vector2(tx + 14, 140), new Vector2(tx + 7, 152) }), Outline);
                p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(tx, 96), new Vector2(tx + 14, 96), new Vector2(tx + 7, 84) }), Outline);
            }
            p.FillOutlined((x, y) => RoundBox(x, y, 80, 118, 56, 46, 18), C(1f, 0.5f, 0.1f), Outline, ow);
            p.FillOutlined((x, y) => Ring(x, y, 70, 176, 26, 12), C(0.25f, 0.25f, 0.28f), Outline, 4);
            p.Save(Dir + "icon_saw.png", true);

            // Backpack
            p = new Painter(256);
            p.FillOutlined((x, y) => RoundBox(x, y, 128, 112, 74, 88, 30), C(0.62f, 0.36f, 0.2f), Outline, ow);
            p.FillOutlined((x, y) => RoundBox(x, y, 128, 168, 74, 36, 26), C(0.5f, 0.28f, 0.15f), Outline, 6);
            p.FillOutlined((x, y) => RoundBox(x, y, 128, 78, 44, 30, 12), C(0.75f, 0.47f, 0.28f), Outline, 6);
            p.Fill((x, y) => Circle(x, y, 128, 140, 8), C(1f, 0.8f, 0.2f));
            p.Save(Dir + "icon_bag.png", true);

            // Lightning (speed)
            p = new Painter(256);
            var bolt = new[] { new Vector2(150, 246), new Vector2(66, 124), new Vector2(120, 124), new Vector2(96, 10), new Vector2(190, 144), new Vector2(134, 144), new Vector2(172, 246) };
            p.FillOutlined((x, y) => Polygon(x, y, bolt), C(1f, 0.85f, 0.15f), Outline, ow);
            p.Save(Dir + "icon_speed.png", true);

            // Worker (hire)
            p = new Painter(256);
            p.FillOutlined((x, y) => RoundBox(x, y, 128, 70, 70, 58, 40), C(0.25f, 0.6f, 0.95f), Outline, ow);
            p.FillOutlined((x, y) => Circle(x, y, 128, 170, 52), C(1f, 0.8f, 0.62f), Outline, ow);
            p.FillOutlined((x, y) => Intersect(Circle(x, y, 128, 180, 56), 186 - y), C(0.95f, 0.35f, 0.3f), Outline, 6);
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
            p.FillOutlined((x, y) => Polygon(x, y, star), C(1f, 0.82f, 0.2f), Outline, ow);
            p.Save(Dir + "icon_star.png", true);

            // Heart (happy customer)
            p = new Painter(256);
            Func<float, float, float> heart = (x, y) => Union(Union(Circle(x, y, 90, 160, 56), Circle(x, y, 166, 160, 56)),
                Polygon(x, y, new[] { new Vector2(40, 140), new Vector2(216, 140), new Vector2(128, 26) }));
            p.FillOutlined(heart, C(1f, 0.3f, 0.4f), Color.white, 12);
            p.Fill((x, y) => Circle(x, y, 84, 172, 16), C(1f, 1f, 1f, 0.6f));
            p.Save(Dir + "icon_heart.png", true);

            // Sound on / off
            for (int k = 0; k < 2; k++)
            {
                p = new Painter(256);
                var spk = new[] { new Vector2(34, 96), new Vector2(84, 96), new Vector2(140, 44), new Vector2(140, 212), new Vector2(84, 160), new Vector2(34, 160) };
                p.Fill((x, y) => Polygon(x, y, spk), Color.white);
                if (k == 0)
                {
                    p.Fill((x, y) => Intersect(Ring(x, y, 132, 128, 48, 14), 150 - x + Mathf.Abs(y - 128) * 0.9f), Color.white);
                    p.Fill((x, y) => Intersect(Ring(x, y, 132, 128, 86, 14), 160 - x + Mathf.Abs(y - 128) * 0.9f), Color.white);
                }
                else
                {
                    p.Fill((x, y) => Union(Segment(x, y, 160, 90, 224, 166, 9), Segment(x, y, 160, 166, 224, 90, 9)), Color.white);
                }
                p.Save(Dir + (k == 0 ? "icon_sound_on.png" : "icon_sound_off.png"), true);
            }
        }

        static void DrawBill(Painter p, float cx, float cy, float hw, float hh, bool outline)
        {
            if (outline) p.Fill((x, y) => RoundBox(x, y, cx, cy, hw, hh, 16) - 8, Outline);
            p.Fill((x, y) => RoundBox(x, y, cx, cy, hw, hh, 16), C(0.35f, 0.75f, 0.38f));
            p.Fill((x, y) => Mathf.Abs(RoundBox(x, y, cx, cy, hw - 12, hh - 12, 10)) - 2.5f, C(0.6f, 0.9f, 0.6f));
            p.Fill((x, y) => Circle(x, y, cx, cy, hh * 0.62f), C(0.22f, 0.56f, 0.27f));
            float s = hh * 0.3f;
            Color dc = C(0.75f, 1f, 0.75f);
            // Top arc keeps everything except the lower-right quadrant, bottom arc everything except upper-left: an "S".
            p.Fill((x, y) => Intersect(Ring(x, y, cx, cy + s * 0.55f, s * 0.55f, s * 0.32f), -Intersect(cx - x, y - (cy + s * 0.55f))), dc);
            p.Fill((x, y) => Intersect(Ring(x, y, cx, cy - s * 0.55f, s * 0.55f, s * 0.32f), -Intersect(x - cx, (cy - s * 0.55f) - y)), dc);
            p.Fill((x, y) => Segment(x, y, cx, cy - s * 1.45f, cx, cy + s * 1.45f, s * 0.13f), dc);
        }

        // ---------------------------------------------------------------- World textures

        static void WorldTextures()
        {
            // Zone pads
            var p = new Painter(256);
            p.Fill((x, y) => RoundBox(x, y, 128, 128, 120, 120, 40), C(1, 1, 1, 0.22f));
            p.Fill((x, y) => Mathf.Abs(RoundBox(x, y, 128, 128, 114, 114, 36)) - 7f, Color.white);
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

            // Ground chevron pointer
            p = new Painter(128);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(64, 124), new Vector2(122, 66), new Vector2(96, 66), new Vector2(64, 98), new Vector2(32, 66), new Vector2(6, 66) }), Color.white);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(64, 76), new Vector2(122, 18), new Vector2(96, 18), new Vector2(64, 50), new Vector2(32, 18), new Vector2(6, 18) }), C(1, 1, 1, 0.7f));
            p.Save(Dir + "pointer.png", false);

            // Grass
            var rnd = new System.Random(11);
            p = new Painter(512, 512, C(0.46f, 0.75f, 0.33f));
            for (int i = 0; i < 260; i++)
            {
                float x0 = (float)rnd.NextDouble() * 512, y0 = (float)rnd.NextDouble() * 512, r = 10 + (float)rnd.NextDouble() * 36;
                float v = ((float)rnd.NextDouble() - 0.5f) * 0.08f;
                var col = C(0.46f + v, 0.75f + v * 1.3f, 0.33f + v * 0.5f, 0.5f);
                for (int ox = -1; ox <= 1; ox++)
                for (int oy = -1; oy <= 1; oy++)
                {
                    float cx = x0 + ox * 512, cy = y0 + oy * 512;
                    if (cx + r < 0 || cx - r > 512 || cy + r < 0 || cy - r > 512) continue;
                    p.Fill((x, y) => Circle(x, y, cx, cy, r), col, cx - r, cy - r, cx + r, cy + r);
                }
            }
            for (int i = 0; i < 700; i++)
            {
                float x0 = 4 + (float)rnd.NextDouble() * 504, y0 = 4 + (float)rnd.NextDouble() * 500;
                float lean = ((float)rnd.NextDouble() - 0.5f) * 5f;
                p.Fill((x, y) => Segment(x, y, x0, y0, x0 + lean, y0 + 7, 1.1f), C(0.36f, 0.63f, 0.26f, 0.5f), x0 - 8, y0 - 2, x0 + 8, y0 + 10);
            }
            p.Save(Dir + "ground_grass.png", false, default, true);

            // Plaza tiles
            p = new Painter(256, 256, C(0.93f, 0.87f, 0.74f));
            p.ForEach((x, y, c) =>
            {
                bool alt = ((x / 128) + (y / 128)) % 2 == 0;
                Color baseC = alt ? C(0.95f, 0.89f, 0.76f) : C(0.91f, 0.84f, 0.7f);
                int mx = x % 128, my = y % 128;
                bool grout = mx < 3 || my < 3 || mx > 124 || my > 124;
                return grout ? C(0.8f, 0.72f, 0.58f) : baseC;
            });
            p.Noise(0.04f, 3);
            p.Save(Dir + "ground_tiles.png", false, default, true);

            // Soil
            p = new Painter(256, 256, C(0.56f, 0.37f, 0.22f));
            p.ForEach((x, y, c) =>
            {
                float f = Mathf.Sin(y / 256f * Mathf.PI * 2f * 6f) * 0.5f + 0.5f;
                float v = Mathf.Lerp(-0.06f, 0.05f, f);
                return C(c.r + v, c.g + v * 0.8f, c.b + v * 0.6f);
            });
            p.Noise(0.08f, 5);
            p.Save(Dir + "ground_soil.png", false, default, true);

            // Road
            p = new Painter(256, 256, C(0.36f, 0.38f, 0.43f));
            p.Noise(0.06f, 9);
            p.Save(Dir + "ground_road.png", false, default, true);

            // Awning stripes
            p = new Painter(256, 64, Color.white);
            p.ForEach((x, y, c) => (x / 32) % 2 == 0 ? C(1f, 0.38f, 0.32f) : C(1f, 0.97f, 0.92f));
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
            p = new Painter(256, 128, C(0.35f, 0.75f, 0.38f));
            DrawBill(p, 128, 64, 126, 62, false);
            p.Save(Dir + "bill.png", false);

            // Grass-edge soft shadow for fields / pads (radial dark)
            p = new Painter(128);
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 63.5f) * (x - 63.5f) + (y - 63.5f) * (y - 63.5f)) / 63f;
                return new Color(0, 0, 0, Mathf.Clamp01(1f - d) * 0.55f);
            });
            p.Save(Dir + "blob_shadow.png", false);
        }
    }
}
