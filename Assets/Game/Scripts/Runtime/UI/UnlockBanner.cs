using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>"NEW!" ribbon that drops in from the top after an unlock.</summary>
    public class UnlockBanner : MonoBehaviour
    {
        public static UnlockBanner I { get; private set; }

        /// <summary>Time (unscaled) until which the banner occupies the top of the screen; toasts wait for it.</summary>
        public static float BusyUntil { get; private set; }

        public RectTransform banner;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI subtitleText;
        public Image icon;
        public RectTransform shine;
        public float hold = 1.8f;

        Vector2 _shown, _hidden;
        int _serial;

        void Awake()
        {
            I = this;
            _shown = banner.anchoredPosition;
            _hidden = _shown + new Vector2(0f, 420f);
            banner.anchoredPosition = _hidden;
            banner.gameObject.SetActive(false);
        }

        public void Show(string title, Sprite sprite, string subtitle = null)
        {
            int serial = ++_serial;
            BusyUntil = Time.unscaledTime + hold + (string.IsNullOrEmpty(subtitle) ? 0f : 0.8f) + 0.35f;
            if (HUD.I != null) HUD.I.HideToast();
            titleText.text = title;
            if (subtitleText != null)
            {
                bool has = !string.IsNullOrEmpty(subtitle);
                subtitleText.text = subtitle ?? "";
                // The text sits on its own note background (its parent, unless it is placed straight on the banner).
                var holder = subtitleText.transform.parent != banner ? subtitleText.transform.parent.gameObject : subtitleText.gameObject;
                holder.SetActive(has);
                if (has) Tweener.Scale(holder.transform, new Vector3(0.6f, 0.6f, 1f), Vector3.one, 0.35f, Ease.OutBack, null, 0.25f);
            }
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            banner.gameObject.SetActive(true);
            Tweener.Kill(banner, 3);
            Vector2 from = banner.anchoredPosition;
            Tweener.Value(banner, 0.45f, t => banner.anchoredPosition = Vector2.LerpUnclamped(from, _shown, Ease.OutBack(t)));
            if (icon != null) Tweener.Scale(icon.rectTransform, Vector3.zero, Vector3.one, 0.6f, Ease.OutElastic, null, 0.15f);
            if (shine != null)
            {
                float w = banner.rect.width;
                Tweener.Value(shine, 0.7f, t => shine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.6f, w * 0.6f, t), 0f), null, 0.35f);
            }
            Tweener.Delay(hold + (string.IsNullOrEmpty(subtitle) ? 0f : 0.8f), () =>
            {
                if (serial != _serial) return;
                Vector2 f2 = banner.anchoredPosition;
                Tweener.Value(banner, 0.3f, t => banner.anchoredPosition = Vector2.Lerp(f2, _hidden, Ease.InQuad(t)),
                    () => { if (serial == _serial) banner.gameObject.SetActive(false); });
            });
        }
    }
}
