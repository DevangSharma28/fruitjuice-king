using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>"NEW!" ribbon that drops in from the top after an unlock.</summary>
    public class UnlockBanner : MonoBehaviour
    {
        public static UnlockBanner I { get; private set; }

        public RectTransform banner;
        public TextMeshProUGUI titleText;
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

        public void Show(string title, Sprite sprite)
        {
            int serial = ++_serial;
            titleText.text = title;
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
            Tweener.Delay(hold, () =>
            {
                if (serial != _serial) return;
                Vector2 f2 = banner.anchoredPosition;
                Tweener.Value(banner, 0.3f, t => banner.anchoredPosition = Vector2.Lerp(f2, _hidden, Ease.InQuad(t)),
                    () => { if (serial == _serial) banner.gameObject.SetActive(false); });
            });
        }
    }
}
