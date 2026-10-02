using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Everything a store build needs from outside the project: ad network keys, ad unit ids, store-specific product ids,
    /// legal URLs. Values left empty here are reported by <see cref="MissingItems"/> (and logged once at boot in
    /// development builds) so a release never goes out half configured. See RELEASE_CHECKLIST.md. Never commit secrets
    /// that are not meant to ship inside the app (server keys, signing passwords...): these are all public client ids.
    /// </summary>
    public static class ReleaseConfig
    {
        // ------------------------------------------------------------------ identity
        /// <summary>Store bundle identifier (e.g. "com.yourstudio.juicekingtycoon"). Empty = keep Player Settings as is.
        /// Applied to Android and iOS by Build Everything. Never change it after the first public release.</summary>
        public const string BundleIdentifier = "";

        // ------------------------------------------------------------------ ads (rewarded + interstitial)
        // App keys / unit ids from your mediation dashboard (LevelPlay, AdMob, AppLovin MAX...). Empty = not configured.
        public const string AndroidAdAppKey = "";
        public const string IosAdAppKey = "";
        public const string AndroidRewardedUnitId = "";
        public const string IosRewardedUnitId = "";
        public const string AndroidInterstitialUnitId = "";
        public const string IosInterstitialUnitId = "";

        public static string AdAppKey => Application.platform == RuntimePlatform.IPhonePlayer ? IosAdAppKey : AndroidAdAppKey;
        public static string RewardedUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? IosRewardedUnitId : AndroidRewardedUnitId;
        public static string InterstitialUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? IosInterstitialUnitId : AndroidInterstitialUnitId;
        public static bool AdsConfigured => !string.IsNullOrEmpty(AdAppKey) && !string.IsNullOrEmpty(RewardedUnitId);

        // ------------------------------------------------------------------ interstitial pacing (forced ads)
        /// <summary>Master switch. Forced ads never show without a real provider, after Remove Ads, or during the tutorial.</summary>
        public const bool InterstitialsEnabled = true;
        /// <summary>No forced ad in the first minutes of a session.</summary>
        public const float InterstitialFirstDelay = 300f;
        /// <summary>Minimum gap between two forced ads.</summary>
        public const float InterstitialCooldown = 240f;
        /// <summary>A rewarded ad the player chose to watch also resets the forced-ad clock by this much.</summary>
        public const float InterstitialAfterRewarded = 120f;

        // ------------------------------------------------------------------ in-app purchases
        /// <summary>
        /// Store product ids that differ from the catalog id (<see cref="IapCatalog"/>). By default the catalog id is
        /// used on both stores, which is the simplest setup: create the products in Play Console and App Store Connect
        /// with exactly these ids. Fill these maps only if a store forces a different id (e.g. a reverse-DNS prefix).
        /// </summary>
        public static readonly Dictionary<string, string> GooglePlayProductIds = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> AppleProductIds = new Dictionary<string, string>();

        // ------------------------------------------------------------------ legal / privacy
        public const string PrivacyPolicyUrl = "";
        public const string TermsOfServiceUrl = "";
        /// <summary>Set when the game is directed at children (COPPA / Families policy): ads must be non-personalised.</summary>
        public const bool ChildDirected = false;

        /// <summary>Human-readable list of what is still missing for a store build.</summary>
        public static List<string> MissingItems()
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(AndroidAdAppKey) || string.IsNullOrEmpty(AndroidRewardedUnitId)) list.Add("Android ad app key / rewarded unit id");
            if (string.IsNullOrEmpty(IosAdAppKey) || string.IsNullOrEmpty(IosRewardedUnitId)) list.Add("iOS ad app key / rewarded unit id");
            if (string.IsNullOrEmpty(AndroidInterstitialUnitId) || string.IsNullOrEmpty(IosInterstitialUnitId)) list.Add("interstitial unit ids");
            if (Ads.Provider == null) list.Add("rewarded ad provider (Ads.Provider)");
            if (Iap.Provider == null) list.Add("store provider (Iap.Provider)");
            if (string.IsNullOrEmpty(PrivacyPolicyUrl)) list.Add("privacy policy URL");
            if (string.IsNullOrEmpty(Application.identifier) || Application.identifier.StartsWith("com.DefaultCompany") || Application.identifier.StartsWith("com.Company"))
                list.Add("bundle identifier");
            return list;
        }
    }
}
