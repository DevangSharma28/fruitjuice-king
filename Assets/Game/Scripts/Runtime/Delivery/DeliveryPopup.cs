using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>
    /// Delivery details, opened from the parcel button: the current order (client, juice, progress, reward, time left)
    /// or the countdown to the next truck, plus a button that shows you the bay. The game keeps running underneath.
    /// </summary>
    public class DeliveryPopup : MonoBehaviour
    {
        public static DeliveryPopup I { get; private set; }

        public RectTransform window;
        public Button dimButton;
        public Button closeButton;
        public Button goButton;
        public TextMeshProUGUI statusText;
        public GameObject orderGroup;
        public TextMeshProUGUI clientText;
        public Image juiceIcon;
        public TextMeshProUGUI countText;
        public Image progressFill;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI timerText;
        public GameObject waitGroup;
        public TextMeshProUGUI waitText;

        bool _open;
        float _fill;
        int _lastSecond = -1;
        int _lastDelivered = -1;

        public bool IsOpen => _open;

        void Awake()
        {
            I = this;
            if (dimButton != null) dimButton.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (goButton != null) goButton.onClick.AddListener(GoToBay);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            // Never stack on another modal (welcome back, offers, world complete).
            if (_open || (OfferPopup.I != null && OfferPopup.I.IsOpen)) return;
            _open = true;
            gameObject.SetActive(true);
            _lastSecond = -1;
            _lastDelivered = -1;
            var dm = DeliveryManager.I;
            _fill = dm != null && dm.Order != null ? dm.Order.Progress : 0f;
            Refresh();
            Tweener.Scale(window, Vector3.one * 0.5f, Vector3.one, 0.35f, Ease.OutBack);
            Sfx.Play(SfxId.Whoosh, 0.25f, 1.3f);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            Sfx.Play(SfxId.Click, 0.4f);
            Tweener.Scale(window, window.localScale, Vector3.one * 0.6f, 0.15f, Ease.InQuad, () =>
            {
                if (!_open) gameObject.SetActive(false);
                window.localScale = Vector3.one;
            });
        }

        void GoToBay()
        {
            var dm = DeliveryManager.I;
            Close();
            if (dm != null && dm.bay != null && !CameraFollow.Busy) CameraFollow.Peek(dm.bay.park.position, 1.4f, 1.15f);
        }

        void Update()
        {
            if (_open) Refresh();
        }

        void Refresh()
        {
            var dm = DeliveryManager.I;
            if (dm == null) return;
            var o = dm.Order;
            bool has = o != null;
            if (orderGroup != null && orderGroup.activeSelf != has) orderGroup.SetActive(has);
            if (waitGroup != null && waitGroup.activeSelf == has) waitGroup.SetActive(!has);

            if (has)
            {
                _fill = Mathf.MoveTowards(_fill, o.Progress, Time.unscaledDeltaTime * 1.5f);
                if (progressFill != null) progressFill.fillAmount = _fill;
                if (o.delivered != _lastDelivered)
                {
                    if (_lastDelivered >= 0 && countText != null) Tweener.Punch(countText.transform, 0.25f, 0.2f, Vector3.one);
                    _lastDelivered = o.delivered;
                    if (clientText != null) clientText.text = o.client;
                    if (juiceIcon != null && GameRefs.I != null) juiceIcon.sprite = GameRefs.I.JuiceIcon(o.kind);
                    if (countText != null) countText.text = o.delivered + " / " + o.qty + "  " + Balance.JuiceNames[(int)o.kind];
                    if (rewardText != null) rewardText.text = "$" + Economy.Money((long)(o.reward * Economy.DeliveryBoostMult));
                }
            }

            int sec = Mathf.CeilToInt(has ? dm.TimeLeft : Mathf.Max(0f, dm.Cooldown));
            if (sec == _lastSecond) return;
            _lastSecond = sec;
            string mmss = (sec / 60) + ":" + (sec % 60).ToString("00");
            if (statusText != null)
                statusText.text = !has ? "No truck at the bay right now" : dm.Loading ? "A truck is waiting - load it at the bay!" : "A truck is on its way...";
            if (has && timerText != null)
            {
                timerText.text = dm.Loading ? "Leaves in " + mmss : "";
                timerText.color = sec <= 30 ? new Color(0.9f, 0.25f, 0.2f) : new Color(0.36f, 0.22f, 0.14f);
            }
            if (!has && waitText != null) waitText.text = "Next truck in " + mmss;
        }
    }
}
