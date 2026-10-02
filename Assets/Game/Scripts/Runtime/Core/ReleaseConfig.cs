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
        /// <summary>Store package name / bundle identifier. Applied to Android and iOS by Build Everything
        /// (<c>ConfigureProject</c>). Never change it after the first upload to Google Play.</summary>
        public const string BundleIdentifier = "com.ionixgames.juicekingtycoon";
        /// <summary>Player Settings company name. It is part of <c>Application.persistentDataPath</c> (save backup file):
        /// never change it after release.</summary>
        public const string CompanyName = "Ionix Games";

        // ------------------------------------------------------------------ ads: Google AdMob, rewarded only
        // AdMob ▸ Apps ▸ App settings ▸ App ID ("ca-app-pub-XXXX~YYYY") and Ad units ▸ Rewarded ("ca-app-pub-XXXX/ZZZZ").
        // The values below are Google's official TEST ids: they always fill with test ads and earn nothing. Replace them
        // with the real ids before the production release (MissingItems and the build check warn while they are in use).
        public const string AndroidAdAppKey = "ca-app-pub-3940256099942544~3347511713";
        public const string IosAdAppKey = "ca-app-pub-3940256099942544~1458002511";
        public const string AndroidRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
        public const string IosRewardedUnitId = "ca-app-pub-3940256099942544/1712485313";
        /// <summary>Not used: the game shows rewarded ads only (see <see cref="InterstitialsEnabled"/>).</summary>
        public const string AndroidInterstitialUnitId = "";
        public const string IosInterstitialUnitId = "";

        /// <summary>Your own phones' AdMob test-device ids (logcat prints "Use new RequestConfiguration...TestDeviceIds")
        /// so real ad units serve test ads to you. Also lets debug builds preview the EEA consent form.</summary>
        public static readonly string[] AdMobTestDeviceIds = new string[0];

        const string GoogleTestPublisher = "ca-app-pub-3940256099942544";

        public static string AdAppKey => Application.platform == RuntimePlatform.IPhonePlayer ? IosAdAppKey : AndroidAdAppKey;
        public static string RewardedUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? IosRewardedUnitId : AndroidRewardedUnitId;
        public static string InterstitialUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? IosInterstitialUnitId : AndroidInterstitialUnitId;
        public static bool AdsConfigured => !string.IsNullOrEmpty(AdAppKey) && !string.IsNullOrEmpty(RewardedUnitId);
        /// <summary>Google's test ids are still in place (fine for internal testing, never for production).</summary>
        public static bool UsingTestAdIds(bool android) =>
            (android ? AndroidAdAppKey : IosAdAppKey).StartsWith(GoogleTestPublisher)
            || (android ? AndroidRewardedUnitId : IosRewardedUnitId).StartsWith(GoogleTestPublisher);

        // ------------------------------------------------------------------ interstitial pacing (forced ads)
        /// <summary>Master switch: off. The game monetises with optional rewarded ads only; no ad is ever forced.</summary>
        public const bool InterstitialsEnabled = false;
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
        /// <summary>Public privacy policy page (Google Play requires it in the store listing <b>and</b> inside the app:
        /// Settings ▸ Privacy). Draft text: PRIVACY_POLICY.md in the project root.</summary>
        public const string PrivacyPolicyUrl = "";
        public const string TermsOfServiceUrl = "";
        /// <summary>Set when the game is directed at children (COPPA / Families policy): ads must be non-personalised.</summary>
        public const bool ChildDirected = false;

        /// <summary>Human-readable list of what is still missing for a store build.</summary>
        public static List<string> MissingItems()
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(AndroidAdAppKey) || string.IsNullOrEmpty(AndroidRewardedUnitId)) list.Add("Android AdMob app id / rewarded unit id");
            else if (UsingTestAdIds(true)) list.Add("real Android AdMob ids (Google test ids in use)");
            if (Ads.Provider == null) list.Add("rewarded ad provider (Ads.Provider)");
            if (Iap.Provider == null) list.Add("store provider (Iap.Provider)");
            if (string.IsNullOrEmpty(PrivacyPolicyUrl)) list.Add("privacy policy URL");
            if (string.IsNullOrEmpty(Application.identifier) || Application.identifier.StartsWith("com.DefaultCompany") || Application.identifier.StartsWith("com.Company"))
                list.Add("bundle identifier");
            return list;
        }
    }
}
