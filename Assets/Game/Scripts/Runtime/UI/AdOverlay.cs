using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>
    /// Stand-in for a real rewarded video: dims the screen, counts down, then grants the reward.
    /// Runs on unscaled time because the game is paused while it is shown.
    /// </summary>
    public class AdOverlay : MonoBehaviour
    {
        public static AdOverlay I { get; private set; }

        public CanvasGroup group;
        public RectTransform card;
        public Image ring;
        public TextMeshProUGUI countText;
        public TextMeshProUGUI titleText;
        public Button closeButton;
        public float duration = 3f;

        Action<bool> _done;
        float _t;
        bool _playing;
        bool _granted;
        float _prevTimeScale = 1f;

        void Awake()
        {
            I = this;
            if (closeButton != null) closeButton.onClick.AddListener(() => Finish(false));
            gameObject.SetActive(false);
        }

        public void Play(string placement, Action<bool> done)
        {
            _done = done;
            _t = 0f;
            _granted = false;
            _playing = true;
            gameObject.SetActive(true);
            if (titleText != null) titleText.text = "Rewarded video\n<size=60%>(test ad - plug your ad network into Ads.Provider)</size>";
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (group != null) group.alpha = 0f;
        }

        void Update()
        {
            if (!_playing) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            if (group != null) group.alpha = Mathf.Clamp01(_t / 0.2f);
            if (card != null)
            {
                float k = Mathf.Clamp01(_t / 0.35f);
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(k));
            }

            float left = Mathf.Max(0f, duration - _t);
            if (ring != null) ring.fillAmount = 1f - left / duration;
            if (countText != null) countText.text = left > 0f ? Mathf.CeilToInt(left).ToString() : "+";

            if (!_granted && left <= 0f)
            {
                _granted = true;
                if (titleText != null) titleText.text = "Reward earned!";
            }
            if (_granted && _t > duration + 0.5f) Finish(true);
        }

        void Finish(bool ok)
        {
            if (!_playing) return;
            _playing = false;
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
            gameObject.SetActive(false);
            var d = _done;
            _done = null;
            d?.Invoke(ok);
        }
    }
}
