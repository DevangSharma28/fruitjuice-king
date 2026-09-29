using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>"JUICE KING!" world-complete celebration: lifetime stats counting up and the door to the next world.</summary>
    public class CompletionPopup : MonoBehaviour
    {
        public RectTransform window;
        public Image dim;
        public RectTransform crown;
        public RectTransform rays;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI subText;
        [Tooltip("Value labels: earnings, juice produced, fruit harvested, customers served, expansion.")]
        public TextMeshProUGUI[] statValues;
        public RectTransform[] statRows;
        public Button enterButton;
        public TextMeshProUGUI enterText;
        public Button stayButton;
        [Tooltip("The panel's painted X: same as 'Stay a bit longer'.")]
        public Button closeButton;

        Action _onEnter, _onStay;
        bool _open;
        long[] _targets = new long[4];

        public bool IsOpen => _open;

        void Awake()
        {
            enterButton.onClick.AddListener(() => Close(_onEnter));
            if (stayButton != null) stayButton.onClick.AddListener(() => Close(_onStay));
            if (closeButton != null) closeButton.onClick.AddListener(() => Close(_onStay));
            gameObject.SetActive(false);
        }

        public void Show(LifetimeStats s, int nextWorld, Action onEnter, Action onStay)
        {
            if (_open) return;
            _open = true;
            _onEnter = onEnter;
            _onStay = onStay;
            InputJoystick.Blocked = true;
            Platform.Pause("completion");
            gameObject.SetActive(true);

            titleText.text = "JUICE KING!";
            subText.text = "You built the ultimate juice shop!";
            string world = nextWorld == 1 ? "TROPICAL FARM" : "NEW WORLD";
            if (enterText != null) enterText.text = "ENTER " + world;

            _targets[0] = s.earned;
            _targets[1] = s.juiceMade;
            _targets[2] = s.fruitHarvested;
            _targets[3] = s.customersServed;
            for (int i = 0; i < statValues.Length; i++)
                if (statValues[i] != null) statValues[i].text = i < 4 ? "0" : world;

            Tweener.Scale(window, Vector3.one * 0.3f, Vector3.one, 0.55f, Ease.OutBack);
            if (crown != null) Tweener.Scale(crown, Vector3.zero, Vector3.one, 0.8f, Ease.OutElastic, null, 0.25f);
            if (dim != null)
            {
                var c = dim.color;
                float a = c.a;
                Tweener.Value(dim.transform, 0.3f, t => dim.color = new Color(c.r, c.g, c.b, a * t));
            }

            // Rows pop in one by one, then their numbers count up.
            for (int i = 0; i < (statRows != null ? statRows.Length : 0); i++)
            {
                var row = statRows[i];
                if (row == null) continue;
                row.localScale = Vector3.zero;
                int idx = i;
                float delay = 0.45f + i * 0.14f;
                Tweener.Scale(row, Vector3.zero, Vector3.one, 0.35f, Ease.OutBack, null, delay);
                if (idx < 4 && idx < statValues.Length && statValues[idx] != null)
                {
                    var label = statValues[idx];
                    long target = _targets[idx];
                    Tweener.Value(label.transform, 0.9f, t =>
                    {
                        long v = (long)(target * Ease.OutCubic(t));
                        label.text = idx == 0 ? "$" + Economy.Money(v) : Economy.Money(v);
                    }, () =>
                    {
                        Sfx.Play(SfxId.Pop, 0.3f, 1.2f + idx * 0.1f);
                        Tweener.Punch(label.transform, 0.25f, 0.2f, Vector3.one);
                    }, delay + 0.15f);
                }
            }
            Tweener.Scale(enterButton.transform, Vector3.zero, Vector3.one, 0.45f, Ease.OutBack, null, 1.3f);

            Sfx.Play(SfxId.Fanfare, 0.8f);
            Tweener.Delay(0.35f, () => Sfx.Play(SfxId.Sparkle, 0.5f));
            if (GameRefs.I != null && GameRefs.I.player != null)
            {
                var p = GameRefs.I.player.transform.position + Vector3.up * 2f;
                Fx.Confetti(p, 90);
                Fx.Stars(p, 24);
            }
            if (HUD.I != null)
                Tweener.Delay(0.8f, () => { if (HUD.I != null) HUD.I.FlyCoinsFromScreen(new Vector2(Screen.width * 0.5f, Screen.height * 0.55f), 10); });
        }

        void Update()
        {
            if (!_open) return;
            float t = Time.unscaledTime;
            if (rays != null) rays.localRotation = Quaternion.Euler(0f, 0f, -t * 18f);
            if (crown != null && crown.localScale.x > 0.98f) crown.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.4f) * 6f);
            if (enterButton.transform.localScale.x > 0.95f)
            {
                float s = 1f + Mathf.Sin(t * 5f) * 0.04f;
                enterButton.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        void Close(Action then)
        {
            if (!_open) return;
            _open = false;
            Platform.Resume("completion");
            InputJoystick.Blocked = false;
            Sfx.Play(SfxId.Click, 0.5f);
            Tweener.Scale(window, Vector3.one, Vector3.one * 0.6f, 0.18f, Ease.InQuad, () =>
            {
                gameObject.SetActive(false);
                window.localScale = Vector3.one;
                then?.Invoke();
            });
        }
    }
}
