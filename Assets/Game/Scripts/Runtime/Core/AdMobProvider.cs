using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Rewarded videos through Google AdMob (Google Mobile Ads Unity plugin). Installed by <see cref="AppServices"/> on
    /// Android / iOS devices once consent allows ad requests (<see cref="UmpConsentProvider"/>). Rewarded only: the
    /// game shows no interstitials and no banners.
    /// <list type="bullet">
    /// <item>One ad is kept loaded. A failed load retries with a growing delay (5 s ... 2 min), and only while online.</item>
    /// <item>A loaded ad is thrown away after 55 min (AdMob ads expire after an hour).</item>
    /// <item>The reward is reported when the video is <b>closed</b>, so the game resumes behind a closed ad and the
    /// player sees the reward land; closing early reports false.</item>
    /// <item>Requests are tagged for a 13+ audience (max content rating T, not child-directed) unless
    /// <see cref="ReleaseConfig.ChildDirected"/> says otherwise.</item>
    /// </list>
    /// </summary>
    public class AdMobProvider : IRewardedAdProvider
    {
        const float AdLifetime = 55f * 60f;
        static readonly float[] RetryDelays = { 5f, 10f, 20f, 40f, 80f, 120f };

        readonly string _unitId;
        RewardedAd _ad;
        float _loadedAt;
        bool _loading;
        int _failures;
        float _retryAt;

        RewardedAd _showingAd;
        Action<bool> _done;
        bool _earned;

        public bool IsShowing => _showingAd != null;

        public bool IsReady => _ad != null && !IsShowing && _ad.CanShowAd();

        AdMobProvider(string unitId) => _unitId = unitId;

        /// <summary>Initialises the Mobile Ads SDK and calls <paramref name="ready"/> with the provider (on the main thread).</summary>
        public static void Start(string unitId, Action<AdMobProvider> ready)
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            var config = new RequestConfiguration
            {
                MaxAdContentRating = ReleaseConfig.ChildDirected ? MaxAdContentRating.G : MaxAdContentRating.T,
                TagForChildDirectedTreatment = ReleaseConfig.ChildDirected ? TagForChildDirectedTreatment.True : TagForChildDirectedTreatment.False,
                TagForUnderAgeOfConsent = ReleaseConfig.ChildDirected ? TagForUnderAgeOfConsent.True : TagForUnderAgeOfConsent.False,
            };
            if (ReleaseConfig.AdMobTestDeviceIds.Length > 0) config.TestDeviceIds.AddRange(ReleaseConfig.AdMobTestDeviceIds);
            MobileAds.SetRequestConfiguration(config);
            MobileAds.Initialize(_ =>
            {
                var p = new AdMobProvider(unitId);
                p.Load();
                ready?.Invoke(p);
            });
        }

        public void Tick()
        {
            if (_loading || IsShowing) return;
            if (_ad != null && Time.realtimeSinceStartup - _loadedAt > AdLifetime)
            {
                _ad.Destroy();
                _ad = null;
            }
            if (_ad == null && Time.realtimeSinceStartup >= _retryAt) Load();
        }

        void Load()
        {
            if (_loading || _ad != null) return;
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                _retryAt = Time.realtimeSinceStartup + 5f;
                return;
            }
            _loading = true;
            RewardedAd.Load(_unitId, new AdRequest(), (ad, error) =>
            {
                _loading = false;
                if (error != null || ad == null)
                {
                    float wait = RetryDelays[Mathf.Min(_failures, RetryDelays.Length - 1)];
                    _failures++;
                    _retryAt = Time.realtimeSinceStartup + wait;
                    if (Debug.isDebugBuild) Debug.Log("[AdMob] rewarded load failed (" + (error != null ? error.GetMessage() : "no ad") + "), retry in " + wait + " s");
                    return;
                }
                _failures = 0;
                _ad = ad;
                _loadedAt = Time.realtimeSinceStartup;
            });
        }

        public void Show(string placement, Action<bool> done)
        {
            if (!IsReady)
            {
                done?.Invoke(false);
                return;
            }
            var ad = _ad;
            _ad = null;
            _showingAd = ad;
            _done = done;
            _earned = false;
            ad.OnAdFullScreenContentClosed += () => Finish(ad);
            ad.OnAdFullScreenContentFailed += e =>
            {
                if (Debug.isDebugBuild) Debug.Log("[AdMob] rewarded show failed: " + (e != null ? e.GetMessage() : "?"));
                Finish(ad);
            };
            try
            {
                ad.Show(_ => _earned = true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdMob] show threw: " + e.Message);
                Finish(ad);
            }
        }

        void Finish(RewardedAd ad)
        {
            if (_showingAd != ad) return;
            _showingAd = null;
            bool ok = _earned;
            _earned = false;
            var done = _done;
            _done = null;
            ad.Destroy();
            // Preload the next one straight away.
            _retryAt = 0f;
            Load();
            done?.Invoke(ok);
        }
    }
}
