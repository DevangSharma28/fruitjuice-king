using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>Money counter, shop progress, objective banner, sound toggle and flying-coin feedback.</summary>
    public class HUD : MonoBehaviour
    {
        public static HUD I { get; private set; }

        public TextMeshProUGUI moneyText;
        public RectTransform moneyIcon;
        public RectTransform moneyPanel;
        public RectTransform objectivePanel;
        public TextMeshProUGUI objectiveText;
        public Button soundButton;
        public Image soundIcon;
        public Sprite soundOn, soundOff;

        [Header("Progress")]
        public Image progressFill;
        public TextMeshProUGUI progressText;
        public RectTransform progressPanel;

        [Header("Toast")]
        public RectTransform toastPanel;
        public TextMeshProUGUI toastText;
        public Image toastIcon;

        [Header("Flying coins")]
        public RectTransform coinLayer;
        public Image coinTemplate;

        double _shown;
        string _objective = "\u0001";
        string _objectiveKey;
        float _progressShown = -1f;
        int _progressPct = -1;
        readonly Stack<Image> _coinPool = new Stack<Image>();
        Canvas _canvas;
        int _flying;

        int _toastSerial;
        Vector2 _toastShown;

        void Awake()
        {
            I = this;
            if (toastPanel != null)
            {
                _toastShown = toastPanel.anchoredPosition;
                toastPanel.gameObject.SetActive(false);
            }
            _canvas = GetComponentInParent<Canvas>();
            if (coinTemplate != null) coinTemplate.gameObject.SetActive(false);
        }

        void Start()
        {
            var gm = GameManager.I;
            _shown = gm.Money;
            moneyText.text = UnlockZone.Format(gm.Money);
            gm.MoneyChanged += OnMoney;
            UnlockManager.ZoneUnlocked += OnUnlocked;
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            RefreshSound();
            SetObjective(null);
            Sfx.StartAmbient();
            Platform.NotifyLoaded();
        }

        void OnDestroy()
        {
            if (GameManager.I != null) GameManager.I.MoneyChanged -= OnMoney;
            UnlockManager.ZoneUnlocked -= OnUnlocked;
        }

        void OnMoney(long value, long delta)
        {
            if (delta > 0 && moneyIcon != null && _flying == 0) Tweener.Punch(moneyIcon, 0.25f, 0.2f, Vector3.one);
        }

        void OnUnlocked(UnlockZone z)
        {
            if (progressPanel != null) Tweener.Punch(progressPanel, 0.15f, 0.35f, Vector3.one);
        }

        void Update()
        {
            long target = GameManager.I.Money;
            if (System.Math.Abs(_shown - target) > 0.01)
            {
                double speed = System.Math.Max(40.0, System.Math.Abs(target - _shown) * 8.0);
                _shown = _shown < target
                    ? System.Math.Min(target, _shown + speed * Time.deltaTime)
                    : System.Math.Max(target, _shown - speed * Time.deltaTime);
                moneyText.text = UnlockZone.Format((long)System.Math.Round(_shown));
            }

            if (progressFill != null)
            {
                // World progress: every unlock pad plus every upgrade level (all of it opens the next world).
                ExpansionManager.Progress(out int done, out int total);
                total = Mathf.Max(1, total);
                float goal = done / (float)total;
                _progressShown = _progressShown < 0f ? goal : Mathf.MoveTowards(_progressShown, goal, Time.deltaTime * 0.6f);
                progressFill.fillAmount = _progressShown;
                if (progressText != null)
                {
                    int pct = done >= total ? 100 : Mathf.Min(99, Mathf.FloorToInt(goal * 100f));
                    if (pct != _progressPct)
                    {
                        _progressPct = pct;
                        progressText.text = done >= total ? "MAX" : pct + "%";
                    }
                }
            }
        }

        /// <summary>
        /// Objective plank. The plank pops when the goal changes (<paramref name="key"/>, default: the text); text that
        /// only updates a number ("earn $120 more") just changes in place.
        /// </summary>
        public void SetObjective(string text, string key = null)
        {
            if (text == _objective) return;
            _objective = text;
            if (objectivePanel == null) return;
            bool show = !string.IsNullOrEmpty(text);
            string k = key ?? text;
            if (show)
            {
                bool pop = k != _objectiveKey || !objectivePanel.gameObject.activeSelf;
                objectiveText.text = text;
                objectivePanel.gameObject.SetActive(true);
                if (pop) Tweener.Scale(objectivePanel, Vector3.one * 0.6f, Vector3.one, 0.35f, Ease.OutBack);
            }
            else objectivePanel.gameObject.SetActive(false);
            _objectiveKey = show ? k : null;
        }

        /// <summary>Short message that drops in under the objective, then slides away.</summary>
        public void Toast(string msg, Sprite icon = null, float hold = 2.6f)
        {
            if (toastPanel == null) return;
            int serial = ++_toastSerial;
            toastText.text = msg;
            if (toastIcon != null)
            {
                toastIcon.sprite = icon;
                toastIcon.enabled = icon != null;
            }
            toastPanel.gameObject.SetActive(true);
            Tweener.Kill(toastPanel);
            var hidden = _toastShown + new Vector2(0f, 160f);
            Tweener.Value(toastPanel, 0.35f, t => toastPanel.anchoredPosition = Vector2.LerpUnclamped(hidden, _toastShown, Ease.OutBack(t)));
            if (toastIcon != null) Tweener.Scale(toastIcon.rectTransform, Vector3.zero, Vector3.one, 0.5f, Ease.OutElastic, null, 0.1f);
            Tweener.Delay(hold, () =>
            {
                if (serial != _toastSerial || toastPanel == null) return;
                Tweener.Value(toastPanel, 0.25f, t => toastPanel.anchoredPosition = Vector2.Lerp(_toastShown, hidden, t),
                    () => { if (serial == _toastSerial) toastPanel.gameObject.SetActive(false); });
            });
        }

        /// <summary>Coins fly from a world position to the money counter.</summary>
        public void FlyCoins(Vector3 worldPos, int count)
        {
            if (coinTemplate == null || coinLayer == null || moneyIcon == null) return;
            var cam = GameRefs.I != null ? GameRefs.I.mainCamera : Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) return;
            FlyCoinsFromScreen(sp, count);
        }

        /// <summary>Coins fly from a screen position (e.g. a reward button) to the money counter.</summary>
        public void FlyCoinsFromScreen(Vector2 screenPos, int count)
        {
            if (coinTemplate == null || coinLayer == null || moneyIcon == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(coinLayer, screenPos, null, out var from);
            Vector2 to = coinLayer.InverseTransformPoint(moneyIcon.position);

            count = Mathf.Min(count, 12 - _flying);
            for (int i = 0; i < count; i++)
            {
                var img = _coinPool.Count > 0 ? _coinPool.Pop() : Instantiate(coinTemplate, coinLayer);
                img.gameObject.SetActive(true);
                var rt = img.rectTransform;
                Vector2 start = from + Random.insideUnitCircle * 40f;
                Vector2 mid = Vector2.Lerp(start, to, 0.3f) + new Vector2(Random.Range(-160f, 160f), Random.Range(80f, 220f));
                rt.anchoredPosition = start;
                rt.localScale = Vector3.one * 0.6f;
                _flying++;
                float dur = Random.Range(0.45f, 0.6f);
                float delay = i * 0.04f;
                Tweener.Value(rt, dur, t =>
                {
                    float e = Ease.InOutQuad(t);
                    // Quadratic bezier for a nice swoop.
                    Vector2 a = Vector2.Lerp(start, mid, e), b = Vector2.Lerp(mid, to, e);
                    rt.anchoredPosition = Vector2.Lerp(a, b, e);
                    float s = t < 0.2f ? Mathf.Lerp(0.6f, 1.1f, t / 0.2f) : Mathf.Lerp(1.1f, 0.7f, (t - 0.2f) / 0.8f);
                    rt.localScale = Vector3.one * s;
                    rt.localRotation = Quaternion.Euler(0f, 0f, t * 360f);
                }, () =>
                {
                    img.gameObject.SetActive(false);
                    _coinPool.Push(img);
                    _flying--;
                    Tweener.Punch(moneyIcon, 0.22f, 0.18f, Vector3.one);
                    Sfx.Play(SfxId.Coin, 0.16f, Random.Range(1.2f, 1.5f));
                }, delay);
            }
        }

        void ToggleSound()
        {
            Sfx.Muted = !Sfx.Muted;
            RefreshSound();
            Sfx.Play(SfxId.Click);
            if (soundButton != null) Tweener.Punch(soundButton.transform, 0.2f, 0.2f, Vector3.one);
        }

        void RefreshSound()
        {
            if (soundIcon != null) soundIcon.sprite = Sfx.Muted ? soundOff : soundOn;
        }
    }
}
