using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Anything that records analytics events (Firebase, GameAnalytics, Unity Analytics, a portal SDK...). Implement it
    /// and add it with <see cref="Analytics.AddProvider"/> at startup. Game code never talks to an SDK directly.
    /// </summary>
    public interface IAnalyticsProvider
    {
        /// <param name="name">Event name (<see cref="Analytics"/> constants, snake_case).</param>
        /// <param name="parameters">Event parameters; may be empty. Do not keep a reference: the dictionary is reused.</param>
        void LogEvent(string name, IReadOnlyDictionary<string, object> parameters);
    }

    /// <summary>
    /// Provider-agnostic analytics hooks. Events are rare (unlocks, purchases, ads), so the cost is negligible; with no
    /// provider registered every call is a no-op apart from an optional Editor log.
    /// </summary>
    public static class Analytics
    {
        // ---- event names (keep stable once shipped: dashboards and funnels refer to them)
        public const string FirstLaunch = "first_launch";
        public const string TutorialComplete = "tutorial_complete";
        public const string Unlock = "unlock";
        public const string Upgrade = "upgrade";
        public const string WorldComplete = "world_complete";
        public const string WorldEnter = "world_enter";
        public const string DeliveryComplete = "delivery_complete";
        public const string DeliveryExpired = "delivery_expired";
        public const string FoxRaid = "fox_raid";
        public const string FoxRecovery = "fox_recovery";
        public const string RewardedStarted = "rewarded_ad_started";
        public const string RewardedCompleted = "rewarded_ad_completed";
        public const string RewardedFailed = "rewarded_ad_failed";
        public const string InterstitialShown = "interstitial_shown";
        public const string IapStarted = "iap_started";
        public const string IapCompleted = "iap_completed";
        public const string IapFailed = "iap_failed";
        public const string IapRestored = "iap_restored";
        public const string OfflineClaimed = "offline_earnings_claimed";
        public const string ApplesSpent = "golden_apples_spent";

        static readonly List<IAnalyticsProvider> Providers = new List<IAnalyticsProvider>();
        static readonly Dictionary<string, object> Params = new Dictionary<string, object>();

        /// <summary>Print every event to the console in the Editor (handy while wiring dashboards).</summary>
        public static bool LogInEditor = false;

        public static void AddProvider(IAnalyticsProvider p)
        {
            if (p != null && !Providers.Contains(p)) Providers.Add(p);
        }

        public static void RemoveProvider(IAnalyticsProvider p) => Providers.Remove(p);

        public static void Log(string name) => Send(name);

        public static void Log(string name, string k1, object v1)
        {
            Params[k1] = v1;
            Send(name);
        }

        public static void Log(string name, string k1, object v1, string k2, object v2)
        {
            Params[k1] = v1;
            Params[k2] = v2;
            Send(name);
        }

        public static void Log(string name, string k1, object v1, string k2, object v2, string k3, object v3)
        {
            Params[k1] = v1;
            Params[k2] = v2;
            Params[k3] = v3;
            Send(name);
        }

        static void Send(string name)
        {
            // Every event carries the world so funnels can be split per expansion.
            if (GameManager.I != null) Params["world"] = GameManager.I.data.expansion;
#if UNITY_EDITOR
            if (LogInEditor)
            {
                var sb = new System.Text.StringBuilder("[Analytics] ").Append(name);
                foreach (var kv in Params) sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
                Debug.Log(sb.ToString());
            }
#endif
            for (int i = 0; i < Providers.Count; i++)
            {
                try { Providers[i].LogEvent(name, Params); }
                catch (Exception e) { Debug.LogWarning("[Analytics] provider failed on " + name + ": " + e.Message); }
            }
            Params.Clear();
        }
    }
}
