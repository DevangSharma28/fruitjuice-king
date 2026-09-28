using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace JuiceKing.EditorTools
{
    public static partial class JuiceKingBuilder
    {
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

        static void BuildUI(Transform systems, out HUD hud)
        {
            var canvasGo = new GameObject("UI", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(systems, false);

            // ---------------- joystick
            var area = Stretch("Joystick", root);
            var baseRt = UIRect("Base", area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(290f, 290f));
            Img(baseRt, _sRing, new Color(1f, 1f, 1f, 0.9f));
            var group = baseRt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var knob = UIRect("Knob", baseRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            Img(knob, _sCircle, new Color(1f, 1f, 1f, 0.95f));
            var js = area.gameObject.AddComponent<InputJoystick>();
            js.area = area;
            js.baseRect = baseRt;
            js.knob = knob;
            js.group = group;
            js.radius = 120f;

            // ---------------- money
            var money = UIRect("Money", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(400f, 124f));
            Img(money, _sRound, new Color(0f, 0f, 0f, 0.4f), true);
            var mIcon = UIRect("Icon", money, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72f, 0f), new Vector2(116f, 116f));
            Img(mIcon, _sMoney, Color.white);
            var mText = Label(Stretch("Value", money, 140f, 34f, 0f, 0f), "0", 74f, Color.white, TextAlignmentOptions.MidlineRight, true);

            // ---------------- objective
            var obj = UIRect("Objective", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(920f, 112f));
            Img(obj, _sRound, new Color(1f, 1f, 1f, 0.95f), true);
            var objText = Label(Stretch("Text", obj, 30f, 30f, 0f, 0f), "", 46f, new Color(0.28f, 0.2f, 0.18f), TextAlignmentOptions.Center, false);
            objText.enableAutoSizing = true;
            objText.fontSizeMin = 28f;
            objText.fontSizeMax = 46f;

            // ---------------- sound button
            var snd = UIRect("Sound", root, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-84f, -122f), new Vector2(112f, 112f));
            Img(snd, _sCircle, new Color(0f, 0f, 0f, 0.4f), false, true);
            var sndBtn = snd.gameObject.AddComponent<Button>();
            var sndIcon = UIRect("Icon", snd, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
            var sndImg = Img(sndIcon, _sSoundOn, Color.white);

            hud = canvasGo.AddComponent<HUD>();
            hud.moneyText = mText;
            hud.moneyIcon = mIcon;
            hud.moneyPanel = money;
            hud.objectivePanel = obj;
            hud.objectiveText = objText;
            hud.soundButton = sndBtn;
            hud.soundIcon = sndImg;
            hud.soundOn = _sSoundOn;
            hud.soundOff = _sSoundOff;

            // ---------------- upgrade panel
            var panelRoot = Stretch("UpgradePanel", root);
            var window = UIRect("Window", panelRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(990f, 900f));
            Img(window, _sRound, Color.white, true, true);
            var title = UIRect("Title", window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 100f));
            Label(title, "UPGRADES", 78f, new Color(1f, 0.6f, 0.15f), TextAlignmentOptions.Center, true);

            string[] names = { "Chainsaw", "Backpack", "Sneakers" };
            Sprite[] icons = { _sSaw, _sBag, _sSpeed };
            UpgradeKind[] kinds = { UpgradeKind.Saw, UpgradeKind.Bag, UpgradeKind.Speed };
            var rows = new UpgradeRow[3];
            for (int i = 0; i < 3; i++)
            {
                var row = UIRect("Row" + i, window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f - i * 240f), new Vector2(930f, 220f));
                Img(row, _sRound, new Color(0.95f, 0.93f, 0.89f), true);
                var ic = UIRect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110f, 0f), new Vector2(160f, 160f));
                Img(ic, icons[i], Color.white);
                var nm = UIRect("Name", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, 50f), new Vector2(330f, 70f));
                Label(nm, names[i], 54f, new Color(0.3f, 0.22f, 0.2f), TextAlignmentOptions.MidlineLeft, false);
                var lv = UIRect("Level", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(470f, 50f), new Vector2(160f, 60f));
                var lvT = Label(lv, "LV 1", 38f, new Color(1f, 0.55f, 0.1f), TextAlignmentOptions.MidlineLeft, false);
                var st = UIRect("Stat", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, -8f), new Vector2(380f, 50f));
                var stT = Label(st, "", 34f, new Color(0.45f, 0.4f, 0.38f), TextAlignmentOptions.MidlineLeft, false);
                var pips = new Image[Balance.MaxUpgradeLevel];
                for (int p = 0; p < pips.Length; p++)
                {
                    var pip = UIRect("Pip" + p, row, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(228f + p * 46f, -64f), new Vector2(36f, 36f));
                    pips[p] = Img(pip, _sCircle, Color.white);
                }
                var btn = UIRect("Buy", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(290f, 160f));
                var bImg = Img(btn, _sButton, new Color(0.35f, 0.82f, 0.35f), true, true);
                var button = btn.gameObject.AddComponent<Button>();
                button.targetGraphic = bImg;
                var cb = button.colors;
                cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);
                button.colors = cb;
                var cost = Label(Stretch("Cost", btn, 10f, 10f, 0f, 14f), "$0", 56f, Color.white, TextAlignmentOptions.Center, true);

                rows[i] = new UpgradeRow
                {
                    kind = kinds[i], levelText = lvT, costText = cost, statText = stT, button = button, pips = pips
                };
            }

            var up = panelRoot.gameObject.AddComponent<UpgradePanel>();
            up.window = window;
            up.rows = rows;
        }
    }
}
