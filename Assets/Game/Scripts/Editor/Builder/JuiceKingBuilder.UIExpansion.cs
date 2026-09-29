using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing.EditorTools
{
    /// <summary>UI added by Expansion 1: tabbed upgrade tree, delivery card, world-complete popup, intro overlay.</summary>
    public static partial class JuiceKingBuilder
    {
        static DeliveryHUD _uiDelivery;
        static GameObject _uiNextWorld;
        static CompletionPopup _uiCompletion;
        static ExpansionIntro _uiIntro;
        static CanvasGroup _uiSafeGroup;

        // ================================================================== upgrade panel

        static void BuildUpgradePanel(RectTransform safe, int world)
        {
            var panelRoot = Stretch("UpgradePanel", safe);
            var tree = Upgrades.ForWorld(world);
            bool tabbed = world > 0;
            int rowsN = tabbed ? 4 : tree.Count;
            const float awningScale = 2f;
            const float rowH = 196f;
            float head = (_uPanelAwning ? _uPanelAwning.border.w : 118f) * awningScale;
            float tabsH = tabbed ? 128f : 0f;
            const float windowW = 1010f;
            // The awning panel's wooden frame is ~100 px wide on each side: rows and tabs live inside it.
            const float frame = 100f;
            const float innerW = windowW - 2f * frame;
            const float rowW = innerW - 12f;
            const float innerX = 7f; // the left frame is a little wider than the right
            var window = UIRect("Window", panelRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f),
                new Vector2(windowW, head + 90f + tabsH + rowsN * rowH + 60f));
            Sliced(window, _uPanelAwning ? _uPanelAwning : _sPanel, awningScale, null, true);
            var ribbon = UIRect("Title", window, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -head - 18f), new Vector2(560f, 120f));
            Sliced(ribbon, _uRibbon ? _uRibbon : _sButton, 1.3f);
            Label(Stretch("Text", ribbon, 60f, 60f, 22f, 34f), "UPGRADES", 60f, Color.white, TextAlignmentOptions.Center, true);

            UpgradeTab[] tabs = null;
            if (tabbed)
            {
                var cats = Upgrades.Categories;
                tabs = new UpgradeTab[cats.Length];
                const float gap = 6f;
                float tw = (innerW - 16f - (cats.Length - 1) * gap) / cats.Length;
                float x0 = -(cats.Length * tw + (cats.Length - 1) * gap) * 0.5f + tw * 0.5f;
                for (int i = 0; i < cats.Length; i++)
                {
                    var trt = UIRect("Tab_" + cats[i], window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(innerX + x0 + i * (tw + gap), -head - 88f), new Vector2(tw, 100f));
                    var btn = AtlasButton(trt, _uBtnCream ? _uBtnCream : _sButton, 1.2f);
                    var lab = Label(Stretch("Text", trt, 6f, 6f, 10f, 16f), cats[i], 30f, UiInk, TextAlignmentOptions.Center, false);
                    lab.enableAutoSizing = true;
                    lab.fontSizeMin = 18f;
                    lab.fontSizeMax = 30f;
                    var badge = UIRect("Badge", trt, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -10f), new Vector2(34f, 34f));
                    Img(badge, _sCircle, new Color(1f, 0.28f, 0.25f));
                    badge.gameObject.SetActive(false);
                    tabs[i] = new UpgradeTab { category = cats[i], button = btn, background = (Image)btn.targetGraphic, label = lab, badge = badge.gameObject };
                }
            }

            UpgradeIconTable(out var iconKeys, out var iconSprites);
            var rows = new UpgradeRow[rowsN];
            var greyBtn = _uBtnGrey ? _uBtnGrey : _sButton;
            for (int i = 0; i < rowsN; i++)
            {
                var def = i < tree.Count ? tree[i] : null;
                var row = UIRect("Row" + i, window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(innerX, -head - 92f - tabsH - i * rowH), new Vector2(rowW, 184f));
                Sliced(row, _uCard ? _uCard : _sPanel, 1.5f);
                const float btnW = 236f, textX = 160f;
                float levelX = rowW - 16f - btnW - 8f - 96f;
                var glow = UIRect("Glow", row, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 2f), new Vector2(160f, 160f));
                Img(glow, _sGlow, new Color(1f, 0.9f, 0.55f, 0.7f));
                var ic = UIRect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 2f), new Vector2(118f, 118f));
                Sprite icon = _sStar;
                if (def != null)
                    for (int k = 0; k < iconKeys.Length; k++)
                        if (iconKeys[k] == def.icon) icon = iconSprites[k];
                var icImg = Img(ic, icon, Color.white);
                var nm = UIRect("Name", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, 44f), new Vector2(levelX - textX - 6f, 64f));
                var nmT = Label(nm, def != null ? def.name : "", 44f, UiInk, TextAlignmentOptions.MidlineLeft, false);
                nmT.enableAutoSizing = true;
                nmT.fontSizeMin = 30f;
                nmT.fontSizeMax = 46f;
                var lv = UIRect("Level", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(levelX, 44f), new Vector2(96f, 56f));
                var lvT = Label(lv, "LV 1", 34f, new Color(0.95f, 0.5f, 0.1f), TextAlignmentOptions.MidlineLeft, false);
                var st = UIRect("Stat", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, -8f), new Vector2(rowW - textX - 16f - btnW - 10f, 46f));
                var stT = Label(st, "", 31f, new Color(0.45f, 0.36f, 0.3f), TextAlignmentOptions.MidlineLeft, false);
                stT.richText = true;
                stT.enableAutoSizing = true;
                stT.fontSizeMin = 22f;
                stT.fontSizeMax = 31f;
                var pips = new Image[Balance.MaxUpgradeLevel];
                for (int p = 0; p < pips.Length; p++)
                {
                    var pip = UIRect("Pip" + p, row, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(textX + 16f + p * 40f, -56f), new Vector2(30f, 30f));
                    pips[p] = Img(pip, _sCircle, Color.white);
                }
                var btn = UIRect("Buy", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 2f), new Vector2(btnW, 124f));
                var button = AtlasButton(btn, _uBtnGreen ? _uBtnGreen : _sButton, 2.2f);
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { disabledSprite = greyBtn, pressedSprite = _uBtnGreen, highlightedSprite = _uBtnGreen, selectedSprite = _uBtnGreen };
                var coin = UIRect("Coin", btn, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(48f, 6f), new Vector2(66f, 66f));
                Img(coin, _uCoin ? _uCoin : _sCoin, Color.white);
                var cost = Label(Stretch("Cost", btn, 82f, 14f, 0f, 16f), "$0", 48f, Color.white, TextAlignmentOptions.Center, true);
                cost.enableAutoSizing = true;
                cost.fontSizeMin = 28f;
                cost.fontSizeMax = 48f;

                rows[i] = new UpgradeRow
                {
                    root = row.gameObject, icon = icImg, nameText = nmT, levelText = lvT, costText = cost, statText = stT, button = button, pips = pips
                };
            }

            var closeUp = CloseHotspot(window, _uPanelAwning, "panel_awning", awningScale);
            var up = panelRoot.gameObject.AddComponent<UpgradePanel>();
            up.window = window;
            up.rows = rows;
            up.tabs = tabs;
            up.closeButton = closeUp;
            up.pipOn = new Color(1f, 0.72f, 0.12f);
            up.pipOff = new Color(0.84f, 0.76f, 0.66f);
            up.tabOn = Color.white;
            up.tabOff = new Color(0.78f, 0.72f, 0.66f);
        }

        // ================================================================== delivery button + popup

        /// <summary>Parcel-box button on the left edge; the order details live in the popup it opens.</summary>
        static DeliveryHUD BuildDeliveryHUD(RectTransform safe)
        {
            // The component sits on an always-active holder: the button itself is hidden until the bay opens.
            var holder = Stretch("Delivery", safe);
            var rt = UIRect("DeliveryButton", holder, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(96f, -720f), new Vector2(150f, 150f));
            var btn = AtlasButton(rt, _uSqOrange ? _uSqOrange : (_uSqWood ? _uSqWood : _sButton), 1.35f);
            var ic = UIRect("Icon", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(118f, 118f));
            Img(ic, _sParcel ? _sParcel : _sTruck, Color.white);
            var lab = UIRect("Label", rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(230f, 50f));
            Label(lab, "DELIVERY", 32f, Color.white, TextAlignmentOptions.Center, true);
            var badge = UIRect("Badge", rt, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), new Vector2(58f, 58f));
            Img(badge, _sCircle, new Color(1f, 0.25f, 0.22f));
            Label(Stretch("Text", badge, 0f, 0f, 2f, 6f), "!", 44f, Color.white, TextAlignmentOptions.Center, true);
            var hud = holder.gameObject.AddComponent<DeliveryHUD>();
            hud.root = rt;
            hud.button = btn;
            hud.icon = ic;
            hud.badge = badge.gameObject;
            return hud;
        }

        static DeliveryPopup BuildDeliveryPopup(Transform root)
        {
            const float panelScale = 2.5f;
            var popRoot = Stretch("DeliveryPopup", root);
            var dimImg = Img(popRoot, _sWhite, new Color(0.05f, 0.04f, 0.08f, 0.5f), false, true);
            dimImg.preserveAspect = false;
            var dim = popRoot.gameObject.AddComponent<Button>();
            dim.targetGraphic = dimImg;
            dim.transition = Selectable.Transition.None;

            var sprite = _uPanelPlain ? _uPanelPlain : _sPanel;
            var w = UIRect("Window", popRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(820f, 920f));
            Sliced(w, sprite, panelScale, null, true);
            float plankY = -48f * panelScale;
            Label(UIRect("Title", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, plankY), new Vector2(460f, 100f)),
                "DELIVERY", 56f, Color.white, TextAlignmentOptions.Center, true);
            var status = Label(UIRect("Status", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -232f), new Vector2(660f, 64f)),
                "", 36f, UiInk, TextAlignmentOptions.Center, false);
            status.enableAutoSizing = true;
            status.fontSizeMin = 24f;
            status.fontSizeMax = 36f;

            // Current order.
            var order = UIRect("Order", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(680f, 420f));
            Sliced(order, _uCard ? _uCard : _sPanel, 1.5f);
            var client = Label(UIRect("Client", order, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(600f, 70f)),
                "CLIENT", 52f, new Color(0.95f, 0.45f, 0.1f), TextAlignmentOptions.Center, false);
            client.enableAutoSizing = true;
            client.fontSizeMin = 30f;
            client.fontSizeMax = 52f;
            var jIcon = UIRect("Juice", order, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 30f), new Vector2(150f, 150f));
            var jImg = Img(jIcon, _sJuiceIcons[3] ? _sJuiceIcons[3] : _sJuice, Color.white);
            var count = Label(UIRect("Count", order, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, 30f), new Vector2(440f, 90f)),
                "0 / 8", 60f, UiInk, TextAlignmentOptions.MidlineLeft, false);
            count.enableAutoSizing = true;
            count.fontSizeMin = 30f;
            count.fontSizeMax = 60f;
            var barBg = UIRect("Bar", order, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(580f, 44f));
            Sliced(barBg, _uBtnGrey ? _uBtnGrey : _sPill, 0.55f);
            barBg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var fillImg = Img(Stretch("Fill", barBg, 6f, 6f, 6f, 6f), _sWhite, new Color(0.4f, 0.9f, 0.35f));
            fillImg.preserveAspect = false;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            var coin = UIRect("Coin", order, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(84f, 70f), new Vector2(72f, 72f));
            Img(coin, _uCoin ? _uCoin : _sCoin, Color.white);
            var reward = Label(UIRect("Reward", order, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(130f, 70f), new Vector2(260f, 70f)),
                "$0", 50f, new Color(1f, 0.82f, 0.2f), TextAlignmentOptions.MidlineLeft, true);
            var timer = Label(UIRect("Timer", order, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-40f, 70f), new Vector2(260f, 60f)),
                "", 38f, UiInk, TextAlignmentOptions.MidlineRight, false);
            timer.enableAutoSizing = true;
            timer.fontSizeMin = 24f;
            timer.fontSizeMax = 38f;

            // Between trucks.
            var wait = UIRect("Wait", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(680f, 420f));
            Sliced(wait, _uCard ? _uCard : _sPanel, 1.5f);
            var truck = UIRect("Truck", wait, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(210f, 210f));
            Img(truck, _sTruck, Color.white);
            var waitT = Label(UIRect("Text", wait, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(600f, 80f)),
                "Next truck in 0:20", 50f, UiInk, TextAlignmentOptions.Center, false);
            waitT.enableAutoSizing = true;
            waitT.fontSizeMin = 30f;
            waitT.fontSizeMax = 50f;
            wait.gameObject.SetActive(false);

            var go = UIRect("Go", w, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(560f, 140f));
            var goBtn = AtlasButton(go, _uBtnGreen ? _uBtnGreen : _sButton, 2.6f, false);
            Label(Stretch("Text", go, 30f, 30f, 0f, 20f), "SHOW ME THE BAY", 50f, Color.white, TextAlignmentOptions.Center, true).enableAutoSizing = true;

            var pop = popRoot.gameObject.AddComponent<DeliveryPopup>();
            pop.window = w;
            pop.dimButton = dim;
            pop.closeButton = CloseHotspot(w, sprite, "panel_plain", panelScale);
            pop.goButton = goBtn;
            pop.statusText = status;
            pop.orderGroup = order.gameObject;
            pop.clientText = client;
            pop.juiceIcon = jImg;
            pop.countText = count;
            pop.progressFill = fillImg;
            pop.rewardText = reward;
            pop.timerText = timer;
            pop.waitGroup = wait.gameObject;
            pop.waitText = waitT;
            return pop;
        }

        // ================================================================== world complete

        static GameObject BuildNextWorldButton(RectTransform safe)
        {
            var rt = UIRect("NextWorld", safe, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(104f, -420f), new Vector2(170f, 170f));
            AtlasButton(rt, _uSqGreen ? _uSqGreen : _sButton, 1.35f);
            var ic = UIRect("Icon", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(120f, 120f));
            Img(ic, _sFruit[3] ? _sFruit[3] : _sStar, Color.white);
            var lab = UIRect("Label", rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(260f, 50f));
            Label(lab, "NEW WORLD", 34f, Color.white, TextAlignmentOptions.Center, true);
            rt.gameObject.AddComponent<UIPress>();
            return rt.gameObject;
        }

        static CompletionPopup BuildCompletionPopup(Transform root)
        {
            var popRoot = Stretch("CompletionPopup", root);
            var dim = Img(popRoot, _sWhite, new Color(0.05f, 0.04f, 0.1f, 0.72f), false, true);
            dim.preserveAspect = false;
            const float panelScale = 2.5f;
            var popSprite = _uPanelGold ? _uPanelGold : _uPanelPlain;
            var w = UIRect("Window", popRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(900f, 1380f));
            Sliced(w, popSprite ? popSprite : _sPanel, panelScale, null, true);
            float plankY = -48f * panelScale;
            var title = Label(UIRect("Title", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-20f, plankY), new Vector2(520f, 110f)),
                "JUICE KING!", 66f, Color.white, TextAlignmentOptions.Center, true);
            title.enableAutoSizing = true;
            title.fontSizeMin = 40f;
            title.fontSizeMax = 66f;

            var rays = UIRect("Rays", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(600f, 600f));
            Img(rays, _sRays, new Color(1f, 0.68f, 0.2f, 0.95f));
            var crown = UIRect("Crown", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(260f, 260f));
            Img(crown, _uCrown ? _uCrown : _sCrown, Color.white);
            var sub = Label(UIRect("Sub", w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -505f), new Vector2(760f, 70f)),
                "You built the ultimate juice shop!", 42f, UiInk, TextAlignmentOptions.Center, false);
            sub.enableAutoSizing = true;
            sub.fontSizeMin = 28f;
            sub.fontSizeMax = 42f;

            string[] names = { "Total earnings", "Juice produced", "Fruit harvested", "Customers served", "Expansion unlocked" };
            Sprite[] icons = { _uCoins ? _uCoins : _sCoin, _sJuice, _sFruit[0], _uWaiter ? _uWaiter : _sHeart, _sFruit[3] ? _sFruit[3] : _sStar };
            var values = new TextMeshProUGUI[names.Length];
            var rowsRt = new RectTransform[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var row = UIRect("Stat" + i, w, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -600f - i * 104f), new Vector2(760f, 94f));
                Sliced(row, _uCard ? _uCard : _sPanel, 1.1f);
                var ic = UIRect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 2f), new Vector2(78f, 78f));
                Img(ic, icons[i], Color.white);
                var nameT = Label(UIRect("Name", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, 0f), new Vector2(330f, 60f)),
                    names[i], 36f, UiInk, TextAlignmentOptions.MidlineLeft, false);
                nameT.enableAutoSizing = true;
                nameT.fontSizeMin = 24f;
                nameT.fontSizeMax = 36f;
                var v = Label(UIRect("Value", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(270f, 64f)),
                    "0", 44f, new Color(0.95f, 0.5f, 0.1f), TextAlignmentOptions.MidlineRight, false);
                v.enableAutoSizing = true;
                v.fontSizeMin = 26f;
                v.fontSizeMax = 44f;
                values[i] = v;
                rowsRt[i] = row;
            }

            var enter = UIRect("Enter", w, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(700f, 160f));
            var enterBtn = AtlasButton(enter, _uBtnGreen ? _uBtnGreen : _sButton, 2.7f, false);
            var enterT = Label(Stretch("Text", enter, 40f, 40f, 0f, 24f), "ENTER TROPICAL FARM", 54f, Color.white, TextAlignmentOptions.Center, true);
            enterT.enableAutoSizing = true;
            enterT.fontSizeMin = 34f;
            enterT.fontSizeMax = 54f;
            var stay = UIRect("Stay", w, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(460f, 86f));
            var stayImg = Img(stay, _sWhite, new Color(1f, 1f, 1f, 0f), false, true);
            var stayBtn = stay.gameObject.AddComponent<Button>();
            stayBtn.targetGraphic = stayImg;
            Label(Stretch("Text", stay), "Stay a bit longer", 40f, new Color(0.55f, 0.42f, 0.34f), TextAlignmentOptions.Center, false);

            var cp = popRoot.gameObject.AddComponent<CompletionPopup>();
            cp.closeButton = CloseHotspot(w, popSprite, _uPanelGold ? "panel_gold" : "panel_plain", panelScale);
            cp.window = w;
            cp.dim = dim;
            cp.crown = crown;
            cp.rays = rays;
            cp.titleText = title;
            cp.subText = sub;
            cp.statValues = values;
            cp.statRows = rowsRt;
            cp.enterButton = enterBtn;
            cp.enterText = enterT;
            cp.stayButton = stayBtn;
            return cp;
        }

        // ================================================================== intro

        static ExpansionIntro BuildIntroOverlay(Transform root)
        {
            var introRoot = Stretch("Intro", root);
            var ink = new Color(0.04f, 0.03f, 0.06f, 0.92f);
            var top = UIRect("TopBar", introRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(3000f, 190f));
            Img(top, _sWhite, ink).preserveAspect = false;
            var bottom = UIRect("BottomBar", introRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3000f, 190f));
            Img(bottom, _sWhite, ink).preserveAspect = false;

            var titleGroup = UIRect("TitleGroup", introRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(980f, 420f));
            var ribbon = UIRect("Ribbon", titleGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(700f, 150f));
            Sliced(ribbon, _uRibbonYellow ? _uRibbonYellow : (_uRibbon ? _uRibbon : _sButton), 1.4f);
            var sub = Label(Stretch("Text", ribbon, 60f, 60f, 26f, 40f), "TROPICAL FARM", 62f, Color.white, TextAlignmentOptions.Center, true);
            var title = Label(UIRect("Title", titleGroup, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(960f, 240f)),
                "Welcome to the Tropical Juice Empire.", 72f, Color.white, TextAlignmentOptions.Center, true);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.enableAutoSizing = true;
            title.fontSizeMin = 44f;
            title.fontSizeMax = 76f;

            var cap = UIRect("Caption", introRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(900f, 150f));
            Sliced(cap, _uPlank ? _uPlank : _sPanel, 1.4f);
            var capT = Label(Stretch("Text", cap, 90f, 90f, 22f, 32f), "", 46f, Color.white, TextAlignmentOptions.Center, true);
            capT.enableAutoSizing = true;
            capT.fontSizeMin = 28f;
            capT.fontSizeMax = 46f;

            var skip = UIRect("Skip", introRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -210f), new Vector2(220f, 96f));
            var skipBtn = AtlasButton(skip, _uBtnCream ? _uBtnCream : _sButton, 1.4f);
            Label(Stretch("Text", skip, 10f, 10f, 8f, 16f), "SKIP >", 40f, UiInk, TextAlignmentOptions.Center, false);

            var intro = introRoot.gameObject.AddComponent<ExpansionIntro>();
            intro.hud = _uiSafeGroup;
            intro.topBar = top;
            intro.bottomBar = bottom;
            intro.titleGroup = titleGroup;
            intro.titleText = title;
            intro.subtitleText = sub;
            intro.captionGroup = cap;
            intro.captionText = capT;
            intro.skipButton = skipBtn;
            return intro;
        }
    }
}
