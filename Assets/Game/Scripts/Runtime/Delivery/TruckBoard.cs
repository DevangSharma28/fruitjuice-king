using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Order sign standing beside the truck stop: client, juice, "12 / 25", progress, reward and time left while a truck
    /// is at the bay, or the countdown to the next truck otherwise.
    /// </summary>
    public class TruckBoard : MonoBehaviour
    {
        public TextMeshPro titleText;
        public TextMeshPro clientText;
        public TextMeshPro countText;
        public TextMeshPro rewardText;
        public TextMeshPro timerText;
        public SpriteRenderer juiceIcon;
        public Transform progressFill;
        public SpriteRenderer progressRenderer;
        [Tooltip("Shown while a truck is on its way or parked.")]
        public GameObject orderGroup;
        [Tooltip("Shown between trucks.")]
        public GameObject waitGroup;
        public TextMeshPro waitText;
        [Tooltip("Punched on news (truck arrived, delivery complete).")]
        public Transform panel;

        Vector3 _fillScale = Vector3.one;
        Vector3 _panelScale = Vector3.one;
        int _lastDelivered = -1;
        int _lastSecond = -1;
        int _mode = -1;
        float _shownFill;
        bool _done;
        bool _init;

        // Show() can run before Awake (inactive object), so capture sizes lazily.
        void Init()
        {
            if (_init) return;
            _init = true;
            if (progressFill != null) _fillScale = progressFill.localScale;
            if (panel != null) _panelScale = panel.localScale;
        }

        void Awake() => Init();

        void Update()
        {
            var dm = DeliveryManager.I;
            if (dm == null) return;
            var o = dm.Order;
            int mode = o == null ? 0 : dm.Loading ? 2 : 1;
            if (mode != _mode && !(_done && o != null))
            {
                _mode = mode;
                _done = false;
                if (orderGroup != null) orderGroup.SetActive(o != null);
                if (waitGroup != null) waitGroup.SetActive(o == null);
                if (titleText != null) titleText.text = o == null ? "NEXT TRUCK" : mode == 2 ? "LOAD THE TRUCK!" : "TRUCK ON THE WAY";
                if (o != null) Show(o);
                _lastSecond = -1;
            }
            if (o == null) _done = false;

            if (o != null && !_done)
            {
                // Smooth bar between cup arrivals.
                _shownFill = Mathf.MoveTowards(_shownFill, o.Progress, Time.deltaTime * 1.5f);
                SetFill(_shownFill);
            }

            int sec = Mathf.CeilToInt(o != null ? dm.TimeLeft : Mathf.Max(0f, dm.Cooldown));
            if (sec == _lastSecond) return;
            _lastSecond = sec;
            string mmss = (sec / 60) + ":" + (sec % 60).ToString("00");
            if (o == null)
            {
                if (waitText != null) waitText.text = mmss;
            }
            else if (timerText != null)
            {
                timerText.text = mode == 2 && !_done ? mmss : "";
                timerText.color = sec <= 30 ? new Color(1f, 0.35f, 0.3f) : new Color(0.36f, 0.22f, 0.14f);
            }
        }

        void SetFill(float p)
        {
            if (progressFill == null) return;
            progressFill.localScale = new Vector3(_fillScale.x * Mathf.Max(0.001f, p), _fillScale.y, _fillScale.z);
            progressFill.localPosition = new Vector3(-0.5f * _fillScale.x * (1f - p), progressFill.localPosition.y, progressFill.localPosition.z);
        }

        public void Show(DeliveryOrder o)
        {
            Init();
            if (o == null) return;
            if (clientText != null) clientText.text = o.client;
            if (rewardText != null) rewardText.text = "$" + Economy.Money((long)(o.reward * Economy.DeliveryBoostMult));
            if (juiceIcon != null && GameRefs.I != null) juiceIcon.sprite = GameRefs.I.JuiceIcon(o.kind);
            if (countText != null) countText.text = o.delivered + " / " + o.qty;
            if (_lastDelivered >= 0 && o.delivered > _lastDelivered && countText != null) Tweener.Punch(countText.transform, 0.2f, 0.18f, Vector3.one);
            if (_lastDelivered < 0 || o.delivered < _lastDelivered) _shownFill = o.Progress;
            _lastDelivered = o.delivered;
            if (progressRenderer != null) progressRenderer.color = o.Done ? new Color(1f, 0.85f, 0.2f) : new Color(0.4f, 0.9f, 0.35f);
        }

        public void ShowDone()
        {
            _done = true;
            if (countText != null) countText.text = "DONE!";
            if (titleText != null) titleText.text = "DELIVERY COMPLETE!";
            if (timerText != null) timerText.text = "";
            SetFill(1f);
            Punch();
        }

        public void Punch()
        {
            Init();
            var t = panel != null ? panel : transform;
            Tweener.Punch(t, 0.12f, 0.35f, panel != null ? _panelScale : Vector3.one);
        }

        /// <summary>A new truck is coming: forget the previous order's counters.</summary>
        public void ResetOrder()
        {
            _lastDelivered = -1;
            _shownFill = 0f;
            _mode = -1;
            _done = false;
        }
    }
}
