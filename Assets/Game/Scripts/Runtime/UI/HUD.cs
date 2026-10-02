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
        [Tooltip("Glossy top half of the progress fill (filled in step with it).")]
        public Image progressGloss;
        public TextMeshProUGUI progressText;
        public RectTransform progressPanel;
        [Tooltip("Glint that sweeps across the filled bar when progress grows (and now and then while idle).")]
        public RectTransform progressShine;

        [Header("Golden Apples")]
        public TextMeshProUGUI applesText;
        public RectTransform applesIcon;
        public RectTransform applesPanel;
        [Tooltip("Flying apple (disabled template, cloned and pooled).")]
        public Image appleTemplate;
        [Tooltip("\"+\" beside the apples: opens the shop on the Golden Apple packs.")]
        public Button applesShopButton;

        [Header("Ad Tickets")]
        public TextMeshProUGUI ticketsText;
        public Image ticketsIcon;
        public RectTransform ticketsPanel;
        public Button ticketsShopButton;

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
        float _lastGoal = -1f;
        float _shineT = -1f;
        float _shineIdle = 2.5f;
        readonly Stack<Image> _coinPool = new Stack<Image>();
        readonly Stack<Image> _applePool = new Stack<Image>();
        int _applesShown = -1;
        int _applesFlying;
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
            if (appleTemplate != null) appleTemplate.gameObject.SetActive(false);
        }

        void Start()
        {
            var gm = GameManager.I;
            _shown = gm.Money;
            moneyText.text = UnlockZone.Format(gm.Money);
            gm.MoneyChanged += OnMoney;
            gm.ApplesChanged += OnApples;
            SetApples(gm.Apples);
            gm.TicketsChanged += OnTickets;
            SetTickets(gm.Tickets);
            if (applesShopButton != null) applesShopButton.onClick.AddListener(() => OpenShop(ShopSection.Apples));
            if (ticketsShopButton != null) ticketsShopButton.onClick.AddListener(() => OpenShop(ShopSection.Tickets));
            UnlockManager.ZoneUnlocked += OnUnlocked;
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            RefreshSound();
            SetObjective(null);
            Sfx.StartAmbient();
            Platform.NotifyLoaded();
        }

        void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.MoneyChanged -= OnMoney;
                GameManager.I.ApplesChanged -= OnApples;
                GameManager.I.TicketsChanged -= OnTickets;
            }
            UnlockManager.ZoneUnlocked -= OnUnlocked;
        }

        void OnMoney(long value, long delta)
        {
            if (delta > 0 && moneyIcon != null && _flying == 0) Tweener.Punch(moneyIcon, 0.25f, 0.2f, Vector3.one);
        }

        void OnApples(int value, int delta)
        {
            // While apples are flying in, the counter ticks up as each one lands.
            if (delta > 0 && _applesFlying > 0) return;
            SetApples(value);
            if (applesPanel != null) Tweener.Punch(applesPanel, delta > 0 ? 0.2f : 0.12f, 0.3f, Vector3.one);
        }

        static void OpenShop(ShopSection section)
        {
            if (ShopPopup.I == null) return;
            Sfx.Play(SfxId.Click, 0.5f);
            ShopPopup.I.Open(section);
        }

        void OnTickets(int value, int delta)
        {
            SetTickets(value);
            if (ticketsPanel != null) Tweener.Punch(ticketsPanel, delta > 0 ? 0.2f : 0.12f, 0.3f, Vector3.one);
            // A ticket was spent on a rewarded ad: say so, since no video played.
            if (delta < 0) Toast("Ad Ticket used! " + value + " left", ticketsIcon != null ? ticketsIcon.sprite : null, 2f);
        }

        void SetTickets(int v)
        {
            if (ticketsText != null) ticketsText.text = v.ToString("N0");
        }

        void SetApples(int v)
        {
            if (applesText == null || v == _applesShown) return;
            _applesShown = v;
            applesText.text = v.ToString("N0");
        }

        /// <summary>Golden Apples fly from a world point to the apple counter; the counter ticks up as they land.</summary>
        public void FlyApples(Vector3 worldPos, int count)
        {
            var cam = GameRefs.I != null ? GameRefs.I.mainCamera : Camera.main;
            if (appleTemplate == null || coinLayer == null || applesIcon == null || cam == null)
            {
                SetApples(GameManager.I.Apples);
                return;
            }
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) sp = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(coinLayer, sp, null, out var from);
            Vector2 to = coinLayer.InverseTransformPoint(applesIcon.position);
            int start = Mathf.Max(0, GameManager.I.Apples - count);
            SetApples(start);
            int shown = Mathf.Min(count, 8);
            for (int i = 0; i < shown; i++)
            {
                var img = _applePool.Count > 0 ? _applePool.Pop() : Instantiate(appleTemplate, coinLayer);
                img.gameObject.SetActive(true);
                var rt = img.rectTransform;
                Vector2 s0 = from + Random.insideUnitCircle * 50f;
                Vector2 mid = Vector2.Lerp(s0, to, 0.35f) + new Vector2(Random.Range(-200f, 200f), Random.Range(120f, 260f));
                rt.anchoredPosition = s0;
                rt.localScale = Vector3.zero;
                _applesFlying++;
                float dur = Random.Range(0.7f, 0.85f);
                float delay = 0.15f + i * 0.09f;
                int landValue = i == shown - 1 ? GameManager.I.Apples : start + Mathf.CeilToInt((i + 1) * count / (float)shown);
                float pitch = 1.1f + i * 0.06f;
                Tweener.Value(rt, dur, t =>
                {
                    float e = Ease.InOutQuad(t);
                    Vector2 a = Vector2.Lerp(s0, mid, e), b = Vector2.Lerp(mid, to, e);
                    rt.anchoredPosition = Vector2.Lerp(a, b, e);
                    float sc = t < 0.25f ? Mathf.Lerp(0f, 1.35f, Ease.OutBack(t / 0.25f)) : Mathf.Lerp(1.35f, 0.75f, (t - 0.25f) / 0.75f);
                    rt.localScale = Vector3.one * sc;
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 18f);
                }, () =>
                {
                    img.gameObject.SetActive(false);
                    _applePool.Push(img);
                    _applesFlying--;
                    // The last one lands on the live balance (apples may have been spent or bought meanwhile).
                    SetApples(_applesFlying == 0 ? GameManager.I.Apples : Mathf.Min(landValue, GameManager.I.Apples));
                    if (applesIcon != null) Tweener.Punch(applesIcon, 0.3f, 0.22f, Vector3.one);
                    Sfx.Play(SfxId.Chime, 0.25f, pitch);
                }, delay);
            }
        }

        void OnUnlocked(UnlockZone z)
        {
            if (progressPanel != null) Tweener.Punch(progressPanel, 0.15f, 0.35f, Vector3.one);
        }

        float _progressT;
        int _progressDone, _progressTotal;

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
                // Counted a few times a second (it walks every pad and upgrade), animated every frame.
                _progressT -= Time.unscaledDeltaTime;
                if (_progressT <= 0f)
                {
                    _progressT = 0.25f;
                    ExpansionManager.Progress(out _progressDone, out _progressTotal);
                }
                int done = _progressDone;
                int total = Mathf.Max(1, _progressTotal);
                float goal = done / (float)total;
                _progressShown = _progressShown < 0f ? goal : Mathf.MoveTowards(_progressShown, goal, Time.deltaTime * 0.6f);
                progressFill.fillAmount = _progressShown;
                if (progressGloss != null) progressGloss.fillAmount = _progressShown;
                if (_lastGoal >= 0f && goal > _lastGoal + 0.0001f) _shineT = 0f;
                _lastGoal = goal;
                UpdateShine();
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

        void UpdateShine()
        {
            if (progressShine == null) return;
            float dt = Time.unscaledDeltaTime;
            if (_shineT < 0f)
            {
                _shineIdle -= dt;
                if (_shineIdle > 0f || _progressShown <= 0.02f)
                {
                    if (progressShine.gameObject.activeSelf) progressShine.gameObject.SetActive(false);
                    return;
                }
                _shineT = 0f;
            }
            _shineIdle = 7f;
            if (!progressShine.gameObject.activeSelf) progressShine.gameObject.SetActive(true);
            _shineT += dt;
            float k = Mathf.Clamp01(_shineT / 0.8f);
            k = k * k * (3f - 2f * k);
            // The fill masks the glint, so it only shows across the gold part of the bar.
            float w = ((RectTransform)progressShine.parent).rect.width;
            progressShine.anchoredPosition = new Vector2(Mathf.Lerp(-50f, w * Mathf.Max(0.1f, _progressShown) + 50f, k), 0f);
            if (_shineT >= 0.8f)
            {
                _shineT = -1f;
                progressShine.gameObject.SetActive(false);
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
            // The NEW! banner uses the same spot: show the toast once it has gone.
            float wait = UnlockBanner.BusyUntil - Time.unscaledTime;
            if (wait > 0f)
            {
                Tweener.Delay(wait + 0.05f, () => { if (this != null) Toast(msg, icon, hold); });
                return;
            }
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

        /// <summary>Slide the toast away now (the unlock banner is taking its place).</summary>
        public void HideToast()
        {
            if (toastPanel == null || !toastPanel.gameObject.activeSelf) return;
            _toastSerial++;
            Tweener.Kill(toastPanel);
            toastPanel.gameObject.SetActive(false);
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
