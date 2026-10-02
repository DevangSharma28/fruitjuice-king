using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Installs the platform services once per app launch, before the first scene loads:
    /// <list type="number">
    /// <item>the store (<see cref="UnityIapProvider"/> on Android / iOS devices; the Editor and dev builds without a
    /// store keep the simulated purchases);</item>
    /// <item>consent (<see cref="UmpConsentProvider"/>, Google UMP), then AdMob rewarded ads
    /// (<see cref="AdMobProvider"/>), which never request an ad before consent allows it;</item>
    /// <item>analytics providers.</item>
    /// </list>
    /// The Editor keeps the simulated store and the test ad overlay (<see cref="AdOverlay"/>) unless
    /// <see cref="UseUnityIapInEditor"/> / <see cref="UseAdMobInEditor"/> are set. Ids come from <see cref="ReleaseConfig"/>.
    /// </summary>
    public static class AppServices
    {
        /// <summary>Editor testing only: use Unity IAP's fake store instead of the built-in simulation.</summary>
        public static bool UseUnityIapInEditor = false;

        /// <summary>Editor testing only: use the AdMob plugin's placeholder ads instead of the test overlay.</summary>
        public static bool UseAdMobInEditor = false;

        // A first launch without internet cannot get consent: try again this often until ads may be requested.
        const float ConsentRetryInterval = 60f;

        static bool _booted, _adsStarting;
        static float _consentRetryAt = -1f;

        static bool Device => !Application.isEditor &&
            (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (_booted) return;
            _booted = true;
            ServicesRunner.Install();

            // ---- store
            if (Iap.Provider == null && (Device || UseUnityIapInEditor))
            {
                var store = new UnityIapProvider();
                Iap.Provider = store;
                store.Connect();
            }

            // ---- consent first, then ads (no ad request before the player answered where consent is required).
            bool admob = (Device || UseAdMobInEditor) && ReleaseConfig.AdsConfigured;
            if (admob && Privacy.Provider == null) Privacy.Provider = new UmpConsentProvider();
            Privacy.RequestOnLaunch(admob ? StartAds : (System.Action)null);

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
            if (Ads.Provider != null || _adsStarting) return;
            if (!Privacy.CanRequestAds)
            {
                // Consent could not be obtained yet (first launch offline, form failed): ask again later.
                _consentRetryAt = Time.realtimeSinceStartup + ConsentRetryInterval;
                return;
            }
            _consentRetryAt = -1f;
            _adsStarting = true;
            AdMobProvider.Start(ReleaseConfig.RewardedUnitId, p =>
            {
                _adsStarting = false;
                Ads.Provider = p;
            });
        }

        /// <summary>Every frame (<see cref="ServicesRunner"/>): consent timeout, consent retry, ad (re)loading.</summary>
        internal static void Tick()
        {
            var pending = Privacy.PendingTimeout;
            if (pending != null) pending();
            if (_consentRetryAt > 0f && Time.realtimeSinceStartup >= _consentRetryAt
                && Application.internetReachability != NetworkReachability.NotReachable)
            {
                _consentRetryAt = -1f;
                Privacy.RequestAgain(StartAds);
            }
            Ads.Provider?.Tick();
        }
    }
}
