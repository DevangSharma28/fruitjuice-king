using System;
using UnityEngine;

namespace JuiceKing
{
    public enum ConsentStatus { Unknown = 0, Granted = 1, Denied = 2, NotRequired = 3 }

    /// <summary>
    /// A consent / tracking-permission flow (Google UMP, the mediation network's CMP, iOS App Tracking Transparency...).
    /// Implement it and assign <see cref="Privacy.Provider"/> before the boot screen runs (see <see cref="AppServices"/>).
    /// </summary>
    public interface IConsentProvider
    {
        /// <summary>Show the consent form if the user's region needs one; call <paramref name="done"/> exactly once.</summary>
        void RequestConsent(Action<ConsentStatus> done);
        /// <summary>True when a "Privacy options" entry must be offered in Settings (GDPR re-consent).</summary>
        bool PrivacyOptionsRequired { get; }
        /// <summary>Ads may be requested (consent obtained or not required). False until the player answered.</summary>
        bool CanRequestAds { get; }
        void ShowPrivacyOptions(Action<ConsentStatus> done);
    }

    /// <summary>
    /// Consent state shared by every SDK. Ad / analytics providers read <see cref="PersonalizedAds"/> and
    /// <see cref="ChildDirected"/> when they initialise. The answer is remembered in its own PlayerPrefs key (never part
    /// of the game save, so a progress reset does not ask again).
    /// </summary>
    public static class Privacy
    {
        const string Key = "juiceking_consent";
        const float RequestTimeout = 20f;

        public static IConsentProvider Provider;

        public static event Action Changed;

        public static ConsentStatus Status
        {
            get => (ConsentStatus)PlayerPrefs.GetInt(Key, 0);
            private set
            {
                if (Status == value) return;
                PlayerPrefs.SetInt(Key, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>True once the consent step has run this session (SDKs may initialise).</summary>
        public static bool Resolved { get; private set; }

        public static bool PersonalizedAds => !ChildDirected && (Status == ConsentStatus.Granted || Status == ConsentStatus.NotRequired);
        public static bool ChildDirected => ReleaseConfig.ChildDirected;
        public static bool PrivacyOptionsAvailable => Provider != null && Provider.PrivacyOptionsRequired;
        public static bool CanRequestAds => Provider == null || Provider.CanRequestAds;

        /// <summary>
        /// Runs the consent flow once per launch (boot screen). Without a provider consent is "not required" and
        /// <paramref name="then"/> runs at once. A provider that never answers is given up on after a timeout so boot
        /// can never hang.
        /// </summary>
        public static void RequestOnLaunch(Action then)
        {
            if (_running) return;
            if (Resolved || Provider == null)
            {
                if (Provider == null && Status == ConsentStatus.Unknown) Status = ConsentStatus.NotRequired;
                Resolved = true;
                then?.Invoke();
                return;
            }
            bool done = false;
            _running = true;
            float t0 = Time.realtimeSinceStartup;
            Action<ConsentStatus> finish = s =>
            {
                if (done) return;
                done = true;
                _running = false;
                PendingTimeout = null;
                Status = s;
                Resolved = true;
                then?.Invoke();
            };
            try { Provider.RequestConsent(finish); }
            catch (Exception e)
            {
                // Never fall back to an old "not required": an unknown answer means no personalised ads.
                Debug.LogWarning("[Privacy] consent request failed: " + e.Message);
                finish(ConsentStatus.Unknown);
            }
            // The timeout only covers the consent-info request: once the form is on screen the player takes their time.
            if (!done) PendingTimeout = () =>
            {
                if (!done && Time.realtimeSinceStartup - t0 > RequestTimeout && !FormShowing) finish(ConsentStatus.Unknown);
                return done;
            };
        }

        /// <summary>Runs the consent flow again (first launch was offline, so ads could not be requested yet).</summary>
        public static void RequestAgain(Action then)
        {
            if (_running || Provider == null) return;
            Resolved = false;
            RequestOnLaunch(then);
        }

        static bool _running;

        /// <summary>Set by a provider while its form is on screen (the request timeout does not apply then).</summary>
        public static bool FormShowing;

        /// <summary>Polled every frame by <see cref="ServicesRunner"/> while a consent request runs (true once resolved).</summary>
        public static Func<bool> PendingTimeout;

        public static void ShowPrivacyOptions()
        {
            if (Provider == null) return;
            Provider.ShowPrivacyOptions(s => Status = s);
        }

        public static void OpenPrivacyPolicy()
        {
            if (!string.IsNullOrEmpty(ReleaseConfig.PrivacyPolicyUrl)) Application.OpenURL(ReleaseConfig.PrivacyPolicyUrl);
        }
    }
}
