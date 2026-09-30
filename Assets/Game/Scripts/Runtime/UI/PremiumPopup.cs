using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>
    /// "Fix it now" card with two ways to pay: Golden Apples (premium) or a rewarded video. Used for fox-raided farms and
    /// other instant actions. An optional live line (e.g. "Regrows in 4:12") updates while it is open.
    /// </summary>
    public class PremiumPopup : MonoBehaviour
    {
        public static PremiumPopup I { get; private set; }

        public RectTransform window;
        public Image dim;
        public Image icon;
        public RectTransform iconRect;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI bodyText;
        public TextMeshProUGUI liveText;
        public Button appleButton;
        public TextMeshProUGUI appleCostText;
        public Button adButton;
        public Button closeButton;

        Action _onApple, _onAd;
        Func<string> _live;
        string _placement;
        int _cost;
        bool _open;

        public bool IsOpen => _open;

        void Awake()
        {
            I = this;
            appleButton.onClick.AddListener(OnApple);
            adButton.onClick.AddListener(OnAd);
            if (closeButton != null) closeButton.onClick.AddListener(() => Close());
            gameObject.SetActive(false);
        }

        public void Show(string title, string body, Sprite sprite, int appleCost, Func<string> live, Action onApple, string adPlacement, Action onAd)
        {
            if (_open) return;
            if (OfferPopup.I != null && OfferPopup.I.IsOpen) return;
            _open = true;
            _onApple = onApple;
            _onAd = onAd;
            _live = live;
            _placement = adPlacement;
            _cost = appleCost;
            titleText.text = title;
            bodyText.text = body;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            if (appleCostText != null) appleCostText.text = appleCost.ToString();
            adButton.gameObject.SetActive(onAd != null);
            RefreshLive();
            RefreshAfford();
            Platform.Pause("premium");
            InputJoystick.Blocked = true;
            gameObject.SetActive(true);
            Tweener.Scale(window, Vector3.one * 0.4f, Vector3.one, 0.4f, Ease.OutBack);
            if (iconRect != null) Tweener.Scale(iconRect, Vector3.zero, Vector3.one, 0.55f, Ease.OutElastic, null, 0.1f);
            Sfx.Play(SfxId.Whoosh, 0.3f, 1.2f);
        }

        void RefreshLive()
        {
            if (liveText == null) return;
            string s = _live != null ? _live() : null;
            liveText.gameObject.SetActive(!string.IsNullOrEmpty(s));
            if (!string.IsNullOrEmpty(s)) liveText.text = s;
        }

        void RefreshAfford()
        {
            if (appleCostText == null || GameManager.I == null) return;
            appleCostText.color = GameManager.I.Apples >= _cost ? Color.white : new Color(1f, 0.55f, 0.5f);
        }

        void Update()
        {
            if (!_open) return;
            RefreshLive();
            RefreshAfford();
            float s = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.035f;
            appleButton.transform.localScale = new Vector3(s, s, 1f);
            if (iconRect != null && iconRect.localScale.x > 0.99f) iconRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2.4f) * 7f);
        }

        void OnApple()
        {
            if (GameManager.I == null) return;
            if (!GameManager.I.TrySpendApples(_cost))
            {
                Sfx.Play(SfxId.Error, 0.35f);
                Tweener.Shake(appleButton.transform, 12f, 0.3f);
                if (HUD.I != null && HUD.I.applesPanel != null) Tweener.Punch(HUD.I.applesPanel, 0.25f, 0.3f, Vector3.one);
                return;
            }
            var then = _onApple;
            Close(() =>
            {
                if (GameRefs.I != null && GameRefs.I.player != null) AppleFx.Spend(GameRefs.I.player.transform.position + Vector3.up * 2f);
                then?.Invoke();
            });
        }

        void OnAd()
        {
            var then = _onAd;
            var placement = _placement;
            Close(() => Ads.ShowRewarded(placement, then));
        }

        public void Close(Action then = null)
        {
            if (!_open) return;
            _open = false;
            Platform.Resume("premium");
            InputJoystick.Blocked = false;
            Sfx.Play(SfxId.Click, 0.5f);
            Tweener.Scale(window, window.localScale, Vector3.zero, 0.16f, Ease.InQuad, () =>
            {
                if (!_open) gameObject.SetActive(false);
                window.localScale = Vector3.one;
            });
            then?.Invoke();
        }
    }
}
