using System;
using UnityEngine;
using static JuiceKing.EditorTools.Painter;

namespace JuiceKing.EditorTools
{
    /// <summary>Expansion 1 art: tropical fruit and juice icons, fruit piece textures, delivery / upgrade icons, beach ground.</summary>
    public static partial class ArtGen
    {
        /// <summary>Juice icon file per FruitKind.</summary>
        public static string JuiceIconFile(int kind) => "icon_juice_" + kind + ".png";

        public static readonly string[] TropicalFruitIcons = { "icon_coconut.png", "icon_mango.png", "icon_banana.png", "icon_papaya.png" };

        static void TropicalArt()
        {
            TropicalFruitIconArt();
            JuiceIcons();
            TropicalSlices();
            DeliveryIcons();
            BeachTextures();
            ShaderTextures();
        }

        // ---------------------------------------------------------------- shader textures

        /// <summary>Tileable value noise in [0,1] (fBm), sampled with wrap-around.</summary>
        static float TileNoise(float x, float y, int period, int seed)
        {
            float Hash(int ix, int iy)
            {
                ix = ((ix % period) + period) % period;
                iy = ((iy % period) + period) % period;
                uint h = (uint)(ix * 374761393 + iy * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0xffff) / 65535f;
            }
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(x0, y0), Hash(x0 + 1, y0), fx);
            float b = Mathf.Lerp(Hash(x0, y0 + 1), Hash(x0 + 1, y0 + 1), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>Tileable value noise with separate periods per axis (long streaks: many cells across, few down).</summary>
        static float TileNoise2(float x, float y, int px, int py, int seed)
        {
            float Hash(int ix, int iy)
            {
                ix = ((ix % px) + px) % px;
                iy = ((iy % py) + py) % py;
                uint h = (uint)(ix * 374761393 + iy * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0xffff) / 65535f;
            }
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(x0, y0), Hash(x0 + 1, y0), fx);
            float b = Mathf.Lerp(Hash(x0, y0 + 1), Hash(x0 + 1, y0 + 1), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static void ShaderTextures()
        {
            const int S = 256;
            // Soft painterly noise for the stylized shader's detail (mid-grey +/- variation).
            var p = new Painter(S, S, Color.gray);
            p.ForEach((x, y, c) =>
            {
                float u = x / (float)S, v = y / (float)S;
                float n = TileNoise(u * 4f, v * 4f, 4, 11) * 0.55f + TileNoise(u * 8f, v * 8f, 8, 12) * 0.3f + TileNoise(u * 16f, v * 16f, 16, 13) * 0.15f;
                return new Color(n, n, n, 1f);
            });
            p.Save(Dir + "noise_soft.png", false, default, true);

            // Caustics: R = web of bright cell edges (tileable Voronoi), G = blotchy noise used for sun sparkles.
            const int cells = 6;
            var pts = new Vector2[cells, cells];
            var rnd = new System.Random(91);
            for (int i = 0; i < cells; i++)
            for (int j = 0; j < cells; j++)
                pts[i, j] = new Vector2((i + 0.15f + (float)rnd.NextDouble() * 0.7f) / cells, (j + 0.15f + (float)rnd.NextDouble() * 0.7f) / cells);
            p = new Painter(S, S, Color.black);
            p.ForEach((x, y, c) =>
            {
                float u = x / (float)S, v = y / (float)S;
                int ci = Mathf.FloorToInt(u * cells), cj = Mathf.FloorToInt(v * cells);
                float f1 = 9f, f2 = 9f;
                for (int di = -1; di <= 1; di++)
                for (int dj = -1; dj <= 1; dj++)
                {
                    int ii = ci + di, jj = cj + dj;
                    int wi = ((ii % cells) + cells) % cells, wj = ((jj % cells) + cells) % cells;
                    var q = pts[wi, wj] + new Vector2(Mathf.Floor((float)ii / cells), Mathf.Floor((float)jj / cells));
                    float d = (new Vector2(u, v) - q).magnitude;
                    if (d < f1) { f2 = f1; f1 = d; }
                    else if (d < f2) f2 = d;
                }
                float edge = Mathf.Clamp01(1f - (f2 - f1) * cells * 3.2f);
                edge = edge * edge;
                float g = TileNoise(u * 10f, v * 10f, 10, 21) * 0.7f + TileNoise(u * 20f, v * 20f, 20, 22) * 0.3f;
                return new Color(edge, g, 0f, 1f);
            });
            p.Save(Dir + "caustics.png", false, default, true);

            // Contact shadow: white (the material tints it) with a long soft falloff, darkest at the centre.
            p = new Painter(128, 128, new Color(1f, 1f, 1f, 0f));
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 63.5f) * (x - 63.5f) + (y - 63.5f) * (y - 63.5f)) / 63f;
                return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f));
            });
            p.Save(Dir + "contact_shadow.png", false);

            // Waterfall: bright strands streaming down sea-green water with foamy edges. Tiles top to bottom (it scrolls).
            p = new Painter(64, 256, Color.white);
            var deep = new Color(0.38f, 0.78f, 0.9f);
            var foam = new Color(0.92f, 0.99f, 1f);
            p.ForEach((x, y, c) =>
            {
                float u = x / 64f, v = y / 256f;
                float n = TileNoise2(u * 14f, v * 2f, 14, 2, 31) * 0.6f + TileNoise2(u * 28f, v * 5f, 28, 5, 32) * 0.4f;
                float strand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.8f, n));
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1f, Mathf.Abs(u - 0.5f) * 2f));
                var col = Color.Lerp(deep, foam, Mathf.Clamp01(strand * 0.85f + edge * 0.7f));
                return new Color(col.r, col.g, col.b, 1f);
            });
            p.Save(Dir + "waterfall.png", false, default, true);
        }

        // ---------------------------------------------------------------- fruit icons

        static void TropicalFruitIconArt()
        {
            // Coconut: hairy brown ball with three eyes and a leaf.
            var p = new Painter(256);
            Func<float, float, float> nut = (x, y) => Circle(x, y, 128, 112, 90);
            Icon(p, nut, C(0.55f, 0.34f, 0.18f));
            var rnd = new System.Random(41);
            for (int i = 0; i < 70; i++)
            {
                float px = 50 + (float)rnd.NextDouble() * 156, py = 30 + (float)rnd.NextDouble() * 160;
                float a = (float)rnd.NextDouble() * Mathf.PI;
                float ex = px + Mathf.Cos(a) * 14, ey = py + Mathf.Sin(a) * 14;
                p.Fill((x, y) => Intersect(Segment(x, y, px, py, ex, ey, 1.6f), nut(x, y) + 8), C(0.35f, 0.2f, 0.1f, 0.45f));
            }
            foreach (var e in new[] { new Vector2(106, 150), new Vector2(150, 150), new Vector2(128, 124) })
            {
                var ee = e;
                p.Fill((x, y) => Circle(x, y, ee.x, ee.y, 11), C(0.25f, 0.14f, 0.07f));
            }
            Gloss(p, nut, 92, 150, 28, 16, -25f, 0.35f);
            Icon(p, R((x, y) => Ellipse(x, y, 176, 214, 40, 14), 176, 214, 24f), C(0.3f, 0.75f, 0.3f), 6, 200, 236, false);
            p.Save(Dir + "icon_coconut.png", true);

            // Mango: tilted blush teardrop with a stem and a leaf.
            p = new Painter(256);
            Func<float, float, float> mango = R((x, y) => Union(Ellipse(x, y, 128, 110, 80, 96), Circle(x, y, 150, 150, 62)), 128, 118, -28f);
            p.FillSoft((x, y) => mango(x, y + 9f) - 9, C(0f, 0f, 0f, 0.28f), 8f);
            p.Fill((x, y) => mango(x, y) - 9, Outline);
            p.FillFn(mango, (x, y) =>
            {
                float t = Mathf.InverseLerp(40, 220, x * 0.45f + y * 0.75f);
                Color a = C(1f, 0.78f, 0.15f), b = C(1f, 0.42f, 0.2f), g = C(0.55f, 0.78f, 0.25f);
                return t < 0.5f ? Color.Lerp(g, a, t * 2f) : Color.Lerp(a, b, (t - 0.5f) * 2f);
            });
            p.Fill((x, y) => Intersect(mango(x, y) + 3f, -(mango(x, y + 7f) + 3f)), C(1f, 1f, 1f, 0.35f));
            Gloss(p, mango, 110, 160, 30, 18, -40f, 0.45f);
            Icon(p, (x, y) => Segment(x, y, 176, 206, 186, 230, 6), C(0.45f, 0.28f, 0.12f), 5, 200, 232, false, false);
            Icon(p, R((x, y) => Ellipse(x, y, 142, 222, 34, 13), 142, 222, -18f), C(0.3f, 0.72f, 0.3f), 6, 208, 236, false);
            p.Save(Dir + "icon_mango.png", true);

            // Banana: a fat yellow crescent with brown tips.
            p = new Painter(256);
            Func<float, float, float> banana = (x, y) => Subtract(Circle(x, y, 128, 196, 150), Circle(x, y, 150, 238, 150));
            Func<float, float, float> bananaC = (x, y) => Intersect(banana(x, y), Intersect(x - 20, 236 - x));
            Icon(p, (x, y) => bananaC(x, y) - 2f, C(1f, 0.86f, 0.2f), 9, 40, 150);
            p.Fill((x, y) => Intersect(Circle(x, y, 26, 128, 16), bananaC(x, y) + 2f), C(0.42f, 0.28f, 0.1f));
            p.Fill((x, y) => Intersect(Circle(x, y, 232, 128, 12), bananaC(x, y) + 2f), C(0.42f, 0.28f, 0.1f));
            p.Fill((x, y) => Intersect(Ring(x, y, 128, 196, 118, 5f), bananaC(x, y) + 6), C(0.85f, 0.65f, 0.1f, 0.6f));
            Gloss(p, bananaC, 110, 70, 50, 10, 8f, 0.5f);
            p.Save(Dir + "icon_banana.png", true);

            // Papaya: halved, orange flesh and a cluster of black seeds.
            p = new Painter(256);
            Func<float, float, float> papaya = R((x, y) => Union(Ellipse(x, y, 128, 100, 82, 76), Ellipse(x, y, 128, 170, 58, 64)), 128, 128, -18f);
            Icon(p, papaya, C(0.55f, 0.75f, 0.25f));
            Func<float, float, float> flesh = (x, y) => papaya(x, y) + 12f;
            p.FillFn(flesh, (x, y) => Color.Lerp(C(1f, 0.45f, 0.2f), C(1f, 0.62f, 0.28f), Mathf.InverseLerp(40, 220, y)));
            Func<float, float, float> cavity = R((x, y) => Ellipse(x, y, 128, 118, 34, 56), 128, 128, -18f);
            p.Fill(cavity, C(1f, 0.78f, 0.45f));
            var srnd = new System.Random(12);
            for (int i = 0; i < 22; i++)
            {
                float sx = 128 + ((float)srnd.NextDouble() - 0.5f) * 44f, sy = 118 + ((float)srnd.NextDouble() - 0.5f) * 90f;
                var rs = R((x, y) => Circle(x, y, sx, sy, 7f), 128, 128, -18f);
                if (cavity(128 + (sx - 128), sy) > -4f) continue;
                p.Fill(rs, C(0.1f, 0.07f, 0.07f));
                p.Fill(R((x, y) => Circle(x, y, sx - 2, sy + 2, 2f), 128, 128, -18f), C(1f, 1f, 1f, 0.5f));
            }
            Gloss(p, papaya, 96, 160, 22, 14, -40f, 0.3f);
            p.Save(Dir + "icon_papaya.png", true);
        }

        // ---------------------------------------------------------------- juice icons

        static void JuiceIcons()
        {
            for (int k = 0; k < ItemTypes.FruitCount; k++)
            {
                var p = new Painter(256);
                Color jc = Balance.JuiceColors[k];
                jc.a = 1f;
                switch ((FruitKind)k)
                {
                    case FruitKind.Coconut: CoconutShellCup(p, jc); break;
                    case FruitKind.Banana: ShakeGlass(p, jc); break;
                    case FruitKind.Mango: TallGlass(p, jc, C(1f, 0.6f, 0.15f)); break;
                    case FruitKind.Papaya: TallGlass(p, jc, C(0.55f, 0.78f, 0.28f)); break;
                    case FruitKind.Strawberry: SmoothieGlass(p, jc); break;
                    case FruitKind.Raspberry: MasonJar(p, jc); break;
                    case FruitKind.Blueberry: MilkBottle(p, jc); break;
                    case FruitKind.Cranberry: CoolerGlass(p, jc); break;
                    default: TakeawayCup(p, jc, Balance.FruitColors[k]); break;
                }
                p.Save(Dir + JuiceIconFile(k), true);
            }
        }

        static void Straw(Painter p, float x0, float y0, float x1, float y1, Color c)
        {
            Icon(p, (x, y) => Segment(x, y, x0, y0, x1, y1, 7), c, 5, Mathf.Min(y0, y1), Mathf.Max(y0, y1), false);
            p.Fill((x, y) => Segment(x, y, x0 + 2, y0, x1 + 2, y1, 2f), C(1f, 1f, 1f, 0.45f));
        }

        /// <summary>Classic lidded take-away cup with a fruit wheel (the original three juices).</summary>
        static void TakeawayCup(Painter p, Color juice, Color garnish)
        {
            var cup = new[] { new Vector2(82, 22), new Vector2(174, 22), new Vector2(194, 180), new Vector2(62, 180) };
            Straw(p, 150, 186, 184, 246, C(0.95f, 0.32f, 0.38f));
            Func<float, float, float> cupS = (x, y) => Polygon(x, y, cup);
            Icon(p, cupS, juice, 9, 22, 180);
            p.Fill((x, y) => Intersect(RoundBox(x, y, 128, 84, 80, 16, 4), cupS(x, y)), C(1f, 1f, 1f, 0.9f));
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(88, 32), new Vector2(106, 32), new Vector2(116, 168), new Vector2(84, 168) }), C(1f, 1f, 1f, 0.28f));
            Icon(p, (x, y) => RoundBox(x, y, 128, 186, 76, 13, 10), Color.white, 6, 172, 200, false);
            Icon(p, (x, y) => Circle(x, y, 190, 176, 30), garnish, 6, 146, 206, false);
            p.Fill((x, y) => Circle(x, y, 190, 176, 22), Light(garnish, 0.45f));
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                float ex = 190 + Mathf.Cos(a) * 22, ey = 176 + Mathf.Sin(a) * 22;
                p.Fill((x, y) => Segment(x, y, 190, 176, ex, ey, 1.8f), C(1f, 0.97f, 0.85f));
            }
        }

        /// <summary>Half coconut shell with milky juice, a straw and a paper umbrella.</summary>
        static void CoconutShellCup(Painter p, Color juice)
        {
            // Umbrella behind.
            Icon(p, (x, y) => Segment(x, y, 150, 140, 188, 226, 3.5f), C(0.9f, 0.85f, 0.75f), 3, 140, 226, false, false);
            Func<float, float, float> umb = (x, y) => Intersect(Circle(x, y, 190, 210, 50), Subtract(210 - y + (x - 190) * 0.2f, Circle(x, y, 190, 150, 30)));
            Icon(p, (x, y) => Intersect(Circle(x, y, 196, 208, 52), (y - 200) - (x - 196) * 0.25f), C(1f, 0.35f, 0.45f), 6, 200, 250, false);
            for (int i = 0; i < 3; i++)
            {
                float a = 0.4f + i * 0.55f;
                p.Fill((x, y) => Intersect(Segment(x, y, 196, 204, 196 + Mathf.Cos(a) * 60, 204 + Mathf.Sin(a) * 60, 2f), Circle(x, y, 196, 208, 50)), C(1f, 0.85f, 0.4f, 0.8f));
            }
            _ = umb;
            Straw(p, 104, 140, 80, 236, C(0.3f, 0.75f, 0.95f));
            Func<float, float, float> shell = (x, y) => Intersect(Circle(x, y, 128, 130, 100), y - 130);
            Icon(p, shell, C(0.55f, 0.33f, 0.17f), 9, 30, 130);
            var rnd = new System.Random(9);
            for (int i = 0; i < 40; i++)
            {
                float px = 44 + (float)rnd.NextDouble() * 168, py = 40 + (float)rnd.NextDouble() * 84;
                float a = (float)rnd.NextDouble() * Mathf.PI;
                p.Fill((x, y) => Intersect(Segment(x, y, px, py, px + Mathf.Cos(a) * 12, py + Mathf.Sin(a) * 12, 1.5f), shell(x, y) + 8), C(0.35f, 0.2f, 0.1f, 0.45f));
            }
            // Rim of white flesh and the juice surface.
            Icon(p, (x, y) => Ellipse(x, y, 128, 132, 100, 22), C(0.98f, 0.96f, 0.9f), 6, 110, 154, false);
            p.Fill((x, y) => Ellipse(x, y, 128, 134, 84, 15), Color.Lerp(juice, Color.white, 0.2f));
            p.Fill((x, y) => Ellipse(x, y, 104, 138, 26, 5), C(1f, 1f, 1f, 0.6f));
        }

        /// <summary>Milkshake glass with whipped cream and a banana wheel.</summary>
        static void ShakeGlass(Painter p, Color juice)
        {
            Straw(p, 146, 176, 176, 250, C(0.95f, 0.35f, 0.4f));
            Func<float, float, float> glass = (x, y) => Union(Polygon(x, y, new[] { new Vector2(92, 60), new Vector2(164, 60), new Vector2(186, 184), new Vector2(70, 184) }),
                RoundBox(x, y, 128, 30, 40, 14, 8));
            Icon(p, (x, y) => Segment(x, y, 128, 30, 128, 64, 10), C(0.85f, 0.92f, 1f), 6, 20, 64, false);
            Icon(p, (x, y) => RoundBox(x, y, 128, 22, 50, 10, 8), C(0.85f, 0.92f, 1f), 6, 12, 32, false);
            Func<float, float, float> body = (x, y) => Polygon(x, y, new[] { new Vector2(92, 64), new Vector2(164, 64), new Vector2(186, 184), new Vector2(70, 184) });
            Icon(p, body, juice, 9, 64, 184);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(98, 72), new Vector2(112, 72), new Vector2(104, 176), new Vector2(84, 176) }), C(1f, 1f, 1f, 0.35f));
            // Whipped cream swirl.
            Func<float, float, float> cream = (x, y) => Union(Union(Circle(x, y, 100, 190, 30), Circle(x, y, 156, 190, 30)), Union(Circle(x, y, 128, 206, 32), Circle(x, y, 128, 236, 14)));
            Icon(p, cream, C(1f, 0.99f, 0.95f), 7, 160, 250);
            Icon(p, (x, y) => Circle(x, y, 60, 180, 24), C(1f, 0.9f, 0.45f), 6, 156, 204, false);
            p.Fill((x, y) => Circle(x, y, 60, 180, 16), C(1f, 0.97f, 0.8f));
            p.Fill((x, y) => Circle(x, y, 60, 180, 4), C(0.6f, 0.45f, 0.2f, 0.8f));
            _ = glass;
        }

        /// <summary>Tall tumbler with a fruit wedge on the rim.</summary>
        static void TallGlass(Painter p, Color juice, Color wedge)
        {
            Straw(p, 108, 176, 86, 246, C(1f, 0.85f, 0.2f));
            var outer = new[] { new Vector2(80, 20), new Vector2(176, 20), new Vector2(190, 200), new Vector2(66, 200) };
            Icon(p, (x, y) => Polygon(x, y, outer), C(0.86f, 0.94f, 1f), 9, 20, 200);
            Func<float, float, float> liquid = (x, y) => Polygon(x, y, new[] { new Vector2(88, 30), new Vector2(168, 30), new Vector2(180, 176), new Vector2(76, 176) });
            p.FillFn(liquid, (x, y) => Color.Lerp(Dark(juice, 0.12f), Light(juice, 0.2f), Mathf.InverseLerp(30, 176, y)));
            p.Fill((x, y) => Intersect(Ellipse(x, y, 128, 176, 52, 7), liquid(x, y) - 2f), Light(juice, 0.45f));
            for (int i = 0; i < 3; i++)
            {
                float bx = 110 + i * 22, by = 60 + i * 34;
                p.Fill((x, y) => Circle(x, y, bx, by, 5), C(1f, 1f, 1f, 0.35f));
            }
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(88, 34), new Vector2(104, 34), new Vector2(100, 190), new Vector2(80, 190) }), C(1f, 1f, 1f, 0.35f));
            Func<float, float, float> w = (x, y) => Intersect(Circle(x, y, 186, 200, 38), 200 - y + 4f);
            Icon(p, (x, y) => Intersect(Circle(x, y, 186, 206, 40), y - 206), wedge, 6, 206, 246, false);
            p.Fill((x, y) => Intersect(Circle(x, y, 186, 206, 30), y - 210), Light(wedge, 0.35f));
            _ = w;
        }

        // ---------------------------------------------------------------- fruit piece textures (top faces of slices)

        static void TropicalSlices()
        {
            const float c = 128f, R0 = 126f;

            // Coconut chunk: white flesh, thin cream layer, brown shell rim.
            var p = new Painter(256, 256, C(0.5f, 0.3f, 0.15f));
            p.Fill((x, y) => Circle(x, y, c, c, R0), C(0.48f, 0.3f, 0.16f));
            p.Fill((x, y) => Circle(x, y, c, c, R0 - 14), C(0.95f, 0.92f, 0.84f));
            p.FillFn((x, y) => Circle(x, y, c, c, R0 - 20), (x, y) =>
                Color.Lerp(C(1f, 1f, 0.97f), C(0.93f, 0.92f, 0.88f), Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / R0));
            p.Noise(0.03f, 4);
            p.Save(Dir + "slice_coconut.png", false);

            // Mango cheek, scored into a grid.
            p = new Painter(256, 256, C(0.9f, 0.55f, 0.1f));
            p.Fill((x, y) => Circle(x, y, c, c, R0), C(0.92f, 0.5f, 0.12f));
            p.FillFn((x, y) => Circle(x, y, c, c, R0 - 10), (x, y) =>
                Color.Lerp(C(1f, 0.8f, 0.25f), C(1f, 0.62f, 0.12f), Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / R0));
            for (int i = -4; i <= 4; i++)
            {
                float o = i * 28f;
                p.Fill((x, y) => Intersect(Segment(x, y, c + o - 130, c - 130, c + o + 130, c + 130, 2f), Circle(x, y, c, c, R0 - 14)), C(0.9f, 0.52f, 0.08f, 0.8f));
                p.Fill((x, y) => Intersect(Segment(x, y, c + o + 130, c - 130, c + o - 130, c + 130, 2f), Circle(x, y, c, c, R0 - 14)), C(0.9f, 0.52f, 0.08f, 0.8f));
            }
            p.Save(Dir + "slice_mango.png", false);

            // Banana coin: cream with a pale rim and a darker star in the middle.
            p = new Painter(256, 256, C(0.98f, 0.9f, 0.5f));
            p.Fill((x, y) => Circle(x, y, c, c, R0), C(0.98f, 0.88f, 0.45f));
            p.Fill((x, y) => Circle(x, y, c, c, R0 - 10), C(1f, 0.97f, 0.82f));
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * Mathf.PI * 2f;
                p.Fill((x, y) => Segment(x, y, c, c, c + Mathf.Cos(a) * 26, c + Mathf.Sin(a) * 26, 6f), C(0.86f, 0.72f, 0.42f, 0.7f));
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                float sx = c + Mathf.Cos(a) * 44, sy = c + Mathf.Sin(a) * 44;
                p.Fill((x, y) => Circle(x, y, sx, sy, 3.5f), C(0.55f, 0.42f, 0.25f, 0.8f));
            }
            p.Noise(0.025f, 6);
            p.Save(Dir + "slice_banana.png", false);

            // Papaya ring: green skin, orange flesh, seed-filled centre.
            p = new Painter(256, 256, C(0.45f, 0.62f, 0.2f));
            p.Fill((x, y) => Circle(x, y, c, c, R0), C(0.48f, 0.66f, 0.22f));
            p.Fill((x, y) => Circle(x, y, c, c, R0 - 9), C(1f, 0.72f, 0.3f));
            p.FillFn((x, y) => Circle(x, y, c, c, R0 - 16), (x, y) =>
                Color.Lerp(C(1f, 0.4f, 0.18f), C(1f, 0.55f, 0.24f), Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / R0));
            p.Fill((x, y) => Circle(x, y, c, c, 50), C(1f, 0.8f, 0.5f));
            var rnd = new System.Random(5);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, rr = (float)rnd.NextDouble() * 38f;
                float sx = c + Mathf.Cos(a) * rr, sy = c + Mathf.Sin(a) * rr;
                p.Fill((x, y) => Circle(x, y, sx, sy, 8f), C(0.1f, 0.07f, 0.07f));
                p.Fill((x, y) => Circle(x, y, sx - 2, sy + 2, 2.4f), C(1f, 1f, 1f, 0.45f));
            }
            p.Save(Dir + "slice_papaya.png", false);
        }

        // ---------------------------------------------------------------- delivery & upgrade icons

        static void DeliveryIcons()
        {
            // Delivery truck.
            var p = new Painter(256);
            Icon(p, (x, y) => RoundBox(x, y, 104, 128, 84, 58, 16), C(1f, 0.55f, 0.2f), 9, 70, 186);
            Icon(p, (x, y) => Union(RoundBox(x, y, 204, 108, 36, 38, 14), RoundBox(x, y, 196, 86, 44, 16, 8)), C(0.3f, 0.65f, 1f), 9, 70, 146);
            p.Fill((x, y) => RoundBox(x, y, 212, 124, 20, 16, 6), C(0.85f, 0.95f, 1f));
            p.Fill((x, y) => RoundBox(x, y, 104, 128, 60, 10, 5), C(1f, 1f, 1f, 0.85f));
            foreach (var wx in new[] { 64f, 190f })
            {
                var cx = wx;
                Icon(p, (x, y) => Circle(x, y, cx, 66, 26), C(0.22f, 0.22f, 0.26f), 7, 40, 92, false);
                p.Fill((x, y) => Circle(x, y, cx, 66, 10), C(0.8f, 0.82f, 0.86f));
            }
            p.Save(Dir + "icon_truck.png", true);

            // Parcel box (delivery button): cardboard cube seen from the front-top, tape across, shipping label.
            p = new Painter(256);
            var front = new[] { new Vector2(44, 28), new Vector2(170, 28), new Vector2(170, 150), new Vector2(44, 150) };
            var side = new[] { new Vector2(170, 28), new Vector2(222, 66), new Vector2(222, 186), new Vector2(170, 150) };
            var top = new[] { new Vector2(44, 150), new Vector2(170, 150), new Vector2(222, 186), new Vector2(96, 186) };
            Func<float, float, float> box = (x, y) => Union(Polygon(x, y, front), Union(Polygon(x, y, side), Polygon(x, y, top)));
            p.FillSoft((x, y) => box(x, y + 9f) - 9f, C(0f, 0f, 0f, 0.28f), 8f);
            p.Fill((x, y) => box(x, y) - 9f, Outline);
            p.FillFn((x, y) => Polygon(x, y, front), (x, y) => Color.Lerp(C(0.8f, 0.55f, 0.3f), C(0.9f, 0.66f, 0.38f), y / 150f));
            p.Fill((x, y) => Polygon(x, y, side), C(0.7f, 0.46f, 0.24f));
            p.Fill((x, y) => Polygon(x, y, top), C(0.96f, 0.75f, 0.47f));
            // Tape over the top and down the front.
            p.Fill((x, y) => Intersect(Polygon(x, y, top), Mathf.Abs((x - 133) - (y - 168) * 1.45f) - 13f), C(0.98f, 0.9f, 0.7f));
            p.Fill((x, y) => Intersect(Polygon(x, y, front), Mathf.Abs(x - 107) - 13f), C(0.98f, 0.9f, 0.7f));
            // Shipping label with lines.
            p.Fill((x, y) => RoundBox(x, y, 75, 70, 22, 16, 4), Color.white);
            p.Fill((x, y) => Segment(x, y, 62, 76, 88, 76, 2.2f), C(0.35f, 0.35f, 0.4f));
            p.Fill((x, y) => Segment(x, y, 62, 66, 82, 66, 2.2f), C(0.35f, 0.35f, 0.4f));
            p.Fill((x, y) => Intersect(Polygon(x, y, top), -(Polygon(x, y, top) + 5f)), C(1f, 1f, 1f, 0.3f));
            p.Save(Dir + "icon_parcel.png", true);

            // Vertical shade (transparent at the top, dark at the bottom) behind the loading bar.
            p = new Painter(8, 256, new Color(0f, 0f, 0f, 0f));
            p.ForEach((x, y, c) => new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, 1f - y / 255f)));
            p.Save(Dir + "grad_vertical.png", true);

            // Clock (truck frequency).
            p = new Painter(256);
            Func<float, float, float> face = (x, y) => Circle(x, y, 128, 120, 96);
            Icon(p, face, C(0.35f, 0.75f, 1f));
            p.Fill((x, y) => Circle(x, y, 128, 120, 74), C(1f, 1f, 1f));
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                float r0 = i % 3 == 0 ? 56 : 62;
                p.Fill((x, y) => Segment(x, y, 128 + Mathf.Cos(a) * r0, 120 + Mathf.Sin(a) * r0, 128 + Mathf.Cos(a) * 68, 120 + Mathf.Sin(a) * 68, 3.5f), C(0.3f, 0.3f, 0.36f));
            }
            p.Fill((x, y) => Segment(x, y, 128, 120, 128, 172, 7f), Outline);
            p.Fill((x, y) => Segment(x, y, 128, 120, 166, 104, 7f), Outline);
            p.Fill((x, y) => Circle(x, y, 128, 120, 10), C(1f, 0.4f, 0.35f));
            Icon(p, (x, y) => RoundBox(x, y, 128, 228, 22, 12, 6), C(0.35f, 0.75f, 1f), 6, 216, 240, false);
            p.Save(Dir + "icon_clock.png", true);

            // Sprout (regrowth).
            p = new Painter(256);
            Icon(p, (x, y) => Intersect(Circle(x, y, 128, 70, 80), 70 - y + 30), C(0.55f, 0.36f, 0.2f), 9, 20, 100);
            Icon(p, (x, y) => Segment(x, y, 128, 90, 128, 170, 8), C(0.35f, 0.68f, 0.28f), 6, 90, 170, false, false);
            Icon(p, R((x, y) => Ellipse(x, y, 84, 180, 50, 24), 84, 180, 30f), C(0.4f, 0.8f, 0.3f), 8, 150, 214);
            Icon(p, R((x, y) => Ellipse(x, y, 172, 196, 52, 26), 172, 196, -30f), C(0.45f, 0.85f, 0.35f), 8, 166, 232);
            p.Save(Dir + "icon_sprout.png", true);

            // Moon (night shift / offline earnings).
            p = new Painter(256);
            Func<float, float, float> moon = (x, y) => Subtract(Circle(x, y, 120, 128, 96), Circle(x, y, 170, 156, 80));
            Icon(p, moon, C(1f, 0.85f, 0.35f));
            DrawSparkle(p, 190, 70, 24, C(1f, 0.95f, 0.6f));
            DrawSparkle(p, 214, 130, 14, C(1f, 0.95f, 0.6f));
            p.Save(Dir + "icon_moon.png", true);

            // Two cups (bonus cup).
            p = new Painter(256);
            for (int k = 0; k < 2; k++)
            {
                float ox = k == 0 ? -40 : 40, oy = k == 0 ? 10 : -8;
                var cup = new[] { new Vector2(98 + ox, 30 + oy), new Vector2(158 + ox, 30 + oy), new Vector2(172 + ox, 170 + oy), new Vector2(84 + ox, 170 + oy) };
                Icon(p, (x, y) => Polygon(x, y, cup), k == 0 ? C(1f, 0.6f, 0.15f) : C(0.95f, 0.35f, 0.4f), 9, 30 + oy, 170 + oy);
                p.Fill((x, y) => Intersect(RoundBox(x, y, 128 + ox, 90 + oy, 60, 12, 4), Polygon(x, y, cup)), C(1f, 1f, 1f, 0.9f));
                Icon(p, (x, y) => RoundBox(x, y, 128 + ox, 176 + oy, 54, 11, 8), Color.white, 6, 165 + oy, 187 + oy, false);
            }
            Icon(p, (x, y) => Union(RoundBox(x, y, 204, 212, 30, 9, 4), RoundBox(x, y, 204, 212, 9, 30, 4)), C(0.35f, 0.85f, 0.4f), 6, 182, 242, false);
            p.Save(Dir + "icon_cups.png", true);
        }

        // ---------------------------------------------------------------- beach ground

        static void BeachTextures()
        {
            // Warm sand with ripples and specks. Tiles seamlessly.
            var rnd = new System.Random(71);
            var p = new Painter(512, 512, C(0.96f, 0.86f, 0.64f));
            for (int i = 0; i < 60; i++)
            {
                float x0 = (float)rnd.NextDouble() * 512, y0 = (float)rnd.NextDouble() * 512, r = 40 + (float)rnd.NextDouble() * 80;
                float v = ((float)rnd.NextDouble() - 0.5f) * 0.07f;
                var col = C(0.96f + v, 0.86f + v, 0.64f + v * 0.8f, 0.35f);
                Wrap(512, x0, y0, r, (cx, cy) => p.FillSoft((x, y) => Circle(x, y, cx, cy, r * 0.5f), col, r * 0.8f, cx - r, cy - r, cx + r, cy + r));
            }
            p.ForEach((x, y, c) =>
            {
                float w = Mathf.Sin((y + Mathf.Sin(x / 512f * Mathf.PI * 4f) * 10f) / 512f * Mathf.PI * 2f * 14f);
                float k = Mathf.Clamp01(w * 3f - 2.2f) * 0.05f;
                return C(c.r - k, c.g - k, c.b - k * 1.3f, 1f);
            });
            for (int i = 0; i < 900; i++)
            {
                float x0 = 3 + (float)rnd.NextDouble() * 506, y0 = 3 + (float)rnd.NextDouble() * 506;
                var col = rnd.NextDouble() < 0.5 ? C(0.8f, 0.68f, 0.5f, 0.6f) : C(1f, 0.97f, 0.9f, 0.7f);
                p.Fill((x, y) => Circle(x, y, x0, y0, 1.3f), col, x0 - 3, y0 - 3, x0 + 3, y0 + 3);
            }
            p.Noise(0.03f, 8);
            p.Save(Dir + "ground_sand.png", false, default, true);

            // Lush tropical grass (warmer, more saturated than the farm grass).
            rnd = new System.Random(72);
            p = new Painter(512, 512, C(0.42f, 0.76f, 0.3f));
            for (int i = 0; i < 70; i++)
            {
                float x0 = (float)rnd.NextDouble() * 512, y0 = (float)rnd.NextDouble() * 512, r = 40 + (float)rnd.NextDouble() * 70;
                float v = ((float)rnd.NextDouble() - 0.5f) * 0.1f;
                var col = C(0.42f + v * 0.6f, 0.76f + v, 0.3f + v * 0.2f, 0.35f);
                Wrap(512, x0, y0, r, (cx, cy) => p.FillSoft((x, y) => Circle(x, y, cx, cy, r * 0.6f), col, r * 0.8f, cx - r, cy - r, cx + r, cy + r));
            }
            for (int i = 0; i < 1500; i++)
            {
                float x0 = 6 + (float)rnd.NextDouble() * 500, y0 = 4 + (float)rnd.NextDouble() * 496;
                float lean = ((float)rnd.NextDouble() - 0.5f) * 7f;
                var col = rnd.NextDouble() < 0.4 ? C(0.58f, 0.9f, 0.38f, 0.55f) : C(0.28f, 0.58f, 0.22f, 0.5f);
                p.Fill((x, y) => Segment(x, y, x0, y0, x0 + lean, y0 + 9, 1.2f), col, x0 - 9, y0 - 2, x0 + 9, y0 + 12);
            }
            for (int i = 0; i < 70; i++)
            {
                float x0 = 5 + (float)rnd.NextDouble() * 502, y0 = 5 + (float)rnd.NextDouble() * 502;
                var col = rnd.NextDouble() < 0.5 ? C(1f, 0.55f, 0.7f, 0.9f) : C(1f, 0.9f, 0.4f, 0.9f);
                p.Fill((x, y) => Circle(x, y, x0, y0, 2.4f), col, x0 - 4, y0 - 4, x0 + 4, y0 + 4);
            }
            p.Save(Dir + "ground_tgrass.png", false, default, true);

            // Turquoise lagoon water with soft caustic lines.
            p = new Painter(256, 256, C(0.2f, 0.72f, 0.8f));
            p.ForEach((x, y, c) =>
            {
                float u = x / 256f, v = y / 256f;
                float w = Mathf.Sin(2f * Mathf.PI * (2f * u + 1f * v)) + Mathf.Sin(2f * Mathf.PI * (3f * u - 2f * v) + 1.3f) * 0.8f
                          + Mathf.Sin(2f * Mathf.PI * (1f * u + 4f * v) + 2.1f) * 0.6f;
                float line = Mathf.Clamp01(1f - Mathf.Abs(w) * 4.5f);
                var col = Color.Lerp(C(0.16f, 0.62f, 0.8f), C(0.26f, 0.78f, 0.86f), w * 0.15f + 0.5f);
                return Color.Lerp(col, C(0.8f, 0.97f, 0.98f), line * 0.28f);
            });
            p.Save(Dir + "ground_ocean.png", false, default, true);

            // Radial soft blob: grass-to-sand blending patches, foam, glows.
            p = new Painter(256);
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 127.5f) * (x - 127.5f) + (y - 127.5f) * (y - 127.5f)) / 127f;
                return new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - d)));
            });
            p.Save(Dir + "soft_blob.png", false);

            // Shore foam band: bright ring that fades both ways.
            p = new Painter(512);
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 255.5f) * (x - 255.5f) + (y - 255.5f) * (y - 255.5f)) / 255f;
                float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.08f);
                return new Color(1f, 1f, 1f, band * band * 0.9f);
            });
            p.Save(Dir + "shore_foam.png", false);

            // Unlock glow ring: bright rim with a soft outer halo and a faint inner fill.
            p = new Painter(512);
            p.ForEach((x, y, c) =>
            {
                float d = Mathf.Sqrt((x - 255.5f) * (x - 255.5f) + (y - 255.5f) * (y - 255.5f)) / 255f;
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.045f);
                float halo = Mathf.Clamp01(1f - Mathf.Abs(d - 0.84f) / 0.16f);
                float inner = d < 0.8f ? 0.16f + 0.1f * d : 0f;
                float a = Mathf.Max(rim, halo * halo * 0.5f);
                a = Mathf.Max(a, inner);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            });
            p.Save(Dir + "glow_ring.png", true);

            // Radial progress ring (white; tinted and filled via UI Image).
            p = new Painter(256);
            p.Fill((x, y) => Ring(x, y, 128, 128, 112, 20), Color.white);
            p.Save(Dir + "ring_progress.png", true);

            // Round station pad: tinted fill with a dashed white rim (rounded look for the tropical world).
            p = new Painter(256);
            p.Fill((x, y) => Circle(x, y, 128, 128, 124), C(0.12f, 0.1f, 0.1f, 0.6f));
            p.FillFn((x, y) => Circle(x, y, 128, 128, 116), (x, y) =>
            {
                float d = -Circle(x, y, 128, 128, 116);
                return C(0.7f, 0.7f, 0.7f, Mathf.Lerp(0.9f, 0.7f, Mathf.Clamp01(d / 30f)));
            });
            p.Fill((x, y) =>
            {
                float d = Mathf.Abs(Circle(x, y, 128, 128, 98)) - 6f;
                float a = Mathf.Atan2(y - 128, x - 128);
                float dash = Mathf.Repeat(a / (Mathf.PI * 2f) * 22f, 1f) < 0.62f ? -1f : 1f;
                return Mathf.Max(d, dash * 3f);
            }, Color.white);
            p.Fill((x, y) => Intersect(Circle(x, y, 128, 128, 116) + 3f, -(Circle(x, y, 128, 120, 116) + 3f)), C(1, 1, 1, 0.45f));
            p.Save(Dir + "pad_round.png", false);

            // Organic grass patch: tropical grass with a wobbly, feathered edge (laid over sand for soft blending).
            rnd = new System.Random(73);
            p = new Painter(512);
            var lobes = new Vector3[9];
            for (int i = 0; i < lobes.Length; i++)
            {
                float a = i / (float)lobes.Length * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.4f;
                float rr = 110f + (float)rnd.NextDouble() * 40f;
                lobes[i] = new Vector3(256 + Mathf.Cos(a) * rr, 256 + Mathf.Sin(a) * rr, 90 + (float)rnd.NextDouble() * 50);
            }
            var grassCol = new Color[4] { C(0.42f, 0.76f, 0.3f), C(0.48f, 0.82f, 0.34f), C(0.36f, 0.68f, 0.26f), C(0.52f, 0.84f, 0.36f) };
            p.ForEach((x, y, c) =>
            {
                float d = Circle(x, y, 256, 256, 150);
                foreach (var l in lobes) d = Mathf.Min(d, Circle(x, y, l.x, l.y, l.z));
                float a = Mathf.Clamp01(-d / 34f);
                float n = Mathf.Sin(x * 0.07f + Mathf.Sin(y * 0.05f) * 2f) * Mathf.Sin(y * 0.06f + 1.3f);
                var g = Color.Lerp(grassCol[0], grassCol[1], n * 0.5f + 0.5f);
                return new Color(g.r, g.g, g.b, Mathf.SmoothStep(0f, 1f, a));
            });
            for (int i = 0; i < 1600; i++)
            {
                float x0 = 20 + (float)rnd.NextDouble() * 472, y0 = 20 + (float)rnd.NextDouble() * 472;
                float lean = ((float)rnd.NextDouble() - 0.5f) * 7f;
                var col = rnd.NextDouble() < 0.4 ? grassCol[3] : grassCol[2];
                col.a = 0.5f;
                p.Fill((x, y) => Segment(x, y, x0, y0, x0 + lean, y0 + 9, 1.2f), col, x0 - 9, y0 - 2, x0 + 9, y0 + 12);
            }
            // Blades only where the patch is solid.
            p.ForEach((x, y, c) =>
            {
                float d = Circle(x, y, 256, 256, 150);
                foreach (var l in lobes) d = Mathf.Min(d, Circle(x, y, l.x, l.y, l.z));
                c.a = Mathf.Min(c.a, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-d / 34f)));
                return c;
            });
            p.Save(Dir + "grass_patch.png", false);

            // Foam strip (u = across the shore band): bright near the sand, fading out to sea, broken into dashes along v.
            p = new Painter(128, 128, new Color(1f, 1f, 1f, 0f));
            p.ForEach((x, y, c) =>
            {
                float u = x / 127f;
                float band = Mathf.Exp(-u * 4.5f) * Mathf.Clamp01(u * 12f);
                float lace = 0.75f + 0.25f * Mathf.Sin(y / 128f * Mathf.PI * 2f * 6f + u * 10f);
                float wave2 = Mathf.Clamp01(1f - Mathf.Abs(u - 0.45f) / 0.07f) * 0.5f * (0.5f + 0.5f * Mathf.Sin(y / 128f * Mathf.PI * 2f * 3f));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(band * lace + wave2));
            });
            p.Save(Dir + "foam_strip.png", false, default, true);

            // Road lane (u across, v along): warm asphalt, white edges, dashed centre line. Repeats along the road.
            p = new Painter(128, 256, C(0.42f, 0.42f, 0.46f));
            p.Noise(0.06f, 19);
            p.ForEach((x, y, c) =>
            {
                float u = x / 127f;
                if (u < 0.04f || u > 0.96f) return C(0.86f, 0.82f, 0.72f);
                if (Mathf.Abs(u - 0.09f) < 0.02f || Mathf.Abs(u - 0.91f) < 0.02f) return C(0.96f, 0.96f, 0.92f);
                if (Mathf.Abs(u - 0.5f) < 0.022f && (y % 128) < 72) return C(1f, 0.85f, 0.3f);
                return c;
            });
            p.Save(Dir + "road_lane.png", false, default, true);

            // Horizontal fade (u: 1 -> 0) for shallow-water banding around the island.
            p = new Painter(128, 8, new Color(1f, 1f, 1f, 0f));
            p.ForEach((x, y, c) => new Color(1f, 1f, 1f, Mathf.SmoothStep(1f, 0f, x / 127f)));
            p.Save(Dir + "gradient_strip.png", false, default, true);

            // Thatch: straw strands running to the roof tip, in overlapping tiers.
            rnd = new System.Random(77);
            p = new Painter(256, 256, C(0.86f, 0.7f, 0.4f));
            for (int i = 0; i < 700; i++)
            {
                float x0 = (float)rnd.NextDouble() * 256, y0 = (float)rnd.NextDouble() * 256, len = 18 + (float)rnd.NextDouble() * 30;
                var col = rnd.NextDouble() < 0.5 ? C(0.96f, 0.82f, 0.52f, 0.7f) : C(0.66f, 0.5f, 0.26f, 0.55f);
                p.Fill((x, y) => Segment(x, y, x0, y0, x0 + ((float)(i % 5) - 2f) * 0.6f, y0 + len, 1.1f), col, x0 - 4, y0 - 2, x0 + 4, y0 + len + 2);
            }
            p.ForEach((x, y, c) =>
            {
                float tier = Mathf.Repeat(y / 64f, 1f);
                float shade = tier < 0.12f ? Mathf.Lerp(0.72f, 1f, tier / 0.12f) : 1f;
                return C(c.r * shade, c.g * shade, c.b * shade, 1f);
            });
            p.Save(Dir + "thatch.png", false, default, true);
        }
    }
}
