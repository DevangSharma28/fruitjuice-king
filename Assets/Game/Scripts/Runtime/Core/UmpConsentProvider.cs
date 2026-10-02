using System;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Consent through Google's User Messaging Platform (bundled with the Google Mobile Ads plugin). Shows the GDPR
    /// form to EEA / UK / Swiss players (and US state notices when configured in AdMob ▸ Privacy &amp; messaging) before
    /// any ad is requested, and offers "Privacy options" in Settings when the region requires it. The Mobile Ads SDK
    /// reads the answer itself (TCF / GPP strings), so ad requests need no extra flags.
    /// <para>Debug builds can force the EEA form with <see cref="DebugForceEea"/> and test device ids
    /// (<see cref="ReleaseConfig.AdMobTestDeviceIds"/>).</para>
    /// </summary>
    public class UmpConsentProvider : IConsentProvider
    {
        /// <summary>Debug builds only: behave as if the device were in the EEA (to test the form).</summary>
        public static bool DebugForceEea;

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        public void RequestConsent(Action<ConsentStatus> done)
        {
            var request = new ConsentRequestParameters
            {
                // The game is not directed at children (13+). See ReleaseConfig.ChildDirected.
                TagForUnderAgeOfConsent = ReleaseConfig.ChildDirected,
            };
            if (Debug.isDebugBuild && (DebugForceEea || ReleaseConfig.AdMobTestDeviceIds.Length > 0))
            {
                request.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugForceEea ? DebugGeography.EEA : DebugGeography.Disabled,
                    TestDeviceHashedIds = new System.Collections.Generic.List<string>(ReleaseConfig.AdMobTestDeviceIds),
                };
            }
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    // Offline or misconfigured: a consent answered in an earlier session still allows ads.
                    Debug.LogWarning("[UMP] consent info update failed: " + updateError.Message);
                    done(Current());
                    return;
                }
                Privacy.FormShowing = ConsentInformation.ConsentStatus == GoogleMobileAds.Ump.Api.ConsentStatus.Required;
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    Privacy.FormShowing = false;
                    if (formError != null) Debug.LogWarning("[UMP] consent form failed: " + formError.Message);
                    done(Current());
                });
            });
        }

        public void ShowPrivacyOptions(Action<ConsentStatus> done)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null) Debug.LogWarning("[UMP] privacy options failed: " + error.Message);
                done(Current());
            });
        }

        static ConsentStatus Current()
        {
            switch (ConsentInformation.ConsentStatus)
            {
                case GoogleMobileAds.Ump.Api.ConsentStatus.NotRequired: return ConsentStatus.NotRequired;
                // "Obtained" means the player answered; whether ads are personalised is in the TCF string the SDK reads.
                case GoogleMobileAds.Ump.Api.ConsentStatus.Obtained: return ConsentStatus.Granted;
                default: return ConsentStatus.Unknown;
            }
        }
    }
}
