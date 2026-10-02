using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>
    /// Small settings card: sound effects and ambience toggles. In the Editor and development builds it also shows a
    /// DEBUG row that jumps between the three worlds (<see cref="GameManager.DebugSwitchWorld"/>).
    /// </summary>
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
        public Image vibrationToggle;
        public Button vibrationButton;
        [Tooltip("Restore Purchases (also in the shop): brings VIP back on a new device.")]
        public Button restoreButton;
        [Tooltip("Privacy policy / consent options. Hidden until a policy URL or a consent provider exists.")]
        public Button privacyButton;
        public TMPro.TextMeshProUGUI versionText;

        [Header("Debug (Editor / development builds)")]
        public GameObject debugRoot;
        [Tooltip("One button per world, in world order.")]
        public Button[] debugWorldButtons;
        [Tooltip("Window height the debug row adds (removed in release builds).")]
        public float debugHeight;

        bool _open;
        float? _restoreX;

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
            if (vibrationButton != null)
                vibrationButton.onClick.AddListener(() =>
                {
                    Haptics.Enabled = !Haptics.Enabled;
                    Sfx.Play(SfxId.Click, 0.6f);
                    Haptics.Play(HapticKind.Medium);
                    Refresh(true);
                });
            if (restoreButton != null) restoreButton.onClick.AddListener(OnRestore);
            if (privacyButton != null) privacyButton.onClick.AddListener(OnPrivacy);
            if (versionText != null) versionText.text = "v" + Application.version;
            bool debug = Debug.isDebugBuild;
            if (debugRoot != null) debugRoot.SetActive(debug);
            if (!debug && window != null) window.sizeDelta -= new Vector2(0f, debugHeight);
            if (debug && debugWorldButtons != null)
                for (int i = 0; i < debugWorldButtons.Length; i++)
                {
                    int world = i;
                    debugWorldButtons[i].onClick.AddListener(() => SwitchWorld(world));
                }
            gameObject.SetActive(false);
        }

        public void Open()
        {
            if (_open) return;
            _open = true;
            gameObject.SetActive(true);
            Refresh(false);
            if (privacyButton != null)
            {
                bool privacy = !string.IsNullOrEmpty(ReleaseConfig.PrivacyPolicyUrl) || Privacy.PrivacyOptionsAvailable;
                privacyButton.gameObject.SetActive(privacy);
                // Alone, Restore Purchases sits in the middle of the row.
                if (restoreButton != null)
                {
                    var rt = (RectTransform)restoreButton.transform;
                    if (_restoreX == null) _restoreX = rt.anchoredPosition.x;
                    rt.anchoredPosition = new Vector2(privacy ? _restoreX.Value : 0f, rt.anchoredPosition.y);
                }
            }
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

        void OnRestore()
        {
            Sfx.Play(SfxId.Click, 0.5f);
            if (HUD.I != null) HUD.I.Toast("Restoring purchases...", null, 1.5f);
            Iap.Restore(ok =>
            {
                if (HUD.I == null) return;
                bool owned = GameManager.I != null && GameManager.I.NoAds;
                HUD.I.Toast(!ok ? "Could not restore purchases. Check your connection." : owned ? "Purchases restored: VIP active" : "Purchases restored. Nothing new to restore.", null, 2.6f);
                Refresh(false);
            });
        }

        void OnPrivacy()
        {
            Sfx.Play(SfxId.Click, 0.5f);
            if (Privacy.PrivacyOptionsAvailable) Privacy.ShowPrivacyOptions();
            else Privacy.OpenPrivacyPolicy();
        }

        void SwitchWorld(int world)
        {
            if (GameManager.I == null || world == GameManager.I.data.expansion) return;
            Sfx.Play(SfxId.Click, 0.6f);
            Close();
            GameManager.I.DebugSwitchWorld(world);
        }

        void Refresh(bool punch)
        {
            if (debugWorldButtons != null && GameManager.I != null)
                for (int i = 0; i < debugWorldButtons.Length; i++)
                    debugWorldButtons[i].interactable = i != GameManager.I.data.expansion;
            soundToggle.sprite = Sfx.Muted ? toggleOff : toggleOn;
            ambienceToggle.sprite = Sfx.AmbienceOn ? toggleOn : toggleOff;
            if (vibrationToggle != null) vibrationToggle.sprite = Haptics.Enabled ? toggleOn : toggleOff;
            if (punch)
            {
                Tweener.Punch(soundToggle.rectTransform, 0.15f, 0.2f, Vector3.one);
                Tweener.Punch(ambienceToggle.rectTransform, 0.15f, 0.2f, Vector3.one);
                if (vibrationToggle != null) Tweener.Punch(vibrationToggle.rectTransform, 0.15f, 0.2f, Vector3.one);
            }
        }
    }
}
