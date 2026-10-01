using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Premium shop popup: Golden Apple packs, Ad Ticket packs and Remove Ads in a scroll view on the red premium panel.
    /// One card per <see cref="IapCatalog"/> product; the card texts are filled at runtime by <see cref="ShopPopup"/>.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        const float ShopCardW = 276f, ShopCardH = 400f, ShopGap = 26f;
        static readonly Color ShopGold = new Color(1f, 0.84f, 0.28f);

        static ShopPopup BuildShopPopup(Transform root)
        {
            var popRoot = Stretch("ShopPopup", root);
            // Own canvas: the spinning rays and scrolling cards re-batch only the shop, not the whole HUD.
            popRoot.gameObject.AddComponent<Canvas>();
            popRoot.gameObject.AddComponent<GraphicRaycaster>();
            var dimImg = Img(popRoot, _sWhite, new Color(0.05f, 0.04f, 0.08f, 0.66f), false, true);
            dimImg.preserveAspect = false;
            var dimBtn = popRoot.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            dimBtn.transition = Selectable.Transition.None;

            var sprite = _uPanelGold ? _uPanelGold : (_uPanelPlain ? _uPanelPlain : _sPanel);
            float panelScale = PanelScale(sprite);
            // Fills the height between fixed margins (1660 tall at 1080x1920, more on tall phones: more cards visible);
            // the top margin keeps the ribbon clear of notches.
            const float winW = 1000f, marginTop = 150f, marginBottom = 110f;
            var w = UIRect("Window", popRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(winW, 1000f));
            w.anchorMin = new Vector2(0.5f, 0f);
            w.anchorMax = new Vector2(0.5f, 1f);
            w.offsetMin = new Vector2(-winW * 0.5f, marginBottom);
            w.offsetMax = new Vector2(winW * 0.5f, -marginTop);
            Sliced(w, sprite, panelScale, null, true);
            var title = Label(UIRect("Title", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(TitleX(sprite), -PlankY(sprite) * panelScale), new Vector2(480f, 100f)),
                "SHOP", 64f, Color.white, TextAlignmentOptions.Center, true);
            title.enableAutoSizing = true;
            title.fontSizeMin = 40f;
            title.fontSizeMax = 64f;

            // Balances under the title: what the player has right now.
            var appleBal = BalancePill(w, "Apples", new Vector2(-150f, -184f), _sApple ? _sApple : _sStar, new Color(1f, 0.9f, 0.45f), out var appleBalText);
            var ticketBal = BalancePill(w, "Tickets", new Vector2(150f, -184f), _sTicket ? _sTicket : _sStar, new Color(1f, 0.8f, 0.64f), out var ticketBalText);
            var status = Label(UIRect("Status", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -256f), new Vector2(820f, 48f)),
                "", 34f, new Color(0.85f, 0.35f, 0.1f), TextAlignmentOptions.Center, false);
            status.enableAutoSizing = true;
            status.fontSizeMin = 24f;
            status.fontSizeMax = 34f;
            status.gameObject.SetActive(false);

            // Scroll view inside the cream body (clear of the ribbon, the frame and the corner flowers).
            var viewport = Stretch("Viewport", w, 60f, 60f, 286f, 150f);
            Img(viewport, _sWhite, new Color(1f, 1f, 1f, 0f), false, true).preserveAspect = false;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UIRect("Content", viewport, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 100f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.sizeDelta = new Vector2(0f, 100f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 40f;

            var cards = new List<ShopCard>();
            float y = -8f;
            var applesHead = ShopSectionHeader(content, "GOLDEN APPLES", _sApple ? _sApple : _sStar, ref y);
            ShopGrid(content, IapCatalog.OfKind(IapKind.Apples), true, cards, ref y);
            var ticketsHead = ShopSectionHeader(content, "AD TICKETS", _sTicket ? _sTicket : _sStar, ref y);
            ShopGrid(content, IapCatalog.OfKind(IapKind.Tickets), false, cards, ref y);
            var adsHead = ShopSectionHeader(content, "REMOVE ADS", _sNoAds ? _sNoAds : _sAd, ref y);
            foreach (var p in IapCatalog.OfKind(IapKind.RemoveAds)) cards.Add(RemoveAdsCard(content, p, ref y));
            content.sizeDelta = new Vector2(0f, -y + 30f);

            // Restore Purchases (App Store requirement for the non-consumable Remove Ads), between the corner flowers.
            var restore = UIRect("Restore", w, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 84f), new Vector2(460f, 76f));
            var restoreImg = Img(restore, _sWhite, new Color(1f, 1f, 1f, 0f), false, true);
            restoreImg.preserveAspect = false;
            var restoreBtn = restore.gameObject.AddComponent<Button>();
            restoreBtn.targetGraphic = restoreImg;
            restore.gameObject.AddComponent<UIPress>();
            var rt = Label(Stretch("Text", restore), "<u>Restore Purchases</u>", 34f, new Color(0.55f, 0.36f, 0.24f), TextAlignmentOptions.Center, false);
            rt.richText = true;

            // "+85 GOLDEN APPLES" that floats up from a bought card (above the list).
            var gain = Label(UIRect("Gain", w, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 90f)),
                "+0", 58f, ShopGold, TextAlignmentOptions.Center, true);

            var shop = popRoot.gameObject.AddComponent<ShopPopup>();
            shop.window = w;
            shop.dim = dimImg;
            shop.dimButton = dimBtn;
            shop.closeButton = CloseHotspot(w, sprite, sprite ? sprite.name : "", panelScale);
            shop.scroll = scroll;
            shop.applesSection = applesHead;
            shop.ticketsSection = ticketsHead;
            shop.removeAdsSection = adsHead;
            shop.cards = cards.ToArray();
            shop.applesBalance = appleBalText;
            shop.ticketsBalance = ticketBalText;
            shop.applesBalancePanel = appleBal;
            shop.ticketsBalancePanel = ticketBal;
            shop.statusText = status;
            shop.gainText = gain;
            shop.restoreButton = restoreBtn;
            var blue = UIKit.Get("ribbon_blue");
            shop.popularRibbon = blue != null ? blue : _uRibbonYellow;
            shop.bestValueRibbon = _uRibbon;
            return shop;
        }

        /// <summary>Dark capsule with an icon and a number (the shop's copy of the HUD counters).</summary>
        static RectTransform BalancePill(RectTransform parent, string name, Vector2 pos, Sprite icon, Color textColor, out TextMeshProUGUI text)
        {
            var pill = UIRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), pos, new Vector2(250f, 84f));
            if (_uBtnGrey != null) Sliced(pill, _uBtnGrey, 1.1f, new Color(1f, 1f, 1f, 0.95f));
            else Sliced(pill, _sPill, 0.9f, UiDark);
            Img(UIRect("Icon", pill, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 4f), new Vector2(92f, 92f)), icon, Color.white);
            text = Label(Stretch("Value", pill, 80f, 26f, 0f, 4f), "0", 50f, textColor, TextAlignmentOptions.MidlineRight, true);
            text.enableAutoSizing = true;
            text.fontSizeMin = 28f;
            text.fontSizeMax = 50f;
            return pill;
        }

        /// <summary>Wooden sign with an icon and the section name; returns it so the shop can scroll to it.</summary>
        static RectTransform ShopSectionHeader(RectTransform content, string text, Sprite icon, ref float y)
        {
            const float h = 104f;
            var head = UIRect("Section_" + text.Replace(' ', '_'), content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(660f, h));
            Sliced(head, _uPlank ? _uPlank : _sPanel, 1.15f);
            Img(UIRect("Icon", head, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 6f), new Vector2(104f, 104f)), icon, Color.white);
            var t = Label(Stretch("Text", head, 140f, 60f, 14f, 24f), text, 50f, Color.white, TextAlignmentOptions.Center, true);
            t.enableAutoSizing = true;
            t.fontSizeMin = 30f;
            t.fontSizeMax = 50f;
            y -= h;
            return head;
        }

        /// <summary>Three cards per row; leaves room above each row for the POPULAR / BEST VALUE ribbons.</summary>
        static void ShopGrid(RectTransform content, List<IapProduct> products, bool apples, List<ShopCard> cards, ref float y)
        {
            const int cols = 3;
            const float rowTop = 52f;
            float x0 = -(cols - 1) * (ShopCardW + ShopGap) * 0.5f;
            for (int i = 0; i < products.Count; i++)
            {
                int col = i % cols;
                if (col == 0) y -= rowTop;
                var pos = new Vector2(x0 + col * (ShopCardW + ShopGap), y);
                cards.Add(PackCard(content, products[i], i, apples, pos));
                if (col == cols - 1 || i == products.Count - 1) y -= ShopCardH;
            }
            y -= 30f;
        }

        static ShopCard PackCard(RectTransform content, IapProduct p, int tier, bool apples, Vector2 pos)
        {
            var card = UIRect("Card_" + p.id, content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(ShopCardW, ShopCardH));
            // Featured packs glow gold behind the card (shown with their ribbon).
            var glow = UIRect("Glow", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ShopCardW + 70f, ShopCardH + 70f));
            var glowImg = Img(glow, _sGlow, new Color(1f, 0.8f, 0.25f, 0.85f));
            glowImg.preserveAspect = false;
            Sliced(UIRect("Back", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ShopCardW, ShopCardH)),
                _uCard ? _uCard : _sPanel, 1.5f);

            var name = Label(UIRect("Title", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(ShopCardW - 40f, 44f)),
                p.title, 32f, UiInk, TextAlignmentOptions.Center, false);
            name.enableAutoSizing = true;
            name.fontSizeMin = 22f;
            name.fontSizeMax = 32f;

            // Icon stage: soft glow, rays on featured packs, then a pile that grows with the tier.
            var stage = UIRect("Icons", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -134f), new Vector2(220f, 160f));
            Img(UIRect("Halo", stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 230f)), _sGlow,
                apples ? new Color(1f, 0.85f, 0.35f, 0.55f) : new Color(1f, 0.6f, 0.45f, 0.5f));
            // Spinning rays only behind featured packs (toggled with the badge).
            var rays = UIRect("Rays", stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 260f));
            Img(rays, _sRays, new Color(1f, 0.88f, 0.4f, 0.7f));
            rays.gameObject.AddComponent<UISpin>().speed = 14f;
            if (apples) ApplePile(stage, tier);
            else TicketFan(stage, tier);

            var amount = Label(UIRect("Amount", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -236f), new Vector2(ShopCardW - 24f, 56f)),
                "x" + p.amount, 54f, apples ? ShopGold : new Color(1f, 0.66f, 0.45f), TextAlignmentOptions.Center, true);
            amount.enableAutoSizing = true;
            amount.fontSizeMin = 34f;
            amount.fontSizeMax = 52f;
            var bonus = Label(UIRect("Bonus", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -278f), new Vector2(ShopCardW - 30f, 30f)),
                "+" + p.bonusPercent + "% BONUS", 28f, new Color(0.2f, 0.62f, 0.18f), TextAlignmentOptions.Center, false);
            bonus.enableAutoSizing = true;
            bonus.fontSizeMin = 20f;
            bonus.fontSizeMax = 28f;

            var price = PriceButton(card, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(ShopCardW - 40f, 86f), out var priceText);

            var badge = UIRect("Badge", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(ShopCardW + 10f, 72f));
            var badgeImg = Sliced(badge, _uRibbon ? _uRibbon : _sButton, 0.9f);
            var badgeText = Label(Stretch("Text", badge, 30f, 30f, 8f, 20f), "POPULAR", 34f, Color.white, TextAlignmentOptions.Center, true);
            badgeText.enableAutoSizing = true;
            badgeText.fontSizeMin = 22f;
            badgeText.fontSizeMax = 34f;
            bool featured = p.badge != IapBadge.None;
            badge.gameObject.SetActive(featured);
            glow.gameObject.SetActive(featured);
            rays.gameObject.SetActive(featured);

            return new ShopCard
            {
                productId = p.id, root = card, button = price, titleText = name, amountText = amount, bonusText = bonus, priceText = priceText,
                badge = badge.gameObject, badgeImage = badgeImg, badgeText = badgeText, highlights = new[] { glow.gameObject, rays.gameObject },
            };
        }

        static Button PriceButton(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, out TextMeshProUGUI text)
        {
            var rt = UIRect("Price", parent, anchor, new Vector2(0.5f, anchor.y), pos, size);
            var btn = AtlasButton(rt, _uBtnGreen ? _uBtnGreen : _sButton, 2.1f);
            btn.transition = Selectable.Transition.SpriteSwap;
            var grey = _uBtnGrey ? _uBtnGrey : _sButton;
            btn.spriteState = new SpriteState { disabledSprite = grey, pressedSprite = _uBtnGreen, highlightedSprite = _uBtnGreen, selectedSprite = _uBtnGreen };
            text = Label(Stretch("Text", rt, 16f, 16f, 0f, 14f), "$0.99", 46f, Color.white, TextAlignmentOptions.Center, true);
            text.enableAutoSizing = true;
            text.fontSizeMin = 26f;
            text.fontSizeMax = 46f;
            return btn;
        }

        /// <summary>1 to 6 Golden Apples stacked in a little pyramid (back row first so the front row overlaps it).</summary>
        static void ApplePile(RectTransform stage, int tier)
        {
            Vector3[][] layouts =
            {
                new[] { new Vector3(0f, 0f, 132f) },
                new[] { new Vector3(-34f, 8f, 104f), new Vector3(34f, -6f, 110f) },
                new[] { new Vector3(0f, 26f, 92f), new Vector3(-48f, -18f, 96f), new Vector3(48f, -18f, 96f) },
                new[] { new Vector3(0f, 36f, 82f), new Vector3(-60f, -22f, 84f), new Vector3(0f, -26f, 88f), new Vector3(60f, -22f, 84f) },
                new[] { new Vector3(-30f, 30f, 78f), new Vector3(30f, 30f, 78f), new Vector3(-62f, -28f, 80f), new Vector3(0f, -32f, 84f), new Vector3(62f, -28f, 80f) },
                new[] { new Vector3(0f, 60f, 70f), new Vector3(-32f, 16f, 74f), new Vector3(32f, 16f, 74f), new Vector3(-64f, -32f, 76f), new Vector3(0f, -36f, 80f), new Vector3(64f, -32f, 76f) },
            };
            var set = layouts[Mathf.Clamp(tier, 0, layouts.Length - 1)];
            for (int i = 0; i < set.Length; i++)
            {
                var a = UIRect("Apple" + i, stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(set[i].x, set[i].y), new Vector2(set[i].z, set[i].z));
                Img(a, _sApple ? _sApple : _sStar, Color.white);
                a.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1f : 1f) * (4f + i * 2f));
            }
        }

        /// <summary>1 to 6 Ad Tickets fanned out like a hand of cards.</summary>
        static void TicketFan(RectTransform stage, int tier)
        {
            int n = Mathf.Clamp(tier + 1, 1, 6);
            float size = 150f - n * 8f;
            for (int i = 0; i < n; i++)
            {
                float k = i - (n - 1) * 0.5f;
                var t = UIRect("Ticket" + i, stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.2f), new Vector2(k * 22f, -40f - Mathf.Abs(k) * 6f), new Vector2(size, size));
                Img(t, _sTicket ? _sTicket : _sStar, Color.white);
                t.localRotation = Quaternion.Euler(0f, 0f, -k * 11f);
            }
        }

        /// <summary>Full-width Remove Ads card: icon, title, two description lines and the price.</summary>
        static ShopCard RemoveAdsCard(RectTransform content, IapProduct p, ref float y)
        {
            y -= 26f;
            const float h = 240f;
            float w = 3f * ShopCardW + 2f * ShopGap;
            var card = UIRect("Card_" + p.id, content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(w, h));
            Sliced(card, _uCard ? _uCard : _sPanel, 1.5f);
            var stage = UIRect("Icon", card, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(124f, 4f), new Vector2(180f, 180f));
            Img(UIRect("Halo", stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 240f)), _sGlow, new Color(1f, 0.55f, 0.45f, 0.5f));
            Img(UIRect("Image", stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 170f)), _sNoAds ? _sNoAds : _sAd, Color.white);

            const float textX = 232f, btnW = 230f;
            float textW = w - textX - btnW - 40f;
            var title = Label(UIRect("Title", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, 52f), new Vector2(textW, 64f)),
                p.title, 52f, new Color(0.86f, 0.2f, 0.14f), TextAlignmentOptions.MidlineLeft, false);
            title.enableAutoSizing = true;
            title.fontSizeMin = 34f;
            title.fontSizeMax = 52f;
            var desc = Label(UIRect("Desc", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, -4f), new Vector2(textW, 44f)),
                "Remove forced ads permanently", 32f, UiInk, TextAlignmentOptions.MidlineLeft, false);
            desc.enableAutoSizing = true;
            desc.fontSizeMin = 22f;
            desc.fontSizeMax = 32f;
            var note = Label(UIRect("Note", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, -50f), new Vector2(textW, 36f)),
                "Optional reward ads stay available", 26f, new Color(0.5f, 0.4f, 0.32f), TextAlignmentOptions.MidlineLeft, false);
            note.enableAutoSizing = true;
            note.fontSizeMin = 18f;
            note.fontSizeMax = 26f;

            var price = PriceButton(card, new Vector2(1f, 0.5f), new Vector2(-24f - btnW * 0.5f, 0f), new Vector2(btnW, 104f), out var priceText);
            y -= h;
            return new ShopCard { productId = p.id, root = card, button = price, titleText = title, priceText = priceText };
        }
    }
}
