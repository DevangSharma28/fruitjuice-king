using System;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Anything that can play a rewarded video (<see cref="AdMobProvider"/> on device). Assigned to
    /// <see cref="Ads.Provider"/> at startup by <see cref="AppServices"/>, after consent. Reads ad unit ids from
    /// <see cref="ReleaseConfig"/>.
    /// </summary>
    public interface IRewardedAdProvider
    {
        bool IsReady { get; }
        /// <summary>A video is on screen. <see cref="Ads"/> never times an ad out while it is showing: the player may sit
        /// on the end card or visit the advertiser's store page for as long as they like.</summary>
        bool IsShowing { get; }
        /// <summary>Called every frame by <see cref="ServicesRunner"/>: (re)load an ad when needed.</summary>
        void Tick();
        /// <param name="placement">Placement name for analytics / network config.</param>
        /// <param name="done">Call once: true only when the network confirmed the reward (video watched to the end).
        /// Extra calls are ignored.</param>
        void Show(string placement, Action<bool> done);
    }

    /// <summary>
    /// Optional: a network that also serves interstitials (forced ads). Implement it on the same provider object.
    /// Forced ads are only requested at natural breaks, never during the tutorial, and never after Remove Ads.
    /// </summary>
    public interface IInterstitialAdProvider
    {
        bool IsInterstitialReady { get; }
        void ShowInterstitial(string placement, Action done);
    }

    /// <summary>
    /// Entry point for every ad. The game ships rewarded ads only (no interstitials, no banners). VIP (the
    /// <c>jk_remove_ads</c> product, <see cref="GameManager.NoAds"/>) skips every video: rewards are granted at once.
    /// Rewarded flow: the game shows the reward and the player confirms (OfferPopup /
    /// PremiumPopup / a labelled button), then <see cref="ShowRewarded"/> plays the video (or spends an Ad Ticket) and
    /// grants exactly once, only after the network confirmed completion, and saves right away. A provider that reports
    /// twice, or never reports back, cannot double-grant or lock the buttons. Without a provider a test overlay is
    /// used in the Editor and development builds; release builds without a provider hide the ad buttons.
    /// </summary>
    public static class Ads
    {
        // Rewarded placements (names are sent to the network and to analytics: keep them stable).
        public const string PlacementCash2x = "boost_cash_2x";
        public const string PlacementTurbo = "boost_turbo";
        public const string PlacementFreeCash = "free_cash";
        public const string PlacementOffline = "offline_2x";
        public const string PlacementUnlock = "unlock_assist";
        public const string PlacementFoxRestore = "fox_restore";
        public const string PlacementTruckCall = "truck_call";
        // Interstitial placements (natural breaks only).
        public const string InterstitialUpgradeClose = "upgrades_closed";
        public const string InterstitialDeliveryDone = "delivery_done";

        /// <summary>Real ad network. Null = simulated (debug builds) or unavailable (release).</summary>
        public static IRewardedAdProvider Provider;

        /// <summary>Use the test overlay when no provider is set. Defaults to debug builds only, so a release build can
        /// never hand out free rewards without a real network.</summary>
        public static bool SimulateWhenNoProvider = Debug.isDebugBuild;

        /// <summary>Spend an Ad Ticket (when the player has one) instead of playing the video.</summary>
        public static bool UseTickets = true;

        // A provider that never calls back must not lock the ad buttons forever (never applied while a video shows).
        const float ShowTimeout = 120f;

        static Action<bool> _finish;
        static float _busySince;
        static float _lastRewarded = -9999f;
        static float _lastForced = -9999f;

        public static bool Busy
        {
            get
            {
                if (_finish != null && Provider != null && Provider.IsShowing) _busySince = Time.realtimeSinceStartup;
                if (_finish != null && Time.realtimeSinceStartup - _busySince > ShowTimeout)
                {
                    Debug.LogWarning("[Ads] provider did not report back: treating the ad as failed");
                    _finish(false);
                }
                return _finish != null;
            }
        }

        static bool HasTicket => UseTickets && GameManager.I != null && GameManager.I.Tickets > 0;

        static bool VideoReady => Provider != null ? Provider.IsReady : SimulateWhenNoProvider;

        /// <summary>VIP: Skip Ads owned (<see cref="GameManager.NoAds"/>): every rewarded placement is granted at once.</summary>
        public static bool Vip => GameManager.I != null && GameManager.I.NoAds;

        /// <summary>A reward can be claimed right now (VIP, an Ad Ticket, or a loaded video).</summary>
        public static bool IsReady => !Busy && (Vip || HasTicket || VideoReady);

        /// <summary>Claiming plays no video (VIP or an Ad Ticket): buttons drop the video badge and say so.</summary>
        public static bool SkipsVideo => Vip || HasTicket;

        /// <summary>
        /// Rewarded offers exist in this build at all: a network is installed (or simulated in debug builds), or the
        /// player can claim without a video. False only in a release build whose ad SDK failed to start, where the
        /// reward buttons are hidden instead of sitting there disabled.
        /// </summary>
        public static bool Enabled => Provider != null || SimulateWhenNoProvider || SkipsVideo;

        /// <summary>Button label for a rewarded offer: <paramref name="watch"/> for a video, otherwise what is spent.</summary>
        public static string ClaimLabel(string watch = "WATCH") => Vip ? "CLAIM" : HasTicket ? "USE TICKET" : watch;

        /// <summary>Why a reward cannot be claimed right now (shown as a toast when a dimmed button is tapped).</summary>
        public static string NotReadyMessage =>
            Busy ? "Please wait..."
            : Application.internetReachability == NetworkReachability.NotReachable ? "No internet connection. Videos need a connection."
            : "No video available right now. Try again in a moment.";

        /// <summary>
        /// False once Remove Ads was bought. Any forced ad (interstitial, banner) must check this; rewarded ads are
        /// optional and stay available.
        /// </summary>
        public static bool ForcedAdsAllowed => GameManager.I == null || !GameManager.I.NoAds;

        /// <summary>Raised after a rewarded placement was granted (video or ticket).</summary>
        public static event Action<string> Rewarded;

        /// <summary>
        /// Plays a rewarded video (or spends an Ad Ticket) and runs <paramref name="onReward"/> exactly once if it was
        /// earned, otherwise <paramref name="onFail"/>. Call it only after the player confirmed the offer.
        /// </summary>
        public static void ShowRewarded(string placement, Action onReward, Action onFail = null)
        {
            if (Busy)
            {
                onFail?.Invoke();
                return;
            }

            if (Vip)
            {
                Analytics.Log(Analytics.RewardedCompleted, "placement", placement, "vip", true);
                Grant(placement, onReward);
                return;
            }

            if (HasTicket && GameManager.I.TrySpendTicket())
            {
                Analytics.Log(Analytics.RewardedCompleted, "placement", placement, "ticket", true);
                Grant(placement, onReward);
                return;
            }

            if (!VideoReady)
            {
                Analytics.Log(Analytics.RewardedFailed, "placement", placement, "reason", "not_ready");
                onFail?.Invoke();
                return;
            }

            Analytics.Log(Analytics.RewardedStarted, "placement", placement);
            _busySince = Time.realtimeSinceStartup;
            AudioListener.pause = true;
            Platform.Pause("ad");
            Action<bool> finish = null;
            finish = ok =>
            {
                // Runs once: duplicate or late callbacks from the network are ignored.
                if (_finish != finish) return;
                _finish = null;
                AudioListener.pause = false;
                Platform.Resume("ad");
                if (ok)
                {
                    Analytics.Log(Analytics.RewardedCompleted, "placement", placement, "ticket", false);
                    Grant(placement, onReward);
                }
                else
                {
                    Analytics.Log(Analytics.RewardedFailed, "placement", placement, "reason", "not_completed");
                    onFail?.Invoke();
                }
            };
            _finish = finish;

            try
            {
                if (Provider != null) Provider.Show(placement, finish);
                else if (AdOverlay.I != null) AdOverlay.I.Play(placement, finish);
                else finish(false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Ads] provider threw: " + e.Message);
                finish(false);
            }
        }

        static void Grant(string placement, Action onReward)
        {
            _lastRewarded = Time.realtimeSinceStartup;
            try { onReward?.Invoke(); }
            finally
            {
                Rewarded?.Invoke(placement);
                Haptics.Play(HapticKind.Success);
                if (GameManager.I != null) GameManager.I.Save();
            }
        }

        /// <summary>
        /// Shows a forced ad at a natural break if every pacing rule allows it (no Remove Ads, tutorial done, not in the
        /// first minutes, cooldown since the last forced and rewarded ad, nothing else on screen). Safe to call often.
        /// Returns true when an ad started.
        /// </summary>
        public static bool TryShowInterstitial(string placement)
        {
            if (!ReleaseConfig.InterstitialsEnabled || !ForcedAdsAllowed || Busy) return false;
            if (!(Provider is IInterstitialAdProvider inter) || !inter.IsInterstitialReady) return false;
            var gm = GameManager.I;
            if (gm == null || gm.TutorialStep < Tutorial.FreePlayStep) return false;
            float now = Time.realtimeSinceStartup;
            if (now < ReleaseConfig.InterstitialFirstDelay) return false;
            if (now - _lastForced < ReleaseConfig.InterstitialCooldown) return false;
            if (now - _lastRewarded < ReleaseConfig.InterstitialAfterRewarded) return false;
            if (!Platform.GameplayRunning || CameraFollow.Busy) return false;

            _lastForced = now;
            _busySince = now;
            AudioListener.pause = true;
            Platform.Pause("ad");
            Action<bool> finish = null;
            finish = _ =>
            {
                if (_finish != finish) return;
                _finish = null;
                AudioListener.pause = false;
                Platform.Resume("ad");
            };
            _finish = finish;
            Analytics.Log(Analytics.InterstitialShown, "placement", placement);
            try { inter.ShowInterstitial(placement, () => finish(true)); }
            catch (Exception e)
            {
                Debug.LogWarning("[Ads] interstitial threw: " + e.Message);
                finish(false);
            }
            return true;
        }
    }
}
