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
        Func<bool> _stillValid;
        float _liveT;
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

        /// <param name="stillValid">Checked before anything is charged: false (the farm already grew back...) closes the
        /// card instead of taking apples or an ad for nothing. Also polled while open.</param>
        public void Show(string title, string body, Sprite sprite, int appleCost, Func<string> live, Action onApple, string adPlacement, Action onAd,
            Func<bool> stillValid = null)
        {
            if (_open) return;
            if (OfferPopup.I != null && OfferPopup.I.IsOpen) return;
            if (ExpansionIntro.Playing) return;
            _open = true;
            _stillValid = stillValid;
            _liveT = 0f;
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
            adButton.gameObject.SetActive(onAd != null && Ads.IsReady);
            appleButton.interactable = true;
            adButton.interactable = true;
            RefreshLive();
            RefreshAfford();
            Platform.Pause("premium");
            InputJoystick.Block("premium", true);
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
            if (_stillValid != null && !_stillValid())
            {
                Close();
                return;
            }
            // The live line ("Regrows in 4:12") only changes once a second.
            _liveT -= Time.unscaledDeltaTime;
            if (_liveT <= 0f)
            {
                _liveT = 0.25f;
                RefreshLive();
            }
            RefreshAfford();
            float s = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.035f;
            appleButton.transform.localScale = new Vector3(s, s, 1f);
            if (iconRect != null && iconRect.localScale.x > 0.99f) iconRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2.4f) * 7f);
        }

        void OnApple()
        {
            // The card stays clickable during its closing tween: a second tap must not charge again.
            if (!_open || GameManager.I == null) return;
            if (_stillValid != null && !_stillValid())
            {
                Close();
                return;
            }
            if (!GameManager.I.TrySpendApples(_cost))
            {
                Sfx.Play(SfxId.Error, 0.35f);
                Tweener.Shake(appleButton.transform, 12f, 0.3f);
                if (HUD.I != null && HUD.I.applesPanel != null) Tweener.Punch(HUD.I.applesPanel, 0.25f, 0.3f, Vector3.one);
                return;
            }
            var then = _onApple;
            Analytics.Log(Analytics.ApplesSpent, "amount", _cost, "what", _placement ?? "premium");
            Close(() =>
            {
                if (GameRefs.I != null && GameRefs.I.player != null) AppleFx.Spend(GameRefs.I.player.transform.position + Vector3.up * 2f);
                then?.Invoke();
            });
        }

        void OnAd()
        {
            if (!_open) return;
            if (_stillValid != null && !_stillValid())
            {
                Close();
                return;
            }
            var then = _onAd;
            var placement = _placement;
            Close(() => Ads.ShowRewarded(placement, then));
        }

        public void Close(Action then = null)
        {
            if (!_open) return;
            _open = false;
            _stillValid = null;
            appleButton.interactable = false;
            adButton.interactable = false;
            Platform.Resume("premium");
            InputJoystick.Block("premium", false);
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
