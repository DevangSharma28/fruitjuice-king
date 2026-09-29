using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>
    /// Parcel-box button on the HUD (shown once the Delivery Bay is open). A badge and a wiggle tell you a truck is
    /// waiting; tapping it opens <see cref="DeliveryPopup"/> with the order details.
    /// </summary>
    public class DeliveryHUD : MonoBehaviour
    {
        public RectTransform root;
        public Button button;
        public RectTransform icon;
        [Tooltip("Red '!' shown while a truck waits at the bay.")]
        public GameObject badge;

        bool _shown;
        bool _loading;

        void Start()
        {
            if (button != null) button.onClick.AddListener(OnTap);
            DeliveryManager.Completed += OnCompleted;
            root.gameObject.SetActive(false);
            if (badge != null) badge.SetActive(false);
        }

        void OnDestroy() => DeliveryManager.Completed -= OnCompleted;

        void OnTap()
        {
            Sfx.Play(SfxId.Click, 0.45f);
            if (DeliveryPopup.I != null) DeliveryPopup.I.Open();
        }

        void OnCompleted(DeliveryOrder o)
        {
            if (_shown) Tweener.Punch(root, 0.25f, 0.35f, Vector3.one);
        }

        void Update()
        {
            var dm = DeliveryManager.I;
            bool show = dm != null && dm.Unlocked;
            if (show != _shown)
            {
                _shown = show;
                root.gameObject.SetActive(show);
                if (show) Tweener.Scale(root, Vector3.zero, Vector3.one, 0.45f, Ease.OutBack);
            }
            if (!show) return;

            bool loading = dm.Loading;
            if (loading != _loading)
            {
                _loading = loading;
                if (badge != null)
                {
                    badge.SetActive(loading);
                    if (loading) Tweener.Scale(badge.transform, Vector3.zero, Vector3.one, 0.4f, Ease.OutElastic);
                }
            }
            // A waiting truck makes the box wiggle now and then.
            if (icon != null)
            {
                float a = loading ? Mathf.Sin(Time.unscaledTime * 14f) * 10f * Mathf.Clamp01(Mathf.Sin(Time.unscaledTime * 1.6f) * 3f - 2f) : 0f;
                icon.localRotation = Quaternion.Euler(0f, 0f, a);
            }
        }
    }
}
