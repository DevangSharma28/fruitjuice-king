using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// HUD and popups, skinned with sprites cut from the UI atlases (see UIKit): wood-and-leaf panels, glossy buttons, cartoon icons.
    /// Layout is authored at 1080x1920 portrait; everything except full-screen overlays lives under a SafeArea root.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        static readonly Color UiInk = new Color(0.36f, 0.22f, 0.14f);
        static RectTransform _edgeArrow, _edgeArea;
        static readonly Color UiDark = new Color(0.1f, 0.09f, 0.16f, 0.62f);

        static RectTransform UIRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        static Image Img(RectTransform rt, Sprite s, Color c, bool sliced = false, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = !sliced;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>9-sliced image whose borders are drawn <paramref name="borderScale"/>x larger than the source pixels.</summary>
        static Image Sliced(RectTransform rt, Sprite s, float borderScale, Color? c = null, bool raycast = false)
        {
            var img = Img(rt, s, c ?? Color.white, true, raycast);
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, borderScale);
            return img;
        }

        static TextMeshProUGUI Label(RectTransform rt, string text, float size, Color c, TextAlignmentOptions align, bool outline)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = B.Font;
            if (outline) t.fontSharedMaterial = B.FontOutlineMat;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>Glossy atlas button (blank, 9-sliced) with a disabled look and a squish on press.</summary>
        static Button AtlasButton(RectTransform rt, Sprite s, float borderScale = 2.6f, bool press = true)
        {
            var img = Sliced(rt, s, borderScale, null, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.disabledColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            b.colors = cb;
            if (press) rt.gameObject.AddComponent<UIPress>();
            return b;
        }

        static Button IconButton(RectTransform rt, Sprite s)
        {
            var img = Img(rt, s, Color.white, false, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            rt.gameObject.AddComponent<UIPress>();
            return b;
        }

        /// <summary>Small red "AD" tag with a play glyph.</summary>
        static GameObject AdBadge(Transform parent, Vector2 pos, float scale = 1f)
        {
            if (_uBadgeAd != null)
            {
                var b = UIRect("AdBadge", parent, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), pos, new Vector2(100f, 76f) * scale);
                Img(b, _uBadgeAd, Color.white);
                b.localRotation = Quaternion.Euler(0f, 0f, -8f);
                return b.gameObject;
            }
            var rt = UIRect("AdBadge", parent, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), pos, new Vector2(96f, 56f) * scale);
            Sliced(rt, _uBtnRed ? _uBtnRed : _sPill, 1.1f * scale);
            var play = UIRect("Play", rt, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f * scale, 2f), new Vector2(26f, 26f) * scale);
            Img(play, _sPlay, Color.white);
            Label(Stretch("Text", rt, 38f * scale, 6f * scale, 0f, 4f), "AD", 30f * scale, Color.white, TextAlignmentOptions.Center, true);
            return rt.gameObject;
        }

        /// <summary>
        /// Hit area over a panel's painted X. The X sits inside the fixed-size top-right corner of the 9-slice, so its offset
        /// from that corner is the source offset times the border scale. Call after the panel's children are built so it is on top.
        /// </summary>
        static Button CloseHotspot(RectTransform panel, Sprite sprite, string spriteName, float borderScale)
        {
            if (!UIKit.HasCloseButton(spriteName) && _uX != null)
            {
                // No X painted on this panel: a real round X button on the top-right corner.
                var spot = CloseSpot(spriteName) * borderScale;
                var xr = UIRect("CloseX", panel, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-spot.x, -spot.y), new Vector2(118f, 118f));
                var xi = Img(xr, _uX, Color.white, false, true);
                var xb = xr.gameObject.AddComponent<Button>();
                xb.targetGraphic = xi;
                xr.gameObject.AddComponent<UIPress>();
                xr.SetAsLastSibling();
                return xb;
            }
            var uv = UIKit.CloseUV(spriteName);
            float w = sprite != null ? sprite.rect.width : 100f, h = sprite != null ? sprite.rect.height : 100f;
            var fromCorner = new Vector2((1f - uv.x) * w, (1f - uv.y) * h) * borderScale;
            var rt = UIRect("CloseX", panel, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-fromCorner.x, -fromCorner.y), new Vector2(140f, 140f));
            var img = Img(rt, _sWhite, new Color(1f, 1f, 1f, 0f), false, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            rt.SetAsLastSibling();
            return b;
        }

        /// <summary>Source-pixel distance from a popup panel's top edge to the centre of its title plank / ribbon.</summary>
        static float PlankY(Sprite s)
        {
            switch (s != null ? s.name : "")
            {
                case "b_panel_wood": return 54f;
                case "b_panel_fox": return 98f;
                case "p_red": return 34f;
                case "p_plank": return 33f;
                case "p_rope": return 64f;
                case "p_gold": return 43f;
                default: return 48f;
            }
        }

        /// <summary>Horizontal title offset: the old gold / plain panels' plank sits a little left of centre.</summary>
        static float TitleX(Sprite s) => s != null && s.name.StartsWith("p_") ? 0f : -20f;

        /// <summary>Atlas5 panels are drawn at 2x source size (hi-res art); the older atlases at 2.5x.</summary>
        static float PanelScale(Sprite s) => s != null && s.name.StartsWith("p_") ? 2f : 2.5f;

        /// <summary>Where a panel without a painted X gets its close button: source px in from its top-right corner.</summary>
        static Vector2 CloseSpot(string panel)
        {
            switch (panel)
            {
                case "p_awning": return new Vector2(46f, 46f);
                case "p_red": return new Vector2(34f, 36f);
                case "p_plank": return new Vector2(30f, 32f);
                case "p_rope": return new Vector2(28f, 62f);
                case "p_gold": return new Vector2(30f, 36f);
                default: return new Vector2(24f, 24f);
            }
        }

        /// <summary>
        /// World 2 dresses its UI in the Berry Blast atlas (Atlas4): panels, ribbon, buttons and the video icon are swapped
        /// for the berry versions while that world's UI is built, then restored.
        /// </summary>
        static Sprite[] SwapUITheme(bool berry)
        {
            Sprite[] saved = { _uPanelPlain, _uPanelGold, _uPanelAwning, _uRibbon, _uBtnGreen, _uBtnYellow, _uBtnOrange, _uBtnBlue, _uBtnRed, _uBtnGrey, _uBtnCream, _uWatchAd };
            if (!berry) return saved;
            Sprite Pick(string n, Sprite fallback)
            {
                var s = UIKit.Get(n);
                return s != null ? s : fallback;
            }
            _uRibbon = Pick("b_ribbon_red", _uRibbon);
            _uBtnGreen = Pick("b_btn_green", _uBtnGreen);
            _uBtnYellow = Pick("b_btn_yellow", _uBtnYellow);
            _uBtnOrange = Pick("b_btn_orange", _uBtnOrange);
            _uBtnBlue = Pick("b_btn_blue", _uBtnBlue);
            _uBtnRed = Pick("b_btn_red", _uBtnRed);
            _uBtnGrey = Pick("b_btn_dark", _uBtnGrey);
            _uBtnCream = Pick("b_btn_cream", _uBtnCream);
            _uWatchAd = Pick("b_icon_videoad", _uWatchAd);
            return saved;
        }

        static void RestoreUITheme(Sprite[] s)
        {
            _uPanelPlain = s[0]; _uPanelGold = s[1]; _uPanelAwning = s[2]; _uRibbon = s[3]; _uBtnGreen = s[4]; _uBtnYellow = s[5];
            _uBtnOrange = s[6]; _uBtnBlue = s[7]; _uBtnRed = s[8]; _uBtnGrey = s[9]; _uBtnCream = s[10]; _uWatchAd = s[11];
        }

        static void BuildUI(Transform systems, out HUD hud, int world = 0)
        {
            var saved = SwapUITheme(world >= 2);
            try { BuildUICore(systems, out hud, world); }
            finally { RestoreUITheme(saved); }
        }

        static void BuildUICore(Transform systems, out HUD hud, int world)
        {
            var canvasGo = new GameObject("UI", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = false;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Expand: the canvas is always at least 1080x1920, so the layout fits tall phones (wider than 16:9 aspect
            // gets extra height) and tablets (extra width) without anything overlapping.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(systems, false);

            // ---------------- joystick (full screen, under everything)
            var area = Stretch("Joystick", root);
            var baseRt = UIRect("Base", area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(290f, 290f));
            Img(baseRt, _sRing, new Color(1f, 1f, 1f, 0.9f));
            var group = baseRt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var knob = UIRect("Knob", baseRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            Img(knob, _sCircleBtn, new Color(1f, 1f, 1f, 0.95f));
            var js = area.gameObject.AddComponent<InputJoystick>();
            js.area = area;
            js.baseRect = baseRt;
            js.knob = knob;
            js.group = group;
            js.radius = 120f;

            // ---------------- safe area root for all HUD
            var safe = Stretch("Safe", root);
            safe.gameObject.AddComponent<SafeArea>();
            // Lets cinematics fade the whole HUD out and back in.
            _uiSafeGroup = safe.gameObject.AddComponent<CanvasGroup>();

            // World progress (top centre, the player's main goal): crown badge, world name, big gold bar with the percentage.
            // Layout across the 1080 px top bar: money 16-286 | progress 300-740 | golden apples 756-932 | settings 941-1059.
            var prog = UIRect("Progress", safe, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-20f, -26f), new Vector2(440f, 132f));
            if (_uBtnGrey != null) Sliced(prog, _uBtnGrey, 1.2f, new Color(1f, 1f, 1f, 0.94f));
            else Sliced(prog, _sPill, 0.9f, UiDark);
            var caption = Label(UIRect("World", prog, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -12f), new Vector2(286f, 44f)),
                world == 0 ? "JUICE FARM" : world == 1 ? "TROPICAL FARM" : "BERRY BLAST", 32f, new Color(1f, 0.86f, 0.35f), TextAlignmentOptions.MidlineLeft, true);
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 22f;
            caption.fontSizeMax = 32f;
            var barBg = UIRect("Bar", prog, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(126f, 20f), new Vector2(294f, 58f));
            Img(barBg, _sPill, new Color(0.05f, 0.04f, 0.06f, 0.7f), true).pixelsPerUnitMultiplier = 1.4f;
            barBg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var fillRt = Stretch("Fill", barBg, 5f, 5f, 5f, 5f);
            var fill = Img(fillRt, _sWhite, new Color(1f, 0.74f, 0.16f));
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.3f;
            var glossRt = Stretch("Gloss", fillRt, 0f, 0f, 0f, 26f);
            var gloss = Img(glossRt, _sWhite, new Color(1f, 0.92f, 0.55f, 0.55f));
            gloss.preserveAspect = false;
            gloss.type = Image.Type.Filled;
            gloss.fillMethod = Image.FillMethod.Horizontal;
            gloss.fillAmount = 0.3f;
            fillRt.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var shine = UIRect("Shine", fillRt, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50f, 0f), new Vector2(60f, 110f));
            Img(shine, _sShine, new Color(1f, 1f, 1f, 0.9f)).raycastTarget = false;
            shine.gameObject.SetActive(false);
            var progText = Label(Stretch("Text", barBg, 0f, 0f, 0f, 3f), "0%", 40f, Color.white, TextAlignmentOptions.Center, true);
            var crown = UIRect("Crown", prog, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 6f), new Vector2(132f, 132f));
            Img(crown, _uCrown ? _uCrown : _sCrown, Color.white);

            // Money (top left): coin overlapping a dark capsule.
            var money = UIRect("Money", safe, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -44f), new Vector2(270f, 94f));
            if (_uBtnGrey != null) Sliced(money, _uBtnGrey, 1.1f, new Color(1f, 1f, 1f, 0.92f));
            else Sliced(money, _sPill, 0.9f, UiDark);
            var mIcon = UIRect("Icon", money, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 2f), new Vector2(104f, 104f));
            Img(mIcon, _uCoin ? _uCoin : _sCoin, Color.white);
            var mText = Label(Stretch("Value", money, 100f, 22f, 0f, 4f), "0", 56f, Color.white, TextAlignmentOptions.MidlineRight, true);
            mText.enableAutoSizing = true;
            mText.fontSizeMin = 32f;
            mText.fontSizeMax = 56f;

            // Golden Apples (premium): a gold capsule next to the settings gear, shared by every world.
            var apples = UIRect("Apples", safe, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-148f, -44f), new Vector2(176f, 94f));
            if (_uBtnGrey != null) Sliced(apples, _uBtnGrey, 1.1f, new Color(1f, 0.97f, 0.85f, 0.95f));
            else Sliced(apples, _sPill, 0.9f, UiDark);
            var appleIcon = UIRect("Icon", apples, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 4f), new Vector2(96f, 96f));
            Img(appleIcon, _sApple ? _sApple : _sStar, Color.white);
            var appleText = Label(Stretch("Value", apples, 76f, 18f, 0f, 4f), "5", 52f, new Color(1f, 0.9f, 0.45f), TextAlignmentOptions.MidlineRight, true);
            appleText.enableAutoSizing = true;
            appleText.fontSizeMin = 28f;
            appleText.fontSizeMax = 52f;

            // Settings (top right).
            var gear = UIRect("Settings", safe, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-80f, -96f), new Vector2(118f, 118f));
            var gearBtn = AtlasButton(gear, _uSqWood ? _uSqWood : _sCircleBtn, 1.1f);
            var gIc = UIRect("Icon", gear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(78f, 78f));
            Img(gIc, _uGear ? _uGear : _sStar, Color.white);

            // Objective on a wooden plank.
            var obj = UIRect("Objective", safe, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(940f, 136f));
            Sliced(obj, _uPlank ? _uPlank : _sPanel, 1.4f);
            var objText = Label(Stretch("Text", obj, 110f, 110f, 24f, 34f), "", 44f, Color.white, TextAlignmentOptions.Center, true);
            objText.enableAutoSizing = true;
            objText.fontSizeMin = 28f;
            objText.fontSizeMax = 44f;

            // ---------------- boosts (right column)
            var boostRoot = UIRect("Boosts", safe, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -330f), new Vector2(190f, 700f));
            BoostButtonKind[] kinds = { BoostButtonKind.Cash2x, BoostButtonKind.Turbo, BoostButtonKind.FreeCash };
            Sprite[] bIcons = { _uCoins ? _uCoins : _sCash2x, _uRocket ? _uRocket : _sTurbo, _uMoneyBag ? _uMoneyBag : _sMoneyBag };
            // Every boost sits on the same brown wooden tile.
            var bBack = _uSqWood ? _uSqWood : (_uSqOrange ? _uSqOrange : _uBtnYellow);
            var boostButtons = new BoostButton[3];
            for (int i = 0; i < 3; i++)
            {
                var brt = UIRect("Boost" + i, boostRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -86f - i * 206f), new Vector2(150f, 150f));
                var btn = AtlasButton(brt, bBack ? bBack : _sButton, 1.35f);
                var ring = UIRect("Ring", brt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176f, 176f));
                var ringImg = Img(ring, _sRingThick, new Color(1f, 1f, 1f, 0.95f));
                ringImg.type = Image.Type.Filled;
                ringImg.fillMethod = Image.FillMethod.Radial360;
                ringImg.fillOrigin = (int)Image.Origin360.Top;
                ringImg.fillClockwise = false;
                var ic = UIRect("Icon", brt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(100f, 100f));
                var icImg = Img(ic, bIcons[i], Color.white);
                var lab = UIRect("Label", brt, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(230f, 50f));
                var labT = Label(lab, "", 34f, Color.white, TextAlignmentOptions.Center, true);
                var badge = AdBadge(brt, new Vector2(-10f, -8f), 0.9f);
                boostButtons[i] = new BoostButton
                {
                    kind = kinds[i], button = btn, rect = brt, icon = icImg, ring = ringImg, label = labT, adBadge = badge
                };
            }

            // ---------------- unlock assist chip
            var assist = UIRect("Assist", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(580f, 150f));
            var assistBtn = AtlasButton(assist, _uBtnGreen ? _uBtnGreen : _sButton, 2.6f, false);
            var aPlay = UIRect("Icon", assist, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 6f), new Vector2(96f, 96f));
            Img(aPlay, _uWatchAd ? _uWatchAd : _sAd, Color.white);
            var aText = Label(Stretch("Text", assist, 140f, 30f, 0f, 18f), "FINISH", 52f, Color.white, TextAlignmentOptions.Center, true);

            // BoostBar lives on the canvas because it hides the boost column until the tutorial reaches the first sale.
            var bar = canvasGo.AddComponent<BoostBar>();
            bar.root = boostRoot;
            bar.buttons = boostButtons;
            bar.cashIcon = _uHeroCoins ? _uHeroCoins : _sCash2x;
            bar.turboIcon = _uLightning ? _uLightning : _sTurbo;
            bar.freeCashIcon = _uHeroMoneyBag ? _uHeroMoneyBag : _sMoneyBag;
            bar.unlockIcon = _uStar ? _uStar : _sStar;
            bar.assistButton = assistBtn;
            bar.assistRect = assist;
            bar.assistText = aText;

            hud = canvasGo.AddComponent<HUD>();
            hud.moneyText = mText;
            hud.moneyIcon = mIcon;
            hud.moneyPanel = money;
            hud.objectivePanel = obj;
            hud.objectiveText = objText;
            hud.progressFill = fill;
            hud.progressGloss = gloss;
            hud.progressText = progText;
            hud.progressPanel = prog;
            hud.applesPanel = apples;
            hud.applesIcon = appleIcon;
            hud.applesText = appleText;
            hud.progressShine = shine;

            // ---------------- unlock banner (red ribbon)
            var bannerRoot = Stretch("UnlockBanner", safe);
            var banner = UIRect("Banner", bannerRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-30f, -356f), new Vector2(720f, 190f));
            Sliced(banner, _uRibbon ? _uRibbon : _sPanel, 1.5f);
            var newTag = UIRect("New", banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(300f, 70f));
            Label(newTag, "NEW!", 50f, new Color(1f, 0.95f, 0.5f), TextAlignmentOptions.Center, true);
            var bIcon = UIRect("Icon", banner, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, -6f), new Vector2(118f, 118f));
            var bIconImg = Img(bIcon, _uStar ? _uStar : _sStar, Color.white);
            var bTitle = Label(Stretch("Title", banner, 220f, 110f, 40f, 44f), "Unlocked", 54f, Color.white, TextAlignmentOptions.MidlineLeft, true);
            bTitle.enableAutoSizing = true;
            bTitle.fontSizeMin = 32f;
            bTitle.fontSizeMax = 54f;
            // Toast (helper warnings etc.) under the objective.
            var toast = UIRect("Toast", safe, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -330f), new Vector2(660f, 120f));
            Sliced(toast, _uNote ? _uNote : _sPanel, 1.3f);
            var tIcon = UIRect("Icon", toast, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(74f, 4f), new Vector2(92f, 92f));
            var tIconImg = Img(tIcon, _sFruit[0], Color.white);
            var tWarn = UIRect("Warn", toast, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-66f, 4f), new Vector2(80f, 72f));
            Img(tWarn, _uWarning ? _uWarning : _sStar, Color.white);
            var tText = Label(Stretch("Text", toast, 132f, 118f, 16f, 22f), "", 40f, UiInk, TextAlignmentOptions.Center, false);
            tText.enableAutoSizing = true;
            tText.fontSizeMin = 26f;
            tText.fontSizeMax = 40f;
            hud.toastPanel = toast;
            hud.toastText = tText;
            hud.toastIcon = tIconImg;

            // Off-screen target indicator (rotated to point at the target, clamped to the screen edge).
            var edgeArea = Stretch("GuideEdge", safe);
            var edge = UIRect("Arrow", edgeArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 138f));
            Img(edge, _uArrowUp ? _uArrowUp : _sPlay, Color.white);
            _edgeArrow = edge;
            _edgeArea = edgeArea;

            var subRt = UIRect("Subtitle", banner, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(660f, 76f));
            Sliced(subRt, _uNote ? _uNote : _sPanel, 1.1f);
            var subT = Label(Stretch("Text", subRt, 30f, 30f, 8f, 12f), "", 36f, UiInk, TextAlignmentOptions.Center, false);
            subT.enableAutoSizing = true;
            subT.fontSizeMin = 24f;
            subT.fontSizeMax = 36f;
            subRt.gameObject.SetActive(false);

            var ub = bannerRoot.gameObject.AddComponent<UnlockBanner>();
            ub.banner = banner;
            ub.titleText = bTitle;
            ub.subtitleText = subT;
            ub.icon = bIconImg;

            // ---------------- upgrade shop (awning panel)
            BuildUpgradePanel(safe, world);

            // ---------------- flying coins
            var coinLayer = Stretch("Coins", root);
            var coinT = UIRect("Coin", coinLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f));
            var coinImg = Img(coinT, _uCoin ? _uCoin : _sCoin, Color.white);
            hud.coinLayer = coinLayer;
            hud.coinTemplate = coinImg;
            var appleT = UIRect("Apple", coinLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));
            hud.appleTemplate = Img(appleT, _sApple ? _sApple : _sStar, Color.white);

            // ---------------- offer popup (wooden panel)
            float panelScale = PanelScale(_uPanelGold ? _uPanelGold : _uPanelPlain);
            var popRoot = Stretch("OfferPopup", root);
            var dim = Img(popRoot, _sWhite, new Color(0.05f, 0.04f, 0.08f, 0.62f), false, true);
            dim.preserveAspect = false;
            var pw = UIRect("Window", popRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(860f, 1100f));
            var popSprite = _uPanelGold ? _uPanelGold : _uPanelPlain;
            Sliced(pw, popSprite ? popSprite : _sPanel, panelScale, null, true);
            // Header plank / ribbon centre, measured from the top of the source panel.
            float plankY = -PlankY(popSprite) * panelScale;
            var pTitle = Label(UIRect("Title", pw, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(TitleX(popSprite), plankY), new Vector2(480f, 100f)),
                "OFFER", 58f, Color.white, TextAlignmentOptions.Center, true);
            pTitle.enableAutoSizing = true;
            pTitle.fontSizeMin = 36f;
            pTitle.fontSizeMax = 58f;
            var rays = UIRect("Rays", pw, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -375f), new Vector2(520f, 520f));
            Img(rays, _sRays, new Color(1f, 0.85f, 0.35f, 0.75f));
            rays.gameObject.AddComponent<UISpin>().speed = 18f;
            var pIcon = UIRect("Icon", pw, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -375f), new Vector2(300f, 300f));
            var pIconImg = Img(pIcon, _uHeroCoins ? _uHeroCoins : _sCash2x, Color.white);
            var pBody = Label(UIRect("Body", pw, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -540f), new Vector2(700f, 200f)),
                "", 42f, UiInk, TextAlignmentOptions.Center, false);
            pBody.enableAutoSizing = true;
            pBody.fontSizeMin = 30f;
            pBody.fontSizeMax = 42f;
            pBody.textWrappingMode = TextWrappingModes.Normal;
            pBody.richText = true;
            var prim = UIRect("Primary", pw, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 200f), new Vector2(580f, 150f));
            var primBtn = AtlasButton(prim, _uBtnGreen ? _uBtnGreen : _sButton, 2.7f, false);
            var primAd = UIRect("Ad", prim, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(96f, 8f), new Vector2(100f, 100f));
            Img(primAd, _uWatchAd ? _uWatchAd : _sAd, Color.white);
            var primText = Label(Stretch("Text", prim, 160f, 34f, 0f, 22f), "WATCH", 62f, Color.white, TextAlignmentOptions.Center, true);
            var sec = UIRect("Secondary", pw, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 98f), new Vector2(420f, 86f));
            var secImg = Img(sec, _sWhite, new Color(1f, 1f, 1f, 0f), false, true);
            var secBtn = sec.gameObject.AddComponent<Button>();
            secBtn.targetGraphic = secImg;
            var secText = Label(Stretch("Text", sec), "No thanks", 42f, new Color(0.55f, 0.42f, 0.34f), TextAlignmentOptions.Center, false);
            var popup = popRoot.gameObject.AddComponent<OfferPopup>();
            popup.window = pw;
            popup.icon = pIconImg;
            popup.iconRect = pIcon;
            popup.titleText = pTitle;
            popup.bodyText = pBody;
            popup.primary = primBtn;
            popup.primaryText = primText;
            popup.primaryAdBadge = primAd.gameObject;
            popup.secondary = secBtn;
            popup.secondaryText = secText;
            popup.closeButton = CloseHotspot(pw, popSprite, popSprite ? popSprite.name : "", panelScale);
            popup.dim = dim;

            // ---------------- settings popup
            var setRoot = Stretch("SettingsPopup", root);
            var setDimImg = Img(setRoot, _sWhite, new Color(0.05f, 0.04f, 0.08f, 0.55f), false, true);
            setDimImg.preserveAspect = false;
            var setDim = setRoot.gameObject.AddComponent<Button>();
            setDim.targetGraphic = setDimImg;
            setDim.transition = Selectable.Transition.None;
            var sw = UIRect("Window", setRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(780f, 640f));
            Sliced(sw, _uPanelPlain ? _uPanelPlain : _sPanel, panelScale, null, true);
            Label(UIRect("Title", sw, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, plankY), new Vector2(460f, 100f)),
                "SETTINGS", 56f, Color.white, TextAlignmentOptions.Center, true);
            Image Toggle(string label, float y, out Button btn)
            {
                var row = UIRect(label, sw, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(620f, 140f));
                Sliced(row, _uCard ? _uCard : _sPanel, 1.3f);
                Label(UIRect("Label", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 4f), new Vector2(360f, 80f)),
                    label, 50f, UiInk, TextAlignmentOptions.MidlineLeft, false);
                var tg = UIRect("Toggle", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, 2f), new Vector2(190f, 110f));
                var img = Img(tg, _uToggleOn ? _uToggleOn : _sPill, Color.white, false, true);
                btn = tg.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                tg.gameObject.AddComponent<UIPress>();
                return img;
            }
            var sndImg = Toggle("Sound", -300f, out var sndBtn);
            var ambImg = Toggle("Ambience", -470f, out var ambBtn);
            var sClose = CloseHotspot(sw, _uPanelPlain, _uPanelPlain ? _uPanelPlain.name : "", panelScale);
            var settings = setRoot.gameObject.AddComponent<SettingsPopup>();
            settings.window = sw;
            settings.openButton = gearBtn;
            settings.closeButton = sClose;
            settings.dimButton = setDim;
            settings.soundToggle = sndImg;
            settings.ambienceToggle = ambImg;
            settings.soundButton = sndBtn;
            settings.ambienceButton = ambBtn;
            settings.toggleOn = _uToggleOn;
            settings.toggleOff = _uToggleOff;

            // ---------------- expansion: delivery card, world-complete popup, intro overlay
            _uiDelivery = world >= 1 ? BuildDeliveryHUD(safe) : null;
            _uiNextWorld = world <= 1 ? BuildNextWorldButton(safe, world + 1) : null;
            _uiCompletion = world <= 1 ? BuildCompletionPopup(root, world + 1) : null;
            if (world >= 1) BuildDeliveryPopup(root);
            _uiIntro = world >= 1 ? BuildIntroOverlay(root) : null;
            _uiFox = world >= 2 ? BuildFoxHUD(safe) : null;
            BuildPremiumPopup(root);

            // ---------------- simulated ad overlay (always on top)
            var adRoot = Stretch("AdOverlay", root);
            var adDim = Img(adRoot, _sWhite, new Color(0.02f, 0.02f, 0.04f, 0.94f), false, true);
            adDim.preserveAspect = false;
            var adGroup = adRoot.gameObject.AddComponent<CanvasGroup>();
            var adCard = UIRect("Card", adRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 760f));
            var adRing = UIRect("Ring", adCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(340f, 340f));
            var adRingImg = Img(adRing, _sRingThick, new Color(1f, 0.78f, 0.22f));
            adRingImg.type = Image.Type.Filled;
            adRingImg.fillMethod = Image.FillMethod.Radial360;
            adRingImg.fillOrigin = (int)Image.Origin360.Top;
            var adCount = Label(UIRect("Count", adCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(300f, 200f)),
                "3", 150f, Color.white, TextAlignmentOptions.Center, true);
            var adTitle = Label(UIRect("Title", adCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -220f), new Vector2(740f, 200f)),
                "Rewarded video", 56f, Color.white, TextAlignmentOptions.Center, false);
            adTitle.textWrappingMode = TextWrappingModes.Normal;
            adTitle.richText = true;
            var adClose = UIRect("Close", adRoot, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-90f, -130f), new Vector2(110f, 110f));
            var adCloseBtn = IconButton(adClose, _uX ? _uX : _sCircle);
            var ad = adRoot.gameObject.AddComponent<AdOverlay>();
            ad.group = adGroup;
            ad.card = adCard;
            ad.ring = adRingImg;
            ad.countText = adCount;
            ad.titleText = adTitle;
            ad.closeButton = adCloseBtn;

            // ---------------- world-change fade (above everything)
            var fadeRt = Stretch("Fader", root);
            var fadeImg = Img(fadeRt, _sWhite, new Color(0.04f, 0.03f, 0.06f, 1f), false, false);
            fadeImg.preserveAspect = false;
            fadeRt.gameObject.AddComponent<ScreenFader>().image = fadeImg;
        }
    }
}
