using System;
using UnityEngine;
using static JuiceKing.EditorTools.Painter;

namespace JuiceKing.EditorTools
{
    /// <summary>Shop art (every world): the Ad Ticket and the Remove Ads icons.</summary>
    public static partial class ArtGen
    {
        public const string TicketIcon = "icon_ticket.png";
        public const string NoAdsIcon = "icon_noads.png";

        /// <summary>Regenerates only the shop icons (used by Build Everything as well).</summary>
        public static void GenerateShop() => ShopIcons();

        static void ShopIcons()
        {
            // Ad Ticket: tilted gold ticket with side notches, a coral stub behind a perforation, a play badge and a star.
            var p = new Painter(256);
            const float tilt = 14f;
            Func<float, float, float> body = (x, y) =>
                Subtract(Subtract(RoundBox(x, y, 128, 128, 110, 68, 18), Circle(x, y, 18, 128, 20)), Circle(x, y, 238, 128, 20));
            var ticket = R(body, 128, 128, tilt);
            Icon(p, ticket, C(1f, 0.76f, 0.2f), 9, 50, 210);
            p.Fill(R((x, y) => Intersect(body(x, y) + 9f, 172f - x), 128, 128, tilt), C(0.96f, 0.36f, 0.3f));
            p.Fill(R((x, y) => Intersect(Intersect(body(x, y) + 9f, 172f - x), -(y - 128f)), 128, 128, tilt), C(0.85f, 0.25f, 0.22f, 0.45f));
            for (int i = 0; i < 6; i++)
            {
                float dy = 78f + i * 20f;
                p.Fill(R((x, y) => Circle(x, y, 172, dy, 4.5f), 128, 128, tilt), C(0.5f, 0.24f, 0.1f, 0.9f));
            }
            var disc = R((x, y) => Circle(x, y, 96, 128, 40), 128, 128, tilt);
            p.Fill((x, y) => disc(x, y) - 5f, C(0.62f, 0.3f, 0.06f));
            p.Fill(disc, C(1f, 0.99f, 0.93f));
            p.Fill(R((x, y) => Polygon(x, y, new[] { new Vector2(84, 106), new Vector2(84, 150), new Vector2(122, 128) }) + 1f, 128, 128, tilt), C(0.94f, 0.33f, 0.27f));
            var star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 22f : 9.5f;
                star[i] = new Vector2(208f + Mathf.Cos(a) * r, 128f + Mathf.Sin(a) * r);
            }
            p.Fill(R((x, y) => Polygon(x, y, star) - 4f, 128, 128, tilt), C(0.55f, 0.12f, 0.1f));
            p.Fill(R((x, y) => Polygon(x, y, star), 128, 128, tilt), C(1f, 0.93f, 0.5f));
            Gloss(p, ticket, 100, 176, 70, 12, tilt, 0.45f);
            p.Save(Dir + TicketIcon, true);

            // Remove Ads: a glossy video screen with a play arrow, crossed out by a red "no" sign with a white rim.
            p = new Painter(256);
            Func<float, float, float> screen = (x, y) => RoundBox(x, y, 128, 128, 66, 48, 16);
            Icon(p, screen, C(0.26f, 0.62f, 1f), 7, 80, 176);
            p.Fill((x, y) => RoundBox(x, y, 128, 128, 52, 34, 10), C(0.16f, 0.36f, 0.8f));
            p.Fill((x, y) => Polygon(x, y, new[] { new Vector2(114, 108), new Vector2(114, 148), new Vector2(148, 128) }) - 3f, C(1f, 1f, 1f));
            Gloss(p, screen, 110, 164, 46, 9, 0f, 0.4f);
            Func<float, float, float> sign = (x, y) =>
                Union(Ring(x, y, 128, 128, 98, 24), Intersect(Segment(x, y, 60, 196, 196, 60, 12), Circle(x, y, 128, 128, 98)));
            p.FillSoft((x, y) => sign(x, y + 6f) - 9f, C(0f, 0f, 0f, 0.25f), 7f);
            p.Fill((x, y) => sign(x, y) - 9f, Outline);
            p.Fill((x, y) => sign(x, y) - 5f, C(1f, 1f, 1f));
            p.FillFn(sign, (x, y) => Color.Lerp(C(0.78f, 0.06f, 0.08f), C(1f, 0.3f, 0.26f), Mathf.InverseLerp(30, 226, y)));
            p.Fill((x, y) => Intersect(sign(x, y) + 2f, -(sign(x, y + 6f) + 2f)), C(1f, 1f, 1f, 0.4f));
            p.Save(Dir + NoAdsIcon, true);
        }
    }
}
