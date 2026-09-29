using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>One row slot of the upgrade panel. Rows are bound to <see cref="UpgradeDef"/>s at runtime.</summary>
    [Serializable]
    public class UpgradeRow
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI statText;
        public Button button;
        public Image[] pips;
        [NonSerialized] public UpgradeDef def;
    }

    [Serializable]
    public class UpgradeTab
    {
        public string category;
        public Button button;
        public Image background;
        public TextMeshProUGUI label;
        public GameObject badge;
    }

    /// <summary>
    /// Upgrade shop popup, shown while standing on the upgrade pad. Rows come from the world's upgrade tree
    /// (<see cref="Upgrades.ForWorld"/>); with more than one category the tabs switch between them.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        public static UpgradePanel I { get; private set; }

        public RectTransform window;
        public UpgradeRow[] rows;
        public UpgradeTab[] tabs;
        public Button closeButton;
        public Color pipOn = new Color(1f, 0.75f, 0.1f);
        public Color pipOff = new Color(0.8f, 0.77f, 0.72f, 1f);
        public Color tabOn = Color.white;
        public Color tabOff = new Color(0.72f, 0.68f, 0.64f, 1f);

        bool _open;
        string _category;

        public bool IsOpen => _open;
        readonly List<UpgradeDef> _shown = new List<UpgradeDef>();

        /// <summary>Closed with the X: stays shut until the player steps off and back onto the pad.</summary>
        public bool Dismissed { get; private set; }

        void Awake()
        {
            I = this;
            if (closeButton != null) closeButton.onClick.AddListener(() =>
            {
                Dismissed = true;
                Hide();
                Sfx.Play(SfxId.Click, 0.5f);
            });
            foreach (var r in rows)
            {
                var row = r;
                row.button.onClick.AddListener(() => Buy(row));
            }
            if (tabs != null)
                foreach (var t in tabs)
                {
                    var tab = t;
                    if (tab.button != null) tab.button.onClick.AddListener(() =>
                    {
                        Sfx.Play(SfxId.Click, 0.45f, 1.2f);
                        SelectTab(tab.category);
                    });
                }
            gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (GameManager.I != null) GameManager.I.MoneyChanged += OnMoney;
        }

        void OnDisable()
        {
            if (GameManager.I != null) GameManager.I.MoneyChanged -= OnMoney;
        }

        void OnMoney(long v, long d) => Refresh();

        List<UpgradeDef> Tree => Upgrades.ForWorld(Economy.World);

        public void Show()
        {
            Dismissed = false;
            if (_open) return;
            _open = true;
            gameObject.SetActive(true);
            if (_category == null) _category = FirstAffordableCategory();
            SelectTab(_category, false);
            Tweener.Scale(window, Vector3.one * 0.5f, Vector3.one, 0.35f, Ease.OutBack);
            Sfx.Play(SfxId.Click, 0.6f);
        }

        public void Hide()
        {
            if (!_open) return;
            _open = false;
            Tweener.Scale(window, window.localScale, Vector3.zero, 0.18f, Ease.InQuad, () =>
            {
                if (!_open) gameObject.SetActive(false);
            });
        }

        /// <summary>Open on the first tab with something the player can buy right now.</summary>
        string FirstAffordableCategory()
        {
            if (tabs == null || tabs.Length == 0) return null;
            long money = GameManager.I.Money;
            foreach (var t in tabs)
                foreach (var d in Tree)
                    if (d.category == t.category && !d.Maxed && d.Cost() <= money) return t.category;
            return tabs[0].category;
        }

        void SelectTab(string category, bool animate = true)
        {
            _category = category;
            _shown.Clear();
            foreach (var d in Tree)
                if (tabs == null || tabs.Length == 0 || d.category == category) _shown.Add(d);
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                r.def = i < _shown.Count ? _shown[i] : null;
                if (r.root != null) r.root.SetActive(r.def != null);
                if (r.def == null) continue;
                if (r.nameText != null) r.nameText.text = r.def.name;
                if (r.icon != null && GameRefs.I != null)
                {
                    var sp = GameRefs.I.UpgradeIcon(r.def.icon);
                    if (sp != null) r.icon.sprite = sp;
                }
                if (animate && r.root != null) Tweener.Scale(r.root.transform, new Vector3(0.9f, 0.9f, 1f), Vector3.one, 0.22f, Ease.OutBack, null, i * 0.03f);
            }
            Refresh();
        }

        void Buy(UpgradeRow row)
        {
            if (row.def == null) return;
            if (GameManager.I.TryBuy(row.def))
            {
                Sfx.Play(SfxId.Unlock, 0.6f);
                Sfx.Play(SfxId.Sparkle, 0.35f);
                Tweener.Punch(row.button.transform, 0.2f, 0.25f, Vector3.one);
                if (row.pips != null)
                {
                    int lvl = row.def.Level() - 1;
                    if (lvl >= 0 && lvl < row.pips.Length) Tweener.Scale(row.pips[lvl].rectTransform, Vector3.one * 1.8f, Vector3.one, 0.4f, Ease.OutBack);
                }
                var p = GameRefs.I.player.transform.position;
                Fx.Confetti(p + Vector3.up * 1.5f, 30);
            }
            else Sfx.Play(SfxId.Error, 0.5f);
            Refresh();
        }

        /// <summary>"Carry 16" and "Carry 22" become "Carry 16 > 22" (the shared prefix is written once).</summary>
        static string StatLine(UpgradeDef d, int lvl)
        {
            if (d.stat == null) return "";
            string now = d.stat(lvl);
            if (lvl >= d.maxLevel) return now;
            string next = d.stat(lvl + 1);
            int cut = now.LastIndexOf(' ');
            string tail = next;
            if (cut > 0 && next.Length > cut && string.CompareOrdinal(now, 0, next, 0, cut + 1) == 0) tail = next.Substring(cut + 1);
            return now + " <color=#3BA84A>> " + tail + "</color>";
        }

        void Refresh()
        {
            var gm = GameManager.I;
            foreach (var r in rows)
            {
                if (r.def == null) continue;
                int lvl = r.def.Level();
                int cost = r.def.Cost();
                r.levelText.text = lvl >= r.def.maxLevel ? "MAX" : "LV " + (lvl + 1);
                for (int i = 0; i < r.pips.Length; i++)
                {
                    bool used = i < r.def.maxLevel;
                    r.pips[i].gameObject.SetActive(used);
                    r.pips[i].color = i < lvl ? pipOn : pipOff;
                }
                if (r.statText != null) r.statText.text = StatLine(r.def, lvl);

                if (cost < 0)
                {
                    r.costText.text = "MAX";
                    r.button.interactable = false;
                }
                else
                {
                    r.costText.text = "$" + UnlockZone.Format(cost);
                    r.button.interactable = gm.Money >= cost;
                }
            }

            if (tabs == null) return;
            foreach (var t in tabs)
            {
                bool on = t.category == _category;
                if (t.background != null) t.background.color = on ? tabOn : tabOff;
                if (t.button != null) t.button.transform.localScale = on ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
                if (t.badge != null)
                {
                    // Dot on tabs that have something affordable.
                    bool any = false;
                    foreach (var d in Tree)
                        if (d.category == t.category && !d.Maxed && d.Cost() <= gm.Money) { any = true; break; }
                    t.badge.SetActive(any && !on);
                }
            }
        }
    }
}
