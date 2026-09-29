using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>Small settings card: sound effects and ambience toggles.</summary>
    public class SettingsPopup : MonoBehaviour
    {
        public static SettingsPopup I { get; private set; }

        public RectTransform window;
        public Button openButton;
        public Button closeButton;
        public Button dimButton;
        public Image soundToggle;
        public Image ambienceToggle;
        public Button soundButton;
        public Button ambienceButton;
        public Sprite toggleOn, toggleOff;

        bool _open;

        void Awake()
        {
            I = this;
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimButton != null) dimButton.onClick.AddListener(Close);
            soundButton.onClick.AddListener(() =>
            {
                Sfx.Muted = !Sfx.Muted;
                Sfx.Play(SfxId.Click, 0.6f);
                Refresh(true);
            });
            ambienceButton.onClick.AddListener(() =>
            {
                Sfx.AmbienceOn = !Sfx.AmbienceOn;
                Sfx.Play(SfxId.Click, 0.6f);
                Refresh(true);
            });
            gameObject.SetActive(false);
        }

        public void Open()
        {
            if (_open) return;
            _open = true;
            gameObject.SetActive(true);
            Refresh(false);
            Platform.Pause("settings");
            Tweener.Scale(window, Vector3.one * 0.5f, Vector3.one, 0.35f, Ease.OutBack);
            Sfx.Play(SfxId.Whoosh, 0.3f, 1.3f);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            Platform.Resume("settings");
            Sfx.Play(SfxId.Click, 0.5f);
            Tweener.Scale(window, window.localScale, Vector3.zero, 0.16f, Ease.InQuad, () =>
            {
                if (!_open) gameObject.SetActive(false);
            });
        }

        void Refresh(bool punch)
        {
            soundToggle.sprite = Sfx.Muted ? toggleOff : toggleOn;
            ambienceToggle.sprite = Sfx.AmbienceOn ? toggleOn : toggleOff;
            if (punch)
            {
                Tweener.Punch(soundToggle.rectTransform, 0.15f, 0.2f, Vector3.one);
                Tweener.Punch(ambienceToggle.rectTransform, 0.15f, 0.2f, Vector3.one);
            }
        }
    }
}
