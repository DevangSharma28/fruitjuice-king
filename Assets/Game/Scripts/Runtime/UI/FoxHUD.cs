using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>HUD chip shown while a fox-raided farm regrows: fox icon, countdown, tap to restore it now.</summary>
    public class FoxHUD : MonoBehaviour
    {
        public RectTransform root;
        public Button button;
        public RectTransform icon;
        public TextMeshProUGUI timerText;

        bool _shown;
        int _sec = -1;

        void Start()
        {
            if (button != null) button.onClick.AddListener(OnTap);
            root.gameObject.SetActive(false);
        }

        void OnTap()
        {
            Sfx.Play(SfxId.Click, 0.45f);
            var raid = FoxRaid.I;
            if (raid != null) raid.OfferRestore(raid.FirstDamaged());
        }

        void Update()
        {
            var raid = FoxRaid.I;
            var field = raid != null ? raid.FirstDamaged() : null;
            bool show = field != null && !ExpansionIntro.Playing;
            if (show != _shown)
            {
                _shown = show;
                root.gameObject.SetActive(show);
                if (show) Tweener.Scale(root, Vector3.zero, Vector3.one, 0.45f, Ease.OutBack);
            }
            if (!show) return;
            int s = Mathf.CeilToInt(raid.TimeLeft(field));
            if (s != _sec && timerText != null)
            {
                _sec = s;
                timerText.text = FoxRaid.Clock(s);
            }
            if (icon != null)
                icon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 12f) * 8f * Mathf.Clamp01(Mathf.Sin(Time.unscaledTime * 1.3f) * 3f - 2f));
        }
    }
}
