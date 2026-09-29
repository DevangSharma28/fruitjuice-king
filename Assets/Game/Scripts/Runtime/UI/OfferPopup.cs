using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>Modal card: icon, title, description, a primary (ad) button and a secondary button.</summary>
    public class OfferPopup : MonoBehaviour
    {
        public static OfferPopup I { get; private set; }

        public RectTransform window;
        public Image icon;
        public RectTransform iconRect;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI bodyText;
        public Button primary;
        public TextMeshProUGUI primaryText;
        public GameObject primaryAdBadge;
        public Button secondary;
        public TextMeshProUGUI secondaryText;
        public Button closeButton;
        public Image dim;

        Action _onPrimary, _onSecondary;
        bool _open;

        public bool IsOpen => _open;

        void Awake()
        {
            I = this;
            primary.onClick.AddListener(() => Close(_onPrimary));
            secondary.onClick.AddListener(() => Close(_onSecondary));
            if (closeButton != null) closeButton.onClick.AddListener(() => Close(_onSecondary));
            gameObject.SetActive(false);
        }

        public void Show(string title, string body, Sprite sprite, string primaryLabel, bool primaryIsAd, Action onPrimary,
            string secondaryLabel = "No thanks", Action onSecondary = null)
        {
            _onPrimary = onPrimary;
            _onSecondary = onSecondary;
            titleText.text = title;
            bodyText.text = body;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            primaryText.text = primaryLabel;
            if (primaryAdBadge != null) primaryAdBadge.SetActive(primaryIsAd);
            secondaryText.text = secondaryLabel;
            secondary.gameObject.SetActive(!string.IsNullOrEmpty(secondaryLabel));

            _open = true;
            Platform.Pause("offer");
            gameObject.SetActive(true);
            Tweener.Scale(window, Vector3.one * 0.4f, Vector3.one, 0.4f, Ease.OutBack);
            if (iconRect != null) Tweener.Scale(iconRect, Vector3.zero, Vector3.one, 0.5f, Ease.OutElastic, null, 0.12f);
            Sfx.Play(SfxId.Whoosh, 0.3f, 1.3f);
        }

        void Update()
        {
            if (!_open) return;
            // Breathing primary button draws the eye.
            float s = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.035f;
            primary.transform.localScale = new Vector3(s, s, 1f);
            if (iconRect != null && !IsTweening()) iconRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2.2f) * 6f);
        }

        bool IsTweening() => iconRect.localScale.x < 0.99f;

        void Close(Action then)
        {
            if (!_open) return;
            _open = false;
            Platform.Resume("offer");
            Sfx.Play(SfxId.Click, 0.6f);
            Tweener.Scale(window, window.localScale, Vector3.zero, 0.16f, Ease.InQuad, () =>
            {
                if (!_open) gameObject.SetActive(false);
            });
            then?.Invoke();
            // An ad pauses the game (and our tweens), so get out of its way immediately.
            if (Ads.Busy && !_open)
            {
                Tweener.Kill(window);
                gameObject.SetActive(false);
            }
        }
    }
}
