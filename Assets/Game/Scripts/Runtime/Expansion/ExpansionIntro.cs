using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>
    /// First-visit fly-over of a new world: letterbox bars, a big welcome title, then the camera tours the key areas
    /// with a caption for each before handing control to the player. Tap SKIP to jump straight in.
    /// </summary>
    public class ExpansionIntro : MonoBehaviour
    {
        public static bool Playing { get; private set; }

        [Serializable]
        public class Shot
        {
            public Transform point;
            public string caption;
            public float distance = 1.3f;
            public float hold = 1.6f;
            [Tooltip("Park the demo truck at the bay while this shot is on screen.")]
            public bool showTruck;
        }

        public RectTransform topBar, bottomBar;
        public RectTransform titleGroup;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI subtitleText;
        public RectTransform captionGroup;
        public TextMeshProUGUI captionText;
        public Button skipButton;
        [Tooltip("Wide opening shot over the whole island.")]
        public Transform openingPoint;
        public float openingDistance = 2.4f;
        public string title = "Welcome to the Tropical Juice Empire.";
        public string subtitle = "TROPICAL FARM";
        public Shot[] shots;
        [Tooltip("HUD root faded out during the fly-over.")]
        public CanvasGroup hud;
        [Tooltip("Truck parked at the (still locked) bay for the delivery shot.")]
        public DeliveryTruck demoTruck;
        public Transform demoPark;

        bool _done;
        float _barH;

        void Awake()
        {
            if (skipButton != null) skipButton.onClick.AddListener(Finish);
            gameObject.SetActive(false);
        }

        public void Play()
        {
            Playing = true;
            _done = false;
            gameObject.SetActive(true);
            InputJoystick.Blocked = true;
            Platform.Pause("intro");

            _barH = topBar != null ? topBar.sizeDelta.y : 0f;
            SlideBars(true);
            FadeHud(false);
            if (captionGroup != null) captionGroup.gameObject.SetActive(false);
            if (titleGroup != null) titleGroup.gameObject.SetActive(false);
            if (skipButton != null) Tweener.Scale(skipButton.transform, Vector3.zero, Vector3.one, 0.4f, Ease.OutBack, null, 1f);

            Vector3 open = openingPoint != null ? openingPoint.position : GameRefs.I.player.transform.position;
            CameraFollow.Peek(open, 2.6f, openingDistance, ShowTitle, HideTitle, 1.2f);
            if (shots != null)
                foreach (var s in shots)
                {
                    if (s.point == null) continue;
                    var shot = s;
                    CameraFollow.Peek(s.point.position, s.hold, s.distance, () =>
                    {
                        ShowCaption(shot.caption);
                        if (shot.showTruck) ShowDemoTruck(true);
                    }, null, 1.1f, 0.9f);
                }
            // A last beat on the player before control returns.
            CameraFollow.Peek(GameRefs.I.player.transform.position, 0.2f, 1f, () => ShowCaption(null), Finish, 1f, 0.3f);
        }

        void ShowTitle()
        {
            if (_done || titleGroup == null) return;
            titleGroup.gameObject.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (subtitleText != null) subtitleText.text = subtitle;
            Tweener.Scale(titleGroup, Vector3.one * 0.5f, Vector3.one, 0.6f, Ease.OutBack);
            Sfx.Play(SfxId.Fanfare, 0.6f);
            Sfx.Play(SfxId.Sparkle, 0.4f);
        }

        void HideTitle()
        {
            if (titleGroup == null || !titleGroup.gameObject.activeSelf) return;
            Tweener.Scale(titleGroup, Vector3.one, Vector3.zero, 0.3f, Ease.InQuad, () => titleGroup.gameObject.SetActive(false));
        }

        void ShowCaption(string text)
        {
            if (_done || captionGroup == null) return;
            if (string.IsNullOrEmpty(text))
            {
                captionGroup.gameObject.SetActive(false);
                return;
            }
            captionGroup.gameObject.SetActive(true);
            captionText.text = text;
            Tweener.Scale(captionGroup, new Vector3(0.6f, 0.6f, 1f), Vector3.one, 0.4f, Ease.OutBack);
            Sfx.Play(SfxId.Whoosh, 0.25f, 1.3f);
        }

        void SlideBars(bool show)
        {
            float from = show ? 0f : 1f, to = show ? 1f : 0f;
            if (topBar != null && bottomBar != null)
                Tweener.Value(topBar, 0.5f, t =>
                {
                    float k = Mathf.Lerp(from, to, Ease.OutQuad(t));
                    topBar.anchoredPosition = new Vector2(0f, _barH * (1f - k));
                    bottomBar.anchoredPosition = new Vector2(0f, -_barH * (1f - k));
                });
        }

        void Finish()
        {
            if (_done) return;
            _done = true;
            CameraFollow.CancelPeeks();
            GameManager.I.data.introSeen = true;
            GameManager.I.Save();
            InputJoystick.Blocked = false;
            Platform.Resume("intro");
            HideTitle();
            ShowCaptionHidden();
            ShowDemoTruck(false);
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            SlideBars(false);
            FadeHud(true);
            Tweener.Delay(0.6f, () =>
            {
                Playing = false;
                if (this != null) gameObject.SetActive(false);
            });
        }

        void FadeHud(bool show)
        {
            if (hud == null) return;
            hud.interactable = show;
            hud.blocksRaycasts = show;
            float from = hud.alpha, to = show ? 1f : 0f;
            Tweener.Value(hud.transform, show ? 0.5f : 0.25f, t => hud.alpha = Mathf.Lerp(from, to, t), null, show ? 0.3f : 0f);
        }

        bool _demoShown;

        /// <summary>A parked truck makes the "fill big trucks" shot readable even before the bay is unlocked.</summary>
        void ShowDemoTruck(bool show)
        {
            if (demoTruck == null || demoPark == null) return;
            var dm = DeliveryManager.I;
            if (dm != null && dm.Unlocked) return; // the real delivery loop owns the trucks
            if (show && !_demoShown)
            {
                _demoShown = true;
                demoTruck.ParkAt(demoPark.position, demoPark.rotation, 0.6f);
            }
            else if (!show && _demoShown)
            {
                _demoShown = false;
                demoTruck.gameObject.SetActive(false);
            }
        }

        void ShowCaptionHidden()
        {
            if (captionGroup != null) captionGroup.gameObject.SetActive(false);
        }

        void OnDestroy() => Playing = false;
    }
}
