using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>
    /// Boot loading screen. The logo pops in over the key art, the bar at the bottom fills with real progress while the
    /// sounds are synthesised and the saved world loads in the background. Once the world is running underneath, the
    /// screen fades away into the game. World changes come back through here too (<see cref="LoadSavedWorld"/>).
    /// Animation is self-contained (no Tweener): the screen outlives the scene it was loaded in.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public const string BootScene = "Boot";

        /// <summary>True from boot until the screen has faded out over the game.</summary>
        public static bool Busy { get; private set; }

        public CanvasGroup group;
        public RectTransform background;
        public RectTransform logo;
        public CanvasGroup logoGroup;
        [Tooltip("Diagonal highlight that sweeps across the logo (clipped by the logo's mask).")]
        public RectTransform logoShine;
        public Image barFill;
        [Tooltip("Glossy top half of the fill (filled in step with it).")]
        public Image barFillGloss;
        [Tooltip("Glint riding on the front of the fill.")]
        public RectTransform barHead;
        public RectTransform barRoot;
        public TextMeshProUGUI percentText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI tipText;
        [Tooltip("Shortest time on screen, so the logo animation reads even when loading is instant.")]
        public float minDuration = 2.6f;
        public float fadeOutTime = 0.6f;
        [TextArea] public string[] tips;

        static readonly string[] LoadingTexts = { "LOADING", "LOADING.", "LOADING..", "LOADING..." };
        int _dots = -1;
        float _shown, _target, _t0;
        int _pct = -1;
        int _tip = -1;
        float _tipT;
        bool _leaving;
        float _leaveT;
        Vector2 _logoBase;
        float _barWidth;

        /// <summary>Show the loading screen and bring up the world the save is in.</summary>
        public static void LoadSavedWorld() => SceneManager.LoadScene(BootScene);

        void Awake()
        {
            Busy = true;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (logo != null) _logoBase = logo.anchoredPosition;
            if (barFill != null) _barWidth = ((RectTransform)barFill.transform).rect.width;
            SetFill(0f);
            if (group != null) group.alpha = 0f;
            ScreenFader.ClearStartBlack();
        }

        IEnumerator Start()
        {
            _t0 = Time.unscaledTime;
            // One frame so the screen is on before any heavy work.
            yield return null;

            // Sounds are synthesised in code: do it here, not in the first second of play.
            _target = 0.08f;
            yield return null;
            Sfx.Warmup();
            _target = 0.3f;
            yield return null;

            string scene = ExpansionManager.SceneName(GameManager.PeekSavedExpansion());
            var prio = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            var op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
            {
                _target = 0.3f + op.progress / 0.9f * 0.6f;
                yield return null;
            }
            _target = 0.92f;
            while (Time.unscaledTime - _t0 < minDuration) yield return null;
            _target = 1f;
            while (_shown < 0.995f) yield return null;
            yield return new WaitForSecondsRealtime(0.12f);

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            Application.backgroundLoadingPriority = prio;
            // The world's first frames (Awake/Start, lazy set-up) run while the screen still covers them.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.15f);
            _leaving = true;
            _leaveT = 0f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Time.unscaledTime - _t0;

            if (!_leaving)
            {
                if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, 1f, dt * 4f);
            }
            else
            {
                _leaveT += dt;
                float k = Mathf.Clamp01(_leaveT / fadeOutTime);
                if (group != null) group.alpha = 1f - k * k;
                if (logo != null)
                {
                    logo.localScale = Vector3.one * (1f + Ease.OutCubic(k) * 0.18f);
                    logo.anchoredPosition = _logoBase + new Vector2(0f, Ease.OutCubic(k) * 60f);
                }
                if (k >= 1f)
                {
                    Busy = false;
                    Destroy(gameObject);
                }
                return;
            }

            // Background drifts in slowly.
            if (background != null) background.localScale = Vector3.one * (1.04f + Mathf.Min(t, 12f) * 0.004f);

            // Logo: pop in with a bounce, then breathe and bob.
            if (logo != null)
            {
                float intro = Mathf.Clamp01((t - 0.15f) / 0.75f);
                float pop = intro <= 0f ? 0f : Ease.OutBack(intro);
                float breathe = 1f + Mathf.Sin(t * 2.2f) * 0.018f * intro;
                logo.localScale = Vector3.one * (Mathf.Lerp(0.35f, 1f, pop) * breathe);
                logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-7f, 0f, pop) + Mathf.Sin(t * 1.3f) * 1.2f * intro);
                logo.anchoredPosition = _logoBase + new Vector2(0f, Mathf.Sin(t * 1.7f) * 10f * intro + (1f - pop) * 120f);
                if (logoGroup != null) logoGroup.alpha = Mathf.Clamp01(intro * 3f);
            }
            // A shine sweeps across the logo every couple of seconds.
            if (logoShine != null && logo != null)
            {
                float cycle = Mathf.Repeat(t - 0.9f, 2.4f) / 0.7f;
                float w = logo.rect.width;
                logoShine.gameObject.SetActive(t > 0.9f && cycle <= 1f);
                logoShine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.7f, w * 0.7f, cycle), 0f);
            }

            // Bar: ease toward the real progress, never backwards.
            _shown = Mathf.MoveTowards(_shown, _target, dt * (_target >= 1f ? 1.6f : 0.9f));
            SetFill(_shown);

            if (statusText != null)
            {
                int dots = (int)(t * 2.5f) % 4;
                if (dots != _dots)
                {
                    _dots = dots;
                    statusText.text = LoadingTexts[dots];
                }
            }

            if (tipText != null && tips != null && tips.Length > 0)
            {
                _tipT -= dt;
                if (_tipT <= 0f)
                {
                    _tipT = 2.6f;
                    int next = tips.Length == 1 ? 0 : Random.Range(0, tips.Length);
                    if (next == _tip) next = (next + 1) % tips.Length;
                    _tip = next;
                    tipText.text = tips[_tip];
                }
                // Fade each tip in and out.
                float a = Mathf.Clamp01(Mathf.Min((2.6f - _tipT) * 3f, _tipT * 3f));
                var c = tipText.color;
                c.a = a;
                tipText.color = c;
            }
        }

        void SetFill(float p)
        {
            if (barFill != null) barFill.fillAmount = p;
            if (barFillGloss != null) barFillGloss.fillAmount = p;
            if (barHead != null)
            {
                barHead.anchoredPosition = new Vector2(_barWidth * p, barHead.anchoredPosition.y);
                barHead.gameObject.SetActive(p > 0.02f && p < 0.995f);
            }
            int pct = Mathf.RoundToInt(p * 100f);
            if (pct != _pct && percentText != null)
            {
                _pct = pct;
                percentText.text = pct + "%";
                if (pct == 100 && barRoot != null) StartCoroutine(Punch(barRoot));
            }
        }

        static IEnumerator Punch(RectTransform t)
        {
            float k = 0f;
            while (k < 1f)
            {
                k += Time.unscaledDeltaTime / 0.3f;
                t.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.06f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        void OnDestroy() => Busy = false;
    }
}
