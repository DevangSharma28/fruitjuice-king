using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    [Serializable]
    public class UpgradeRow
    {
        public UpgradeKind kind;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI statText;
        public Button button;
        public Image[] pips;
    }

    /// <summary>Upgrade shop popup, shown while standing on the upgrade pad.</summary>
    public class UpgradePanel : MonoBehaviour
    {
        public static UpgradePanel I { get; private set; }

        public RectTransform window;
        public UpgradeRow[] rows;
        public Button closeButton;
        public Color pipOn = new Color(1f, 0.75f, 0.1f);
        public Color pipOff = new Color(0.8f, 0.77f, 0.72f, 1f);

        bool _open;

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

        public void Show()
        {
            Dismissed = false;
            if (_open) return;
            _open = true;
            gameObject.SetActive(true);
            Refresh();
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

        void Buy(UpgradeRow row)
        {
            if (GameManager.I.TryBuyUpgrade(row.kind))
            {
                Sfx.Play(SfxId.Unlock, 0.6f);
                Sfx.Play(SfxId.Sparkle, 0.35f);
                Tweener.Punch(row.button.transform, 0.2f, 0.25f, Vector3.one);
                if (row.pips != null)
                {
                    int lvl = GameManager.I.GetLevel(row.kind) - 1;
                    if (lvl >= 0 && lvl < row.pips.Length) Tweener.Scale(row.pips[lvl].rectTransform, Vector3.one * 1.8f, Vector3.one, 0.4f, Ease.OutBack);
                }
                var p = GameRefs.I.player.transform.position;
                Fx.Confetti(p + Vector3.up * 1.5f, 30);
            }
            else Sfx.Play(SfxId.Error, 0.5f);
            Refresh();
        }

        void Refresh()
        {
            var gm = GameManager.I;
            foreach (var r in rows)
            {
                int lvl = gm.GetLevel(r.kind);
                int cost = gm.GetCost(r.kind);
                r.levelText.text = "LV " + (lvl + 1);
                for (int i = 0; i < r.pips.Length; i++) r.pips[i].color = i < lvl ? pipOn : pipOff;

                bool maxed = lvl >= Balance.MaxUpgradeLevel;
                int nl = Mathf.Min(lvl + 1, Balance.MaxUpgradeLevel);
                const string arrow = " <color=#3BA84A>> ";
                string stat = r.kind switch
                {
                    UpgradeKind.Saw => $"Cut power {Balance.SawDps(lvl):0}" + (maxed ? "" : arrow + $"{Balance.SawDps(nl):0}</color>"),
                    UpgradeKind.Bag => $"Carry {Balance.BagCapacity(lvl)}" + (maxed ? "" : arrow + $"{Balance.BagCapacity(nl)}</color>"),
                    UpgradeKind.Speed => $"Speed {Balance.MoveSpeed(lvl):0.0}" + (maxed ? "" : arrow + $"{Balance.MoveSpeed(nl):0.0}</color>"),
                    UpgradeKind.Price => $"Price +{(Balance.PriceMult(lvl) - 1f) * 100f:0}%" + (maxed ? "" : arrow + $"+{(Balance.PriceMult(nl) - 1f) * 100f:0}%</color>"),
                    _ => $"Stack {Balance.CounterLayers(lvl) * 12}" + (maxed ? "" : arrow + $"{Balance.CounterLayers(nl) * 12}</color>")
                };
                if (r.statText != null) r.statText.text = stat;

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
        }
    }
}
