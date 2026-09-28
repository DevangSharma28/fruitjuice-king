using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>Money counter, objective banner and sound toggle.</summary>
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

        double _shown;
        string _objective;

        void Awake() => I = this;

        void Start()
        {
            var gm = GameManager.I;
            _shown = gm.Money;
            moneyText.text = UnlockZone.Format(gm.Money);
            gm.MoneyChanged += OnMoney;
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            RefreshSound();
            SetObjective(null);
        }

        void OnDestroy()
        {
            if (GameManager.I != null) GameManager.I.MoneyChanged -= OnMoney;
        }

        void OnMoney(long value, long delta)
        {
            if (delta > 0 && moneyIcon != null) Tweener.Punch(moneyIcon, 0.25f, 0.2f, Vector3.one);
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
        }

        public void SetObjective(string text)
        {
            if (text == _objective) return;
            _objective = text;
            if (objectivePanel == null) return;
            bool show = !string.IsNullOrEmpty(text);
            if (show)
            {
                objectiveText.text = text;
                objectivePanel.gameObject.SetActive(true);
                Tweener.Scale(objectivePanel, Vector3.one * 0.6f, Vector3.one, 0.35f, Ease.OutBack);
            }
            else objectivePanel.gameObject.SetActive(false);
        }

        void ToggleSound()
        {
            Sfx.Muted = !Sfx.Muted;
            RefreshSound();
            Sfx.Play(SfxId.Click);
        }

        void RefreshSound()
        {
            if (soundIcon != null) soundIcon.sprite = Sfx.Muted ? soundOff : soundOn;
        }
    }
}
