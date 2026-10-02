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
        [Header("Golden Apple action (optional)")]
        [Tooltip("CALL TRUCK NOW between trucks, FINISH ORDER while loading.")]
        public Button appleButton;
        public TextMeshProUGUI appleText;
        [Tooltip("Desk name (scenes with more than one desk).")]
        public TextMeshProUGUI deskText;

        DeliveryManager _dm;

        bool _open;
        float _fill;
        int _lastSecond = -1;
        int _lastDelivered = -1;
        DeliveryOrder _lastOrder;
        bool _lastBoost;
        // Golden Apple spend needs a second tap ("TAP TO CONFIRM") within a few seconds.
        float _confirmUntil;
        int _appleShown = int.MinValue;

        public bool IsOpen => _open;

        void Awake()
        {
            I = this;
            if (dimButton != null) dimButton.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (goButton != null) goButton.onClick.AddListener(GoToBay);
            if (appleButton != null) appleButton.onClick.AddListener(OnApple);
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
            _lastOrder = null;
            _confirmUntil = 0f;
            _appleShown = int.MinValue;
            _dm = DeliveryManager.Focus();
            var dm = _dm;
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

        void OnApple()
        {
            var dm = _dm;
            if (dm == null || !_open) return;
            bool call = dm.Order == null;
            int cost = call ? Economy.ApplesCallTruck : dm.FinishCost;
            string why = call && !dm.CanOrder ? "Nothing to order yet - open a farm first"
                : !call && cost <= 0 ? "Everything is already on its way!"
                : GameManager.I.Apples < cost ? "Not enough Golden Apples" : null;
            if (why != null)
            {
                Sfx.Play(SfxId.Error, 0.3f);
                if (appleButton != null) Tweener.Punch(appleButton.transform, 0.2f, 0.25f, Vector3.one);
                if (HUD.I != null) HUD.I.Toast(why, GameRefs.I != null ? GameRefs.I.appleIcon : null);
                // Out of apples: the shop is one tap away.
                if (GameManager.I.Apples < cost && ShopPopup.I != null)
                {
                    Close();
                    ShopPopup.I.Open(ShopSection.Apples);
                }
                return;
            }
            // First tap arms, second tap spends: premium currency is never spent by accident.
            if (Time.unscaledTime > _confirmUntil)
            {
                _confirmUntil = Time.unscaledTime + 3f;
                _appleShown = int.MinValue;
                Sfx.Play(SfxId.Click, 0.4f, 1.3f);
                Haptics.Play(HapticKind.Selection);
                if (appleButton != null) Tweener.Punch(appleButton.transform, 0.15f, 0.2f, Vector3.one);
                return;
            }
            _confirmUntil = 0f;
            bool ok = call ? dm.CallTruckNow() : dm.FinishNow();
            if (!ok)
            {
                Sfx.Play(SfxId.Error, 0.3f);
                return;
            }
            Sfx.Play(SfxId.Sparkle, 0.5f, 1.2f);
            _lastSecond = -1;
            _lastDelivered = -1;
            _appleShown = int.MinValue;
        }

        void GoToBay()
        {
            var dm = _dm != null ? _dm : DeliveryManager.I;
            Close();
            if (dm != null && dm.bay != null && !CameraFollow.Busy) CameraFollow.Peek(dm.bay.park.position, 1.4f, 1.15f);
        }

        void Update()
        {
            if (_open) Refresh();
        }

        void Refresh()
        {
            var dm = _dm != null ? _dm : DeliveryManager.I;
            if (dm == null) return;
            if (deskText != null)
            {
                bool multi = DeliveryManager.All.Count > 1;
                if (deskText.gameObject.activeSelf != multi) deskText.gameObject.SetActive(multi);
                if (multi) deskText.text = dm.deskName;
            }
            var o = dm.Order;
            bool has = o != null;
            if (orderGroup != null && orderGroup.activeSelf != has) orderGroup.SetActive(has);
            if (waitGroup != null && waitGroup.activeSelf == has) waitGroup.SetActive(!has);

            if (has)
            {
                _fill = Mathf.MoveTowards(_fill, o.Progress, Time.unscaledDeltaTime * 1.5f);
                if (progressFill != null) progressFill.fillAmount = _fill;
                bool boost = Boosts.IsActive(BoostKind.Cash2x);
                if (o.delivered != _lastDelivered || o != _lastOrder || boost != _lastBoost)
                {
                    _lastOrder = o;
                    _lastBoost = boost;
                    if (_lastDelivered >= 0 && countText != null) Tweener.Punch(countText.transform, 0.25f, 0.2f, Vector3.one);
                    _lastDelivered = o.delivered;
                    if (clientText != null) clientText.text = o.client;
                    if (juiceIcon != null && GameRefs.I != null) juiceIcon.sprite = GameRefs.I.ProductIcon(o.line, o.kind);
                    if (countText != null) countText.text = o.delivered + " / " + o.qty + "  " + o.Name;
                    if (rewardText != null) rewardText.text = "$" + Economy.Money((long)(o.reward * Economy.DeliveryBoostMult));
                }
            }

            if (appleButton != null)
            {
                bool offer = !has || dm.Loading;
                if (appleButton.gameObject.activeSelf != offer) appleButton.gameObject.SetActive(offer);
                bool confirming = Time.unscaledTime <= _confirmUntil;
                int key = !offer ? 0 : (confirming ? 100000 : 0) + (!has ? -1 - Economy.ApplesCallTruck : dm.FinishCost);
                if (offer && appleText != null && key != _appleShown)
                {
                    _appleShown = key;
                    appleText.text = confirming ? "TAP TO CONFIRM" : !has ? "CALL NOW  " + Economy.ApplesCallTruck : "FINISH  " + dm.FinishCost;
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
