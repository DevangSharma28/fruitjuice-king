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
        public Color pipOn = new Color(1f, 0.75f, 0.1f);
        public Color pipOff = new Color(0.8f, 0.77f, 0.72f, 1f);

        bool _open;

        void Awake()
        {
            I = this;
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
                Tweener.Punch(row.button.transform, 0.2f, 0.25f, Vector3.one);
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

                string stat = r.kind switch
                {
                    UpgradeKind.Saw => $"Cut power {Balance.SawDps(lvl):0}",
                    UpgradeKind.Bag => $"Carry {Balance.BagCapacity(lvl)}",
                    _ => $"Speed {Balance.MoveSpeed(lvl):0.0}"
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
