using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuiceKing
{
    /// <summary>
    /// World progression. In the original farm it watches for completion (every pad unlocked, every upgrade maxed)
    /// and offers the Tropical Farm; in an expansion scene it runs the first-visit intro.
    /// </summary>
    public class ExpansionManager : MonoBehaviour
    {
        public static ExpansionManager I { get; private set; }

        /// <summary>Scene of each world, in build-settings order.</summary>
        static readonly string[] Scenes = { "JuiceKing", "Tropical", "Berry" };

        public static int WorldCount => Scenes.Length;
        public static string SceneName(int expansion) => Scenes[Mathf.Clamp(expansion, 0, Scenes.Length - 1)];

        [Tooltip("World this scene offers once it is finished (-1 = none).")]
        public int nextExpansion = 1;
        public CompletionPopup completionPopup;
        [Tooltip("HUD button that re-opens the completion popup after 'Stay a bit longer'.")]
        public GameObject nextWorldButton;
        public ExpansionIntro intro;

        bool _offered;
        bool _leaving;
        float _checkT;

        void Awake() => I = this;

        void Start()
        {
            if (GameManager.Redirecting) return;
            if (nextWorldButton != null)
            {
                nextWorldButton.SetActive(false);
                var b = nextWorldButton.GetComponent<UnityEngine.UI.Button>();
                if (b != null) b.onClick.AddListener(ShowCompletion);
            }
            ScreenFader.FadeInIfBlack(0.6f);
            if (intro != null && !GameManager.I.data.introSeen) StartCoroutine(PlayIntroWhenVisible());
        }

        /// <summary>Wait for the loading screen to fade away so the fly-over is seen from its first shot.</summary>
        System.Collections.IEnumerator PlayIntroWhenVisible()
        {
            while (LoadingScreen.Busy) yield return null;
            intro.Play();
        }

        void Update()
        {
            if (_offered || _leaving || completionPopup == null) return;
            // The last world celebrates once (its reward is a one-time grant).
            if (nextExpansion < 0 && GameManager.I.data.world2Complete) return;
            _checkT -= Time.unscaledDeltaTime;
            if (_checkT > 0f) return;
            _checkT = 1f;
            if (!IsComplete()) return;
            _offered = true;
            Analytics.Log(Analytics.WorldComplete, "next", nextExpansion);
            StartCoroutine(ShowWhenClear());
        }

        /// <summary>Let the last unlock / upgrade celebration finish and wait for other popups to close.</summary>
        System.Collections.IEnumerator ShowWhenClear()
        {
            yield return new WaitForSeconds(2.2f);
            while (Platform.Paused || ExpansionIntro.Playing || CameraFollow.Busy || (UpgradePanel.I != null && UpgradePanel.I.IsOpen))
                yield return null;
            ShowCompletion();
        }

        /// <summary>Every pad of this world is unlocked and every upgrade is maxed.</summary>
        public static bool IsComplete()
        {
            int total = UnlockManager.TotalCount;
            if (total == 0 || UnlockManager.UnlockedCount < total) return false;
            return Upgrades.AllMaxed(GameManager.I.data.expansion);
        }

        /// <summary>World progress: unlocked pads plus bought upgrade levels, out of all pads plus all levels.</summary>
        public static void Progress(out int done, out int total)
        {
            done = UnlockManager.UnlockedCount;
            total = UnlockManager.TotalCount;
            if (GameManager.I == null) return;
            foreach (var d in Upgrades.ForWorld(GameManager.I.data.expansion))
            {
                done += Mathf.Min(d.Level(), d.maxLevel);
                total += d.maxLevel;
            }
        }

        public void ShowCompletion()
        {
            if (_leaving || completionPopup == null) return;
            if (nextWorldButton != null) nextWorldButton.SetActive(false);
            if (nextExpansion < 0)
            {
                completionPopup.Show(GameManager.I.data.stats, -1, ClaimFinal, ClaimFinal);
                return;
            }
            completionPopup.Show(GameManager.I.data.stats, nextExpansion, () => EnterExpansion(nextExpansion), OnStay);
        }

        /// <summary>The last world is done: a one-time Golden Apple reward, then the player keeps playing.</summary>
        void ClaimFinal()
        {
            var gm = GameManager.I;
            if (gm == null || gm.data.world2Complete) return;
            gm.data.world2Complete = true;
            gm.Save();
            var p = GameRefs.I != null && GameRefs.I.player != null ? GameRefs.I.player.transform.position + Vector3.up * 2f : Vector3.zero;
            AppleFx.Reward(p, Economy.ApplesFinalCompletion);
            Fx.Confetti(p, 80);
            Haptics.Play(HapticKind.Success);
        }

        void OnStay()
        {
            if (nextWorldButton == null) return;
            nextWorldButton.SetActive(true);
            Tweener.Scale(nextWorldButton.transform, Vector3.zero, Vector3.one, 0.5f, Ease.OutBack);
        }

        /// <summary>Archive this world, reset per-world progress and load the next world's scene.</summary>
        public void EnterExpansion(int expansion)
        {
            if (_leaving) return;
            _leaving = true;
            InputJoystick.Block("expansion", true);
            Sfx.Play(SfxId.Whoosh, 0.5f, 0.8f);
            Analytics.Log(Analytics.WorldEnter, "to", expansion);
            ScreenFader.FadeOut(0.8f, () =>
            {
                GameManager.I.BeginExpansion(expansion, Economy.StartMoney(expansion));
                // A little premium welcome for every new world.
                GameManager.I.AddApples(expansion >= 2 ? 5 : 3);
                InputJoystick.Block("expansion", false);
                // The loading screen takes over from the black fade and brings up the new world.
                LoadingScreen.LoadSavedWorld();
            });
        }

        /// <summary>Debug (F10): open the completion popup without finishing the world.</summary>
        public static void DebugComplete()
        {
            if (I == null || I.completionPopup == null)
            {
                Debug.Log("[JuiceKing] No completion popup in this world.");
                return;
            }
            I._offered = true;
            I.ShowCompletion();
        }
    }
}
