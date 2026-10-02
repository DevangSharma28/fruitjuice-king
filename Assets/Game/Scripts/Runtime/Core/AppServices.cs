using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Installs the platform services once per app launch, before the first scene loads:
    /// <list type="number">
    /// <item>the store (<see cref="UnityIapProvider"/> on Android / iOS devices; the Editor and dev builds without a
    /// store keep the simulated purchases);</item>
    /// <item>consent (<see cref="Privacy"/>), then the ad network, which must start only after consent;</item>
    /// <item>analytics providers.</item>
    /// </list>
    /// Plug real SDKs in at the marked places (see RELEASE_CHECKLIST.md). Nothing here holds credentials: ids come
    /// from <see cref="ReleaseConfig"/>.
    /// </summary>
    public static class AppServices
    {
        /// <summary>Editor testing only: use Unity IAP's fake store instead of the built-in simulation.</summary>
        public static bool UseUnityIapInEditor = false;

        static bool _booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (_booted) return;
            _booted = true;

            // ---- store
            bool device = !Application.isEditor && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
            if (Iap.Provider == null && (device || UseUnityIapInEditor))
            {
                var store = new UnityIapProvider();
                Iap.Provider = store;
                store.Connect();
            }

            // ---- consent first, then ads (an ad SDK must not start before the player answered).
            // Privacy.Provider = new MyConsentProvider();            // e.g. Google UMP + iOS ATT
            Privacy.RequestOnLaunch(StartAds);

            // ---- analytics
            // Analytics.AddProvider(new MyAnalyticsProvider());      // e.g. Firebase / GameAnalytics

            if (Debug.isDebugBuild && !Application.isEditor)
            {
                var missing = ReleaseConfig.MissingItems();
                if (missing.Count > 0) Debug.Log("[Release] not configured yet: " + string.Join(", ", missing));
            }
        }

        static void StartAds()
        {
            // Ads.Provider = new MyRewardedProvider(ReleaseConfig.AdAppKey, ReleaseConfig.RewardedUnitId,
            //     ReleaseConfig.InterstitialUnitId, Privacy.PersonalizedAds, Privacy.ChildDirected);
        }
    }
}
