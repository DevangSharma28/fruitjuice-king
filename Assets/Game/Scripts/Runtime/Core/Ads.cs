using System;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Anything that can play a rewarded video. Plug a real network (LevelPlay, Unity Ads, AdMob...) in by
    /// implementing this and assigning <see cref="Ads.Provider"/> at startup.
    /// </summary>
    public interface IRewardedAdProvider
    {
        bool IsReady { get; }
        /// <param name="placement">Placement name for analytics / network config.</param>
        /// <param name="done">true when the reward was earned.</param>
        void Show(string placement, Action<bool> done);
    }

    /// <summary>Entry point for rewarded ads. Falls back to a simulated ad so the reward flow can be tested.</summary>
    public static class Ads
    {
        public const string PlacementCash2x = "boost_cash_2x";
        public const string PlacementTurbo = "boost_turbo";
        public const string PlacementFreeCash = "free_cash";
        public const string PlacementOffline = "offline_2x";
        public const string PlacementUnlock = "unlock_assist";

        /// <summary>Real ad network. Null = simulated.</summary>
        public static IRewardedAdProvider Provider;

        /// <summary>Use the fake ad overlay when no provider is set (turn off for release builds without ads).</summary>
        public static bool SimulateWhenNoProvider = true;

        public static bool Busy { get; private set; }

        /// <summary>Spend an Ad Ticket (when the player has one) instead of playing the video.</summary>
        public static bool UseTickets = true;

        static bool HasTicket => UseTickets && GameManager.I != null && GameManager.I.Tickets > 0;

        public static bool IsReady => !Busy && (HasTicket || (Provider != null ? Provider.IsReady : SimulateWhenNoProvider));

        /// <summary>
        /// False once Remove Ads was bought. Any forced ad (interstitial, banner) must check this; rewarded ads are
        /// optional and stay available.
        /// </summary>
        public static bool ForcedAdsAllowed => GameManager.I == null || !GameManager.I.NoAds;

        public static event Action<string> Rewarded;

        public static void ShowRewarded(string placement, Action onReward, Action onFail = null)
        {
            if (!Busy && HasTicket && GameManager.I.TrySpendTicket())
            {
                Rewarded?.Invoke(placement);
                onReward?.Invoke();
                return;
            }

            if (!IsReady)
            {
                onFail?.Invoke();
                return;
            }

            Busy = true;
            AudioListener.pause = true;
            Platform.Pause("ad");
            Action<bool> finish = ok =>
            {
                Busy = false;
                AudioListener.pause = false;
                Platform.Resume("ad");
                if (ok)
                {
                    Rewarded?.Invoke(placement);
                    onReward?.Invoke();
                }
                else onFail?.Invoke();
            };

            if (Provider != null) Provider.Show(placement, finish);
            else if (AdOverlay.I != null) AdOverlay.I.Play(placement, finish);
            else finish(true);
        }
    }
}
