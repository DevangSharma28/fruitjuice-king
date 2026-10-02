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

        /// <summary>
        /// Runs the consent flow once per launch (boot screen). Without a provider consent is "not required" and
        /// <paramref name="then"/> runs at once. A provider that never answers is given up on after a timeout so boot
        /// can never hang.
        /// </summary>
        public static void RequestOnLaunch(Action then)
        {
            if (Resolved || Provider == null)
            {
                if (Provider == null && Status == ConsentStatus.Unknown) Status = ConsentStatus.NotRequired;
                Resolved = true;
                then?.Invoke();
                return;
            }
            bool done = false;
            float t0 = Time.realtimeSinceStartup;
            Action<ConsentStatus> finish = s =>
            {
                if (done) return;
                done = true;
                Status = s;
                Resolved = true;
                then?.Invoke();
            };
            try { Provider.RequestConsent(finish); }
            catch (Exception e)
            {
                Debug.LogWarning("[Privacy] consent request failed: " + e.Message);
                finish(Status);
            }
            if (!done) PendingTimeout = () =>
            {
                if (!done && Time.realtimeSinceStartup - t0 > RequestTimeout) finish(Status);
                return done;
            };
        }

        /// <summary>Polled by the boot screen while a consent form is up (returns true once resolved).</summary>
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
