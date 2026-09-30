using System;
using UnityEngine;
using static JuiceKing.EditorTools.Painter;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Expansion 2 art (Berry Blast): berry, cake, Golden Apple, fox and bakery icons, the berry juice vessels, and the
    /// village ground textures (meadow, cobbles, mulched soil) plus berry skins.
    /// </summary>
    public static partial class ArtGen
    {
        public static readonly string[] BerryFruitIcons = { "icon_strawberry.png", "icon_raspberry.png", "icon_blueberry.png", "icon_cranberry.png" };

        /// <summary>Cake icon per FruitKind (berries only).</summary>
        public static string CakeIconFile(int kind) => "icon_cake_" + kind + ".png";

        /// <summary>GLSL-style smoothstep (Mathf.SmoothStep interpolates between its first two arguments instead).</summary>
        static float Edge(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Only the Berry Blast art (quick iteration without regenerating everything).</summary>
        public static void GenerateBerry() => BerryArt();

        static void BerryArt()
        {
            BerryIcons();
            CakeIcons();
            PremiumIcons();
            BakeryIcons();
            BerryTextures();
        }

        // ---------------------------------------------------------------- berry icons

        static void BerryIcons()
        {
            // Strawberry: plump heart with golden seeds and a leafy crown.
            var p = new Painter(256);
            Func<float, float, float> berry = (x, y) => Union(Ellipse(x, y, 128, 138, 88, 72),
                Polygon(x, y, new[] { new Vector2(52, 132), new Vector2(204, 132), new Vector2(150, 50), new Vector2(128, 32), new Vector2(106, 50) }));
            p.FillSoft((x, y) => berry(x, y + 9f) - 9f, C(0f, 0f, 0f, 0.28f), 8f);
            p.Fill((x, y) => berry(x, y) - 9f, Outline);
            p.FillFn(berry, (x, y) => Color.Lerp(C(0.78f, 0.06f, 0.14f), C(1f, 0.3f, 0.32f), Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(40, 200, y))));
            for (int row = 0; row < 5; row++)
                for (int i = 0; i < 6; i++)
                {
                    float sy = 70 + row * 28f, sx = 76 + i * 21f + (row % 2) * 10f;
                    if (berry(sx, sy) > -10f) continue;
                    p.Fill((x, y) => Ellipse(x, y, sx, sy, 3.5f, 5.5f), C(1f, 0.85f, 0.35f));
                    p.Fill((x, y) => Ellipse(x, y, sx - 1f, sy + 1.5f, 1.4f, 2f), C(1f, 1f, 0.8f, 0.8f));
                }
            Gloss(p, berry, 92, 160, 26, 16, -35f, 0.45f);
            for (int i = 0; i < 5; i++)
            {
                float a = (i / 5f) * Mathf.PI + 0.1f;
                float lx = 128 + Mathf.Cos(a) * 34, ly = 200 + Mathf.Sin(a) * 8;
                var leaf = R((x, y) => Ellipse(x, y, lx, ly, 26, 11), lx, ly, a * Mathf.Rad2Deg);
                Icon(p, leaf, C(0.3f, 0.7f, 0.28f), 5, 186, 222, false);
            }
            Icon(p, (x, y) => Segment(x, y, 128, 206, 136, 238, 6), C(0.35f, 0.6f, 0.25f), 5, 206, 240, false, false);
            p.Save(Dir + "icon_strawberry.png", true);

            // Raspberry: dome of glossy drupelets.
            p = new Painter(256);
            Func<float, float, float> dome = (x, y) => Union(Ellipse(x, y, 128, 118, 82, 88), Circle(x, y, 128, 60, 50));
            p.FillSoft((x, y) => dome(x, y + 9f) - 8f, C(0f, 0f, 0f, 0.28f), 8f);
            p.Fill((x, y) => dome(x, y) - 8f, Outline);
            p.Fill(dome, C(0.7f, 0.08f, 0.3f));
            var rnd = new System.Random(3);
            for (int row = 0; row < 7; row++)
                for (int i = 0; i < 7; i++)
                {
                    float sx = 60 + i * 23f + (row % 2) * 11f, sy = 44 + row * 25f;
                    if (dome(sx, sy) > -6f) continue;
                    float r = 14f + (float)rnd.NextDouble() * 2f;
                    p.FillFn((x, y) => Circle(x, y, sx, sy, r), (x, y) =>
                        Color.Lerp(C(0.82f, 0.14f, 0.38f), C(1f, 0.45f, 0.62f), Mathf.InverseLerp(sy - r, sy + r, y)));
                    p.Fill((x, y) => Circle(x, y, sx - 4f, sy + 5f, 3.5f), C(1f, 1f, 1f, 0.7f));
                }
            for (int i = 0; i < 4; i++)
            {
                float a = 0.3f + i * 0.8f;
                float lx = 128 + Mathf.Cos(a) * 28, ly = 206 + Mathf.Sin(a) * 6;
                Icon(p, R((x, y) => Ellipse(x, y, lx, ly, 22, 9), lx, ly, a * Mathf.Rad2Deg), C(0.35f, 0.66f, 0.3f), 5, 196, 220, false);
            }
            p.Save(Dir + "icon_raspberry.png", true);

            // Blueberries: three dusky berries with star-shaped crowns.
            p = new Painter(256);
            var blues = new[] { new Vector3(92, 104, 58), new Vector3(166, 98, 54), new Vector3(128, 164, 56) };
            foreach (var b in blues)
            {
                var bb = b;
                Func<float, float, float> c = (x, y) => Circle(x, y, bb.x, bb.y, bb.z);
                p.FillSoft((x, y) => c(x, y + 8f) - 8f, C(0f, 0f, 0f, 0.25f), 7f);
                p.Fill((x, y) => c(x, y) - 8f, Outline);
                p.FillFn(c, (x, y) => Color.Lerp(C(0.16f, 0.2f, 0.52f), C(0.45f, 0.55f, 0.92f), Mathf.InverseLerp(bb.y - bb.z, bb.y + bb.z, y)));
                p.Fill((x, y) => Intersect(c(x, y) + 4f, -(c(x, y + 8f) + 4f)), C(0.8f, 0.85f, 1f, 0.35f));
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 5f * Mathf.PI * 2f + 0.3f;
                    p.Fill((x, y) => Segment(x, y, bb.x, bb.y + bb.z * 0.45f, bb.x + Mathf.Cos(a) * 11f, bb.y + bb.z * 0.45f + Mathf.Sin(a) * 11f, 3.5f), C(0.1f, 0.1f, 0.25f));
                }
                Gloss(p, c, bb.x - bb.z * 0.35f, bb.y + bb.z * 0.1f, bb.z * 0.28f, bb.z * 0.16f, -30f, 0.3f);
            }
            p.Save(Dir + "icon_blueberry.png", true);

            // Cranberries: glossy deep-red ovals on a sprig.
            p = new Painter(256);
            Icon(p, (x, y) => Segment(x, y, 70, 210, 190, 190, 5), C(0.45f, 0.3f, 0.18f), 4, 180, 220, false, false);
            Icon(p, R((x, y) => Ellipse(x, y, 186, 212, 30, 12), 186, 212, 20f), C(0.3f, 0.62f, 0.3f), 5, 196, 226, false);
            var crans = new[] { new Vector3(90, 118, 50), new Vector3(164, 124, 48), new Vector3(126, 66, 50) };
            foreach (var b in crans)
            {
                var bb = b;
                Func<float, float, float> c = (x, y) => Ellipse(x, y, bb.x, bb.y, bb.z * 0.92f, bb.z);
                p.FillSoft((x, y) => c(x, y + 8f) - 8f, C(0f, 0f, 0f, 0.25f), 7f);
                p.Fill((x, y) => c(x, y) - 8f, Outline);
                p.FillFn(c, (x, y) => Color.Lerp(C(0.5f, 0.02f, 0.1f), C(0.95f, 0.2f, 0.28f), Mathf.InverseLerp(bb.y - bb.z, bb.y + bb.z, y)));
                Gloss(p, c, bb.x - bb.z * 0.3f, bb.y + bb.z * 0.35f, bb.z * 0.3f, bb.z * 0.18f, -30f, 0.6f);
                p.Fill((x, y) => Circle(x, y, bb.x + bb.z * 0.4f, bb.y - bb.z * 0.3f, 4f), C(1f, 1f, 1f, 0.45f));
            }
            p.Save(Dir + "icon_cranberry.png", true);
        }

        // ---------------------------------------------------------------- berry juice vessels (called from JuiceIcons)

        /// <summary>Strawberry smoothie: tall curvy glass, pink swirl, whipped cream, a strawberry on top.</summary>
        static void SmoothieGlass(Painter p, Color juice)
        {
            Straw(p, 150, 190, 180, 250, C(1f, 1f, 1f));
            Func<float, float, float> glass = (x, y) => Union(Polygon(x, y, new[] { new Vector2(96, 22), new Vector2(160, 22), new Vector2(182, 170), new Vector2(74, 170) }),
                Ellipse(x, y, 128, 30, 40, 12));
            Icon(p, glass, C(0.86f, 0.94f, 1f), 9, 20, 180);
            Func<float, float, float> liq = (x, y) => Polygon(x, y, new[] { new Vector2(100, 32), new Vector2(156, 32), new Vector2(174, 162), new Vector2(82, 162) });
            p.FillFn(liq, (x, y) => Color.Lerp(Dark(juice, 0.1f), Light(juice, 0.3f), Mathf.InverseLerp(30, 162, y) + Mathf.Sin(y * 0.08f + x * 0.02f) * 0.12f));
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(100, 40), new Vector2(112, 40), new Vector2(104, 160), new Vector2(90, 160) }), C(1f, 1f, 1f, 0.35f));
            Func<float, float, float> cream = (x, y) => Union(Union(Circle(x, y, 100, 176, 26), Circle(x, y, 156, 176, 26)), Circle(x, y, 128, 196, 30));
            Icon(p, cream, C(1f, 0.99f, 0.96f), 7, 150, 226);
            Func<float, float, float> sb = (x, y) => Union(Ellipse(x, y, 128, 222, 24, 18), Polygon(x, y, new[] { new Vector2(106, 220), new Vector2(150, 220), new Vector2(128, 196) }));
            Icon(p, sb, C(0.95f, 0.15f, 0.22f), 6, 196, 240);
            p.Fill((x, y) => Ellipse(x, y, 128, 242, 12, 5), C(0.3f, 0.7f, 0.3f));
        }

        /// <summary>Raspberry fizz: mason jar with a lid, straw, bubbles and floating berries.</summary>
        static void MasonJar(Painter p, Color juice)
        {
            Straw(p, 148, 200, 170, 252, C(0.95f, 0.4f, 0.6f));
            Func<float, float, float> jar = (x, y) => RoundBox(x, y, 128, 104, 64, 84, 22);
            Icon(p, jar, C(0.86f, 0.94f, 1f), 9, 20, 190);
            Func<float, float, float> liq = (x, y) => RoundBox(x, y, 128, 96, 54, 70, 16);
            p.FillFn(liq, (x, y) => Color.Lerp(Dark(juice, 0.15f), Light(juice, 0.25f), Mathf.InverseLerp(26, 166, y)));
            var rnd = new System.Random(8);
            for (int i = 0; i < 12; i++)
            {
                float bx = 86 + (float)rnd.NextDouble() * 84, by = 36 + (float)rnd.NextDouble() * 120, br = 3f + (float)rnd.NextDouble() * 4f;
                p.Fill((x, y) => Ring(x, y, bx, by, br, 1.6f), C(1f, 1f, 1f, 0.55f));
            }
            foreach (var b in new[] { new Vector2(104, 140), new Vector2(150, 118) })
            {
                var bb = b;
                p.Fill((x, y) => Circle(x, y, bb.x, bb.y, 13), C(0.85f, 0.16f, 0.4f));
                p.Fill((x, y) => Circle(x, y, bb.x - 4, bb.y + 4, 3), C(1f, 1f, 1f, 0.6f));
            }
            Icon(p, (x, y) => RoundBox(x, y, 128, 194, 70, 14, 6), C(0.85f, 0.7f, 0.3f), 6, 180, 208, false);
            p.Fill((x, y) => RoundBox(x, y, 128, 194, 64, 3, 2), C(0.65f, 0.5f, 0.2f, 0.7f));
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(74, 40), new Vector2(86, 40), new Vector2(86, 170), new Vector2(74, 170) }), C(1f, 1f, 1f, 0.3f));
        }

        /// <summary>Blueberry shake: milk bottle with a gingham cap and a label.</summary>
        static void MilkBottle(Painter p, Color juice)
        {
            Func<float, float, float> bottle = (x, y) => Union(RoundBox(x, y, 128, 90, 58, 72, 26), RoundBox(x, y, 128, 186, 30, 36, 12));
            Icon(p, bottle, C(0.9f, 0.95f, 1f), 9, 16, 226);
            Func<float, float, float> liq = (x, y) => Union(RoundBox(x, y, 128, 84, 48, 60, 20), RoundBox(x, y, 128, 160, 22, 20, 8));
            p.FillFn(liq, (x, y) => Color.Lerp(Dark(juice, 0.15f), Light(juice, 0.35f), Mathf.InverseLerp(24, 180, y)));
            Icon(p, (x, y) => RoundBox(x, y, 128, 226, 36, 14, 8), C(0.35f, 0.5f, 0.95f), 6, 212, 240, false);
            for (int i = 0; i < 4; i++)
            {
                float cx = 102 + i * 17f;
                p.Fill((x, y) => Intersect(RoundBox(x, y, cx, 226, 4, 14, 1), RoundBox(x, y, 128, 226, 34, 12, 6)), C(1f, 1f, 1f, 0.6f));
            }
            Icon(p, (x, y) => RoundBox(x, y, 128, 88, 44, 22, 8), C(1f, 0.98f, 0.92f), 5, 66, 110, false);
            foreach (var b in new[] { new Vector2(114, 88), new Vector2(136, 92), new Vector2(126, 80) })
            {
                var bb = b;
                p.Fill((x, y) => Circle(x, y, bb.x, bb.y, 8), C(0.25f, 0.3f, 0.7f));
                p.Fill((x, y) => Circle(x, y, bb.x - 2, bb.y + 2, 2), C(1f, 1f, 1f, 0.6f));
            }
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(78, 40), new Vector2(90, 40), new Vector2(90, 146), new Vector2(78, 146) }), C(1f, 1f, 1f, 0.35f));
        }

        /// <summary>Cranberry cooler: tumbler with ice cubes, a lime wheel and a sugared rim.</summary>
        static void CoolerGlass(Painter p, Color juice)
        {
            Straw(p, 104, 180, 80, 246, C(0.3f, 0.8f, 0.4f));
            var outer = new[] { new Vector2(74, 24), new Vector2(182, 24), new Vector2(192, 180), new Vector2(64, 180) };
            Icon(p, (x, y) => Polygon(x, y, outer), C(0.88f, 0.95f, 1f), 9, 20, 184);
            Func<float, float, float> liq = (x, y) => Polygon(x, y, new[] { new Vector2(82, 34), new Vector2(174, 34), new Vector2(182, 160), new Vector2(74, 160) });
            p.FillFn(liq, (x, y) => Color.Lerp(Dark(juice, 0.2f), Light(juice, 0.15f), Mathf.InverseLerp(34, 160, y)));
            foreach (var c in new[] { new Vector3(104, 132, 12f), new Vector3(146, 118, -18f), new Vector3(122, 92, 30f) })
            {
                var cc = c;
                var cube = R((x, y) => RoundBox(x, y, cc.x, cc.y, 17, 17, 5), cc.x, cc.y, cc.z);
                p.Fill(cube, C(1f, 1f, 1f, 0.45f));
                p.Fill((x, y) => Intersect(cube(x, y) + 3f, -(cube(x, y + 6f) + 3f)), C(1f, 1f, 1f, 0.6f));
            }
            p.Fill((x, y) => RoundBox(x, y, 128, 180, 62, 7, 4), C(1f, 1f, 1f, 0.9f));
            Icon(p, (x, y) => Circle(x, y, 188, 180, 28), C(0.55f, 0.85f, 0.3f), 6, 152, 208, false);
            p.Fill((x, y) => Circle(x, y, 188, 180, 20), C(0.85f, 1f, 0.6f));
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                p.Fill((x, y) => Segment(x, y, 188, 180, 188 + Mathf.Cos(a) * 20, 180 + Mathf.Sin(a) * 20, 1.6f), C(1f, 1f, 0.9f));
            }
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(82, 38), new Vector2(96, 38), new Vector2(90, 170), new Vector2(74, 170) }), C(1f, 1f, 1f, 0.3f));
        }

        // ---------------------------------------------------------------- cake icons

        static void CakeIcons()
        {
            // Shared: a round layer cake seen from the side and slightly above.
            void Layers(Painter p, Color sponge, Color filling, Color frosting, float top = 176f)
            {
                Func<float, float, float> cake = (x, y) => Union(RoundBox(x, y, 128, 100, 92, 66, 16), Ellipse(x, y, 128, 166, 92, 22));
                p.FillSoft((x, y) => cake(x, y + 9f) - 8f, C(0f, 0f, 0f, 0.28f), 8f);
                p.Fill((x, y) => cake(x, y) - 8f, Outline);
                p.FillFn(cake, (x, y) => Color.Lerp(Dark(sponge, 0.1f), Light(sponge, 0.15f), Mathf.InverseLerp(30, 170, y)));
                foreach (var ly in new[] { 74f, 120f })
                {
                    float yy = ly;
                    p.Fill((x, y) => Intersect(RoundBox(x, y, 128, yy, 94, 9, 4), cake(x, y)), filling);
                    p.Fill((x, y) => Intersect(RoundBox(x, y, 128, yy + 3, 94, 3, 2), cake(x, y)), Light(filling, 0.4f));
                }
                p.Fill((x, y) => Ellipse(x, y, 128, top - 10, 90, 20), frosting);
                for (int i = 0; i < 7; i++)
                {
                    float dx = 50 + i * 26f, len = 14 + (i * 37 % 5) * 5f;
                    p.Fill((x, y) => Union(Segment(x, y, dx, top - 20, dx, top - 20 - len, 7f), Circle(x, y, dx, top - 20 - len, 8f)), frosting);
                }
                p.Fill((x, y) => Ellipse(x, y, 104, top - 4, 40, 7), C(1f, 1f, 1f, 0.45f));
            }

            // Strawberry shortcake: cream layers, strawberries on top.
            var p = new Painter(256);
            Layers(p, C(1f, 0.88f, 0.62f), C(0.95f, 0.25f, 0.3f), C(1f, 0.98f, 0.95f));
            foreach (var s in new[] { new Vector2(92, 196), new Vector2(128, 206), new Vector2(164, 196) })
            {
                var ss = s;
                Func<float, float, float> sb = (x, y) => Union(Ellipse(x, y, ss.x, ss.y, 18, 15), Polygon(x, y, new[] { new Vector2(ss.x - 17, ss.y - 2), new Vector2(ss.x + 17, ss.y - 2), new Vector2(ss.x, ss.y - 26) }));
                Icon(p, sb, C(0.95f, 0.15f, 0.22f), 5, ss.y - 26, ss.y + 16);
                p.Fill((x, y) => Ellipse(x, y, ss.x, ss.y + 15, 10, 5), C(0.3f, 0.7f, 0.3f));
            }
            p.Save(Dir + CakeIconFile((int)FruitKind.Strawberry), true);

            // Raspberry velvet: pink frosting with raspberries.
            p = new Painter(256);
            Layers(p, C(0.85f, 0.25f, 0.4f), C(1f, 0.92f, 0.95f), C(1f, 0.6f, 0.75f));
            foreach (var s in new[] { new Vector2(96, 196), new Vector2(128, 204), new Vector2(160, 196), new Vector2(112, 212), new Vector2(146, 212) })
            {
                var ss = s;
                Icon(p, (x, y) => Circle(x, y, ss.x, ss.y, 13), C(0.85f, 0.14f, 0.38f), 5, ss.y - 13, ss.y + 13);
                p.Fill((x, y) => Circle(x, y, ss.x - 3, ss.y + 4, 3), C(1f, 1f, 1f, 0.6f));
            }
            p.Save(Dir + CakeIconFile((int)FruitKind.Raspberry), true);

            // Blueberry cheesecake: cream body, biscuit base, purple topping running down.
            p = new Painter(256);
            Func<float, float, float> cheese = (x, y) => Union(RoundBox(x, y, 128, 108, 94, 56, 12), Ellipse(x, y, 128, 164, 94, 22));
            p.FillSoft((x, y) => cheese(x, y + 9f) - 8f, C(0f, 0f, 0f, 0.28f), 8f);
            p.Fill((x, y) => cheese(x, y) - 8f, Outline);
            p.FillFn(cheese, (x, y) => Color.Lerp(C(0.95f, 0.86f, 0.64f), C(1f, 0.97f, 0.86f), Mathf.InverseLerp(52, 170, y)));
            p.Fill((x, y) => Intersect(RoundBox(x, y, 128, 62, 96, 12, 8), cheese(x, y) - 2f), C(0.72f, 0.48f, 0.26f));
            Color topping = C(0.4f, 0.22f, 0.62f);
            p.Fill((x, y) => Ellipse(x, y, 128, 166, 90, 20), topping);
            for (int i = 0; i < 6; i++)
            {
                float dx = 56 + i * 29f, len = 18 + (i * 29 % 4) * 9f;
                p.Fill((x, y) => Union(Segment(x, y, dx, 150, dx, 150 - len, 8f), Circle(x, y, dx, 150 - len, 9f)), topping);
            }
            foreach (var s in new[] { new Vector2(98, 176), new Vector2(128, 184), new Vector2(158, 176), new Vector2(116, 166), new Vector2(142, 166) })
            {
                var ss = s;
                p.Fill((x, y) => Circle(x, y, ss.x, ss.y, 11), C(0.22f, 0.26f, 0.62f));
                p.Fill((x, y) => Circle(x, y, ss.x - 3, ss.y + 3, 3), C(0.8f, 0.85f, 1f, 0.6f));
            }
            p.Save(Dir + CakeIconFile((int)FruitKind.Blueberry), true);

            // Cranberry tart: fluted golden shell brimming with glossy berries, dusted with sugar.
            p = new Painter(256);
            Func<float, float, float> shell = (x, y) =>
            {
                float d = RoundBox(x, y, 128, 96, 100, 42, 18);
                float flute = Mathf.Sin(x * 0.32f) * 3f;
                return d + flute;
            };
            Icon(p, shell, C(0.95f, 0.72f, 0.36f), 8, 50, 140);
            p.Fill((x, y) => Ellipse(x, y, 128, 136, 92, 22), C(0.6f, 0.05f, 0.12f));
            var rnd = new System.Random(21);
            for (int i = 0; i < 16; i++)
            {
                float bx = 56 + (float)rnd.NextDouble() * 144, by = 124 + (float)rnd.NextDouble() * 34;
                if (Ellipse(bx, by, 128, 138, 86, 22) > -4f) continue;
                p.Fill((x, y) => Circle(x, y, bx, by, 10), C(0.82f, 0.1f, 0.2f));
                p.Fill((x, y) => Circle(x, y, bx - 3, by + 3, 2.6f), C(1f, 1f, 1f, 0.7f));
            }
            for (int i = 0; i < 40; i++)
            {
                float sx = 50 + (float)rnd.NextDouble() * 156, sy = 118 + (float)rnd.NextDouble() * 44;
                p.Fill((x, y) => Circle(x, y, sx, sy, 1.4f), C(1f, 1f, 1f, 0.85f));
            }
            Icon(p, R((x, y) => Ellipse(x, y, 176, 176, 26, 11), 176, 176, 25f), C(0.35f, 0.7f, 0.35f), 5, 164, 190, false);
            p.Save(Dir + CakeIconFile((int)FruitKind.Cranberry), true);
        }

        // ---------------------------------------------------------------- golden apple, fox

        static void PremiumIcons()
        {
            // Golden Apple: shiny gold apple with a leaf and sparkles.
            var p = new Painter(256);
            Func<float, float, float> apple = (x, y) => Union(Union(Circle(x, y, 98, 128, 74), Circle(x, y, 158, 128, 74)), Ellipse(x, y, 128, 90, 84, 66));
            Func<float, float, float> appleCut = (x, y) => Subtract(apple(x, y), Circle(x, y, 128, 212, 24));
            p.FillSoft((x, y) => appleCut(x, y + 9f) - 9f, C(0.4f, 0.25f, 0f, 0.35f), 9f);
            p.Fill((x, y) => appleCut(x, y) - 9f, C(0.35f, 0.18f, 0.02f));
            p.FillFn(appleCut, (x, y) =>
            {
                float t = Mathf.InverseLerp(30, 210, y);
                float band = 0.5f + 0.5f * Mathf.Sin((x + y) * 0.03f);
                return Color.Lerp(Color.Lerp(C(0.85f, 0.45f, 0.05f), C(1f, 0.82f, 0.2f), t), C(1f, 0.95f, 0.55f), band * 0.25f);
            });
            p.Fill((x, y) => Intersect(appleCut(x, y) + 4f, -(appleCut(x, y + 9f) + 4f)), C(1f, 1f, 0.8f, 0.55f));
            Gloss(p, appleCut, 90, 150, 30, 20, -30f, 0.65f);
            p.Fill((x, y) => Circle(x, y, 78, 122, 7), C(1f, 1f, 1f, 0.85f));
            Icon(p, (x, y) => Segment(x, y, 128, 190, 138, 236, 7), C(0.45f, 0.28f, 0.12f), 5, 190, 238, false, false);
            Icon(p, R((x, y) => Ellipse(x, y, 170, 222, 34, 14), 170, 222, 22f), C(0.35f, 0.78f, 0.3f), 6, 206, 238, false);
            foreach (var s in new[] { new Vector3(214, 70, 16), new Vector3(44, 196, 12), new Vector3(210, 170, 9) })
            {
                var ss = s;
                p.Fill((x, y) => Union(Segment(x, y, ss.x - ss.z, ss.y, ss.x + ss.z, ss.y, 2.5f), Segment(x, y, ss.x, ss.y - ss.z, ss.x, ss.y + ss.z, 2.5f)), C(1f, 0.97f, 0.7f));
            }
            p.Save(Dir + "icon_golden_apple.png", true);

            // Fox face (matches the Berry Blast atlas fox): big dark-lined ears with cream fluff, a wide head with fluffy
            // cheek tufts, white muzzle, sly brows over glossy amber eyes, a shiny nose and a toothy smirk.
            p = new Painter(256);
            Color orange = C(0.98f, 0.46f, 0.1f), cream = C(1f, 0.96f, 0.9f), ink = C(0.14f, 0.07f, 0.05f);
            Func<float, float, float> head = (x, y) => Union(Union(Ellipse(x, y, 128, 130, 88, 66), Ellipse(x, y, 128, 92, 52, 40)),
                Union(Polygon(x, y, new[] { new Vector2(48, 146), new Vector2(14, 92), new Vector2(76, 100) }),
                      Polygon(x, y, new[] { new Vector2(208, 146), new Vector2(242, 92), new Vector2(180, 100) })));
            p.FillSoft((x, y) => head(x, y + 10f) - 8f, C(0f, 0f, 0f, 0.3f), 9f);
            foreach (var sx in new[] { -1f, 1f })
            {
                float s = sx;
                Func<float, float, float> ear = (x, y) => Polygon(x, y, new[] { new Vector2(128 + s * 22, 170), new Vector2(128 + s * 92, 150), new Vector2(128 + s * 76, 244) });
                p.FillSoft((x, y) => ear(x, y + 8f) - 8f, C(0f, 0f, 0f, 0.25f), 8f);
                Icon(p, ear, orange, 8, 150, 244, false);
                // Dark inner ear with a cream tuft at its base.
                Func<float, float, float> inner = (x, y) => Polygon(x, y, new[] { new Vector2(128 + s * 40, 172), new Vector2(128 + s * 80, 162), new Vector2(128 + s * 72, 224) });
                p.Fill(inner, C(0.36f, 0.14f, 0.1f));
                p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(128 + s * 44, 170), new Vector2(128 + s * 76, 164), new Vector2(128 + s * 62, 196) }), cream);
            }
            Icon(p, head, orange, 9, 40, 196);
            // Forehead blaze and cheek fluff.
            p.Fill((x, y) => Intersect(head(x, y) + 2f, Polygon(x, y, new[] { new Vector2(112, 190), new Vector2(144, 190), new Vector2(128, 150) })), C(1f, 0.62f, 0.25f));
            Func<float, float, float> muzzle = (x, y) => Union(Union(Ellipse(x, y, 88, 102, 46, 30), Ellipse(x, y, 168, 102, 46, 30)), Ellipse(x, y, 128, 80, 38, 30));
            Func<float, float, float> tufts = (x, y) => Union(Polygon(x, y, new[] { new Vector2(40, 112), new Vector2(18, 92), new Vector2(62, 96) }),
                Polygon(x, y, new[] { new Vector2(216, 112), new Vector2(238, 92), new Vector2(194, 96) }));
            p.FillFn((x, y) => Intersect(Union(muzzle(x, y), tufts(x, y)), head(x, y) + 3f), (x, y) => Color.Lerp(C(0.94f, 0.88f, 0.82f), cream, Mathf.InverseLerp(56, 120, y)));
            // Eyes: sly brows, amber irises, pupils and a glint.
            foreach (var ex in new[] { 94f, 162f })
            {
                float e = ex, side = e < 128 ? -1f : 1f;
                Func<float, float, float> eye = R((xx, yy) => Ellipse(xx, yy, e, 134, 19, 14), e, 134, side * 10f);
                p.Fill((x, y) => eye(x, y) - 3f, ink);
                p.Fill(eye, C(1f, 1f, 0.96f));
                p.Fill((x, y) => Intersect(Circle(x, y, e + side * -3f, 131, 11), eye(x, y)), C(0.95f, 0.62f, 0.1f));
                p.Fill((x, y) => Intersect(Circle(x, y, e + side * -3f, 131, 6), eye(x, y)), ink);
                p.Fill((x, y) => Circle(x, y, e + side * -6f, 136, 3.2f), C(1f, 1f, 1f, 0.95f));
                // Brow: thick stroke, low on the inside for a cheeky scowl.
                p.Fill((x, y) => Segment(x, y, e - side * 20f, 154, e + side * 16f, 166, 5f), ink);
            }
            // Nose, mouth and a little fang.
            p.Fill((x, y) => Ellipse(x, y, 128, 92, 16, 11), ink);
            p.Fill((x, y) => Ellipse(x, y, 123, 96, 6, 3.5f), C(1f, 1f, 1f, 0.7f));
            p.Fill((x, y) => Segment(x, y, 128, 82, 128, 72, 3f), ink);
            p.Fill((x, y) => Intersect(Ring(x, y, 128, 86, 22, 5f), y - 72), ink);
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(136, 66), new Vector2(146, 67), new Vector2(141, 56) }), C(1f, 1f, 1f));
            Gloss(p, head, 98, 176, 28, 12, -18f, 0.35f);
            p.Save(Dir + "icon_fox.png", true);
        }

        // ---------------------------------------------------------------- bakery icons

        static void BakeryIcons()
        {
            // Oven: brick-red body, glowing window, dial.
            var p = new Painter(256);
            Icon(p, (x, y) => RoundBox(x, y, 128, 118, 96, 90, 22), C(0.95f, 0.45f, 0.35f), 9, 28, 208);
            Icon(p, (x, y) => RoundBox(x, y, 128, 100, 70, 50, 14), C(0.3f, 0.2f, 0.2f), 5, 50, 150, false);
            p.FillFn((x, y) => RoundBox(x, y, 128, 100, 58, 38, 10), (x, y) => Color.Lerp(C(1f, 0.5f, 0.1f), C(1f, 0.85f, 0.35f), Mathf.InverseLerp(62, 138, y)));
            p.Fill((x, y) => RoundBox(x, y, 128, 176, 70, 10, 5), C(0.3f, 0.2f, 0.2f));
            foreach (var dx in new[] { 88f, 128f, 168f })
            {
                float d = dx;
                Icon(p, (x, y) => Circle(x, y, d, 178, 11), C(0.95f, 0.9f, 0.8f), 4, 166, 190, false);
            }
            p.Fill((x, y) => Ellipse(x, y, 112, 118, 30, 8), C(0.7f, 0.35f, 0.15f));
            p.Save(Dir + "icon_oven.png", true);

            // Cake mixer: bowl with a whisk and batter.
            p = new Painter(256);
            Icon(p, (x, y) => Segment(x, y, 150, 230, 128, 120, 8), C(0.75f, 0.78f, 0.82f), 5, 110, 236, false);
            for (int i = 0; i < 3; i++)
            {
                float o = (i - 1) * 16f;
                p.Fill((x, y) => Ring(x, y, 128 + o * 0.3f, 128, 22 + Mathf.Abs(o) * 0.5f, 3f), C(0.75f, 0.78f, 0.82f));
            }
            Func<float, float, float> bowl = (x, y) => Intersect(Circle(x, y, 128, 130, 96), 130 - y);
            Icon(p, bowl, C(0.4f, 0.75f, 0.95f), 9, 34, 130);
            Icon(p, (x, y) => Ellipse(x, y, 128, 132, 96, 18), C(1f, 0.85f, 0.9f), 6, 114, 150, false);
            p.Fill((x, y) => Ellipse(x, y, 108, 136, 30, 6), C(1f, 1f, 1f, 0.6f));
            p.Save(Dir + "icon_mixer.png", true);

            // Cake shop: a slice of layer cake with a cherry.
            p = new Painter(256);
            Func<float, float, float> slice = (x, y) => Polygon(x, y, new[] { new Vector2(40, 60), new Vector2(216, 60), new Vector2(216, 150), new Vector2(40, 190) });
            Icon(p, slice, C(1f, 0.86f, 0.6f), 9, 60, 190);
            foreach (var ly in new[] { 90f, 124f })
            {
                float yy = ly;
                p.Fill((x, y) => Intersect(Polygon(x, y, new[] { new Vector2(40, yy - 7), new Vector2(216, yy - 7), new Vector2(216, yy + 7), new Vector2(40, yy + 7 + (216 - 216) * 0f) }), slice(x, y)), C(1f, 0.45f, 0.55f));
            }
            p.Fill((x, y) => Intersect(Polygon(x, y, new[] { new Vector2(40, 172), new Vector2(216, 136), new Vector2(216, 150), new Vector2(40, 190) }), slice(x, y) - 1f), C(1f, 0.98f, 0.95f));
            Icon(p, (x, y) => Circle(x, y, 80, 206, 18), C(0.9f, 0.1f, 0.18f), 6, 188, 224, false);
            p.Save(Dir + "icon_cakeshop.png", true);

            // Paw print (fox tracks).
            p = new Painter(128);
            p.Fill((x, y) => Ellipse(x, y, 64, 48, 26, 22), C(0.3f, 0.18f, 0.12f, 0.9f));
            foreach (var t in new[] { new Vector2(34, 84), new Vector2(54, 100), new Vector2(76, 100), new Vector2(96, 84) })
            {
                var tt = t;
                p.Fill((x, y) => Ellipse(x, y, tt.x, tt.y, 10, 13), C(0.3f, 0.18f, 0.12f, 0.9f));
            }
            p.Save(Dir + "paw.png", false);
        }

        // ---------------------------------------------------------------- textures

        static void BerryTextures()
        {
            const int S = 512;
            // Meadow: lush tileable grass, darker clover patches, tiny flower dots.
            var p = new Painter(S, S, Color.green);
            var dots = new System.Random(77);
            var flowers = new Vector4[260];
            for (int i = 0; i < flowers.Length; i++)
                flowers[i] = new Vector4((float)dots.NextDouble() * S, (float)dots.NextDouble() * S, 1.5f + (float)dots.NextDouble() * 2.5f, dots.Next(4));
            Color[] flowerCols = { C(1f, 1f, 0.95f), C(1f, 0.9f, 0.35f), C(1f, 0.62f, 0.78f), C(0.75f, 0.7f, 1f) };
            p.ForEach((x, y, c) =>
            {
                float u = x / (float)S, v = y / (float)S;
                float n = TileNoise(u * 6f, v * 6f, 6, 71) * 0.5f + TileNoise(u * 14f, v * 14f, 14, 72) * 0.3f + TileNoise(u * 40f, v * 40f, 40, 73) * 0.2f;
                float clover = Edge(0.55f, 0.75f, TileNoise(u * 9f, v * 9f, 9, 74));
                // Bright spring green (the Game view grading pulls it down a little, so it starts lively).
                var col = Color.Lerp(C(0.42f, 0.72f, 0.26f), C(0.62f, 0.9f, 0.36f), n);
                col = Color.Lerp(col, C(0.36f, 0.66f, 0.24f), clover * 0.3f);
                return col;
            });
            foreach (var f in flowers)
            {
                var ff = f;
                // Wrap so the texture tiles.
                for (int wx = -1; wx <= 1; wx++)
                    for (int wy = -1; wy <= 1; wy++)
                    {
                        float fx = ff.x + wx * S, fy = ff.y + wy * S;
                        if (fx < -8 || fx > S + 8 || fy < -8 || fy > S + 8) continue;
                        p.Fill((x, y) => Circle(x, y, fx, fy, ff.z), flowerCols[(int)ff.w], Mathf.Max(0, fx - 8), Mathf.Max(0, fy - 8), Mathf.Min(S, fx + 8), Mathf.Min(S, fy + 8));
                    }
            }
            p.Save(Dir + "ground_meadow.png", false, default, true);

            // Cobbles: tileable rounded stones in warm greys and beiges with dark joints.
            const int cells = 8;
            var rnd = new System.Random(55);
            var pts = new Vector2[cells, cells];
            var tint = new float[cells, cells];
            for (int i = 0; i < cells; i++)
                for (int j = 0; j < cells; j++)
                {
                    pts[i, j] = new Vector2((i + 0.2f + (float)rnd.NextDouble() * 0.6f) / cells, (j + 0.2f + (float)rnd.NextDouble() * 0.6f) / cells);
                    tint[i, j] = (float)rnd.NextDouble();
                }
            p = new Painter(S, S, Color.gray);
            p.ForEach((x, y, c) =>
            {
                float u = x / (float)S, v = y / (float)S;
                int ci = Mathf.FloorToInt(u * cells), cj = Mathf.FloorToInt(v * cells);
                float f1 = 9f, f2 = 9f;
                float t = 0f;
                for (int di = -1; di <= 1; di++)
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        int ii = ci + di, jj = cj + dj;
                        int wi = ((ii % cells) + cells) % cells, wj = ((jj % cells) + cells) % cells;
                        var q = pts[wi, wj] + new Vector2(Mathf.Floor((float)ii / cells), Mathf.Floor((float)jj / cells));
                        float d = (new Vector2(u, v) - q).magnitude;
                        if (d < f1)
                        {
                            f2 = f1;
                            f1 = d;
                            t = tint[wi, wj];
                        }
                        else if (d < f2) f2 = d;
                    }
                float edge = (f2 - f1) * cells;
                float stone = Edge(0.04f, 0.16f, edge);
                float bevel = Edge(0.05f, 0.35f, edge);
                var sc = Color.Lerp(C(0.78f, 0.72f, 0.64f), C(0.92f, 0.86f, 0.74f), t);
                sc = Color.Lerp(sc, C(0.86f, 0.74f, 0.66f), Mathf.Max(0f, t - 0.75f) * 2f);
                float n = TileNoise(u * 24f, v * 24f, 24, 56) * 0.12f - 0.06f;
                sc = new Color(sc.r + n, sc.g + n, sc.b + n);
                sc = Color.Lerp(Dark(sc, 0.18f), Light(sc, 0.1f), bevel);
                return Color.Lerp(C(0.42f, 0.36f, 0.32f), sc, stone);
            });
            p.Save(Dir + "ground_cobble.png", false, default, true);

            // Juice-square pavers: running-bond rounded bricks in soft sand / terracotta with warm grout (calmer and a
            // little darker than the cobbles, so the bar, pads and crowd read on top of it).
            Color[] paver = { C(0.74f, 0.63f, 0.5f), C(0.79f, 0.69f, 0.56f), C(0.7f, 0.58f, 0.46f), C(0.76f, 0.65f, 0.55f) };
            p = new Painter(S, S, Color.gray);
            p.ForEach((x, y, c) =>
            {
                const float bw = 128f, bh = 64f;
                int row = Mathf.FloorToInt(y / bh);
                float off = (row % 2) * bw * 0.5f;
                int col = Mathf.FloorToInt((x + off) / bw);
                float lx = (x + off) - (col + 0.5f) * bw, ly = y - (row + 0.5f) * bh;
                float d = RoundBox(lx, ly, 0f, 0f, bw * 0.5f - 4f, bh * 0.5f - 4f, 11f);
                int wc = ((col % 4) + 4) % 4, wr = ((row % 8) + 8) % 8;
                int h = (wc * 7 + wr * 13 + wc * wr * 3) % 4;
                float u = x / (float)S, v = y / (float)S;
                var pc = paver[h];
                float n = TileNoise(u * 24f, v * 24f, 24, 81) * 0.1f - 0.05f;
                pc = new Color(pc.r + n, pc.g + n * 0.9f, pc.b + n * 0.8f);
                // Soft bevel: lit top edge, shaded bottom edge.
                float rim = Edge(-12f, 0f, d);
                pc = Color.Lerp(pc, ly > 0 ? Light(pc, 0.14f) : Dark(pc, 0.16f), rim * Mathf.Clamp01(Mathf.Abs(ly) / (bh * 0.5f) + 0.2f));
                float body = Edge(1.2f, -1.2f, d);
                return Color.Lerp(C(0.52f, 0.44f, 0.37f), pc, body);
            });
            p.Save(Dir + "ground_pavers.png", false, default, true);

            // Mulched berry soil: dark crumbly earth with straw bits.
            p = new Painter(256, 256, Color.black);
            p.ForEach((x, y, c) =>
            {
                float u = x / 256f, v = y / 256f;
                float n = TileNoise(u * 8f, v * 8f, 8, 61) * 0.6f + TileNoise(u * 32f, v * 32f, 32, 62) * 0.4f;
                return Color.Lerp(C(0.3f, 0.2f, 0.13f), C(0.46f, 0.32f, 0.2f), n);
            });
            var srnd = new System.Random(63);
            for (int i = 0; i < 70; i++)
            {
                float sx = (float)srnd.NextDouble() * 236 + 10, sy = (float)srnd.NextDouble() * 236 + 10, a = (float)srnd.NextDouble() * Mathf.PI;
                float ex = sx + Mathf.Cos(a) * 12, ey = sy + Mathf.Sin(a) * 12;
                p.Fill((x, y) => Segment(x, y, sx, sy, ex, ey, 1.3f), C(0.92f, 0.78f, 0.45f, 0.75f));
            }
            p.Save(Dir + "soil_berry.png", false, default, true);

            // Strawberry skin (wraps the berry meshes): red with rows of golden seeds.
            p = new Painter(128, 128, C(0.9f, 0.12f, 0.18f));
            p.ForEach((x, y, c) => Color.Lerp(C(0.82f, 0.08f, 0.14f), C(1f, 0.28f, 0.3f), TileNoise(x / 128f * 4f, y / 128f * 4f, 4, 90)));
            for (int row = 0; row < 8; row++)
                for (int i = 0; i < 8; i++)
                {
                    float sx = i * 16f + (row % 2) * 8f + 4f, sy = row * 16f + 8f;
                    p.Fill((x, y) => Ellipse(x, y, sx, sy, 2.2f, 3.2f), C(1f, 0.86f, 0.35f));
                }
            p.Save(Dir + "skin_strawberry.png", false, default, true);

            // Red-and-white gingham for tablecloths and the bakery awning.
            p = new Painter(64, 64, Color.white);
            p.ForEach((x, y, c) =>
            {
                bool a = (x / 16) % 2 == 0, b = (y / 16) % 2 == 0;
                return a && b ? C(0.9f, 0.3f, 0.35f) : a || b ? C(0.97f, 0.66f, 0.68f) : C(1f, 0.98f, 0.96f);
            });
            p.Save(Dir + "gingham.png", false, default, true);

            // Conveyor belt: dark rubber with raised ribs (scrolls along V).
            p = new Painter(64, 64, Color.black);
            p.ForEach((x, y, c) =>
            {
                int k = y % 16;
                return k < 3 ? C(0.46f, 0.47f, 0.5f) : k < 5 ? C(0.3f, 0.31f, 0.34f) : C(0.2f, 0.21f, 0.24f);
            });
            p.Save(Dir + "belt.png", false, default, true);
        }
    }
}
