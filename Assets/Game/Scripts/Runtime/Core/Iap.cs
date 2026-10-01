using System;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// A real store (Unity IAP, a portal SDK...). Implement this and assign <see cref="Iap.Provider"/> at startup.
    /// Product ids come from <see cref="IapCatalog"/>.
    /// </summary>
    public interface IIapProvider
    {
        bool IsReady { get; }
        /// <summary>The store's localized price string ("1,09 €"), or null while it is unknown.</summary>
        string LocalizedPrice(string productId);
        /// <summary>Start a purchase; call <paramref name="done"/> once with true when it succeeded. Iap grants the goods.</summary>
        void Purchase(string productId, Action<bool> done);
        /// <summary>
        /// Restore non-consumables (iOS needs a button for it). Call <see cref="Iap.Deliver"/> for every restored product
        /// that is not owned yet, then <paramref name="done"/>.
        /// </summary>
        void Restore(Action<bool> done);
    }

    /// <summary>
    /// Entry point for in-app purchases: prices, purchase flow and granting the goods. Without a provider, purchases are
    /// simulated in the Editor and development builds only, so a release build never hands out free premium currency.
    /// </summary>
    public static class Iap
    {
        /// <summary>Real store. Null = simulated (debug builds) or unavailable (release).</summary>
        public static IIapProvider Provider;

        /// <summary>Forces simulation on or off; null = simulate in the Editor and development builds.</summary>
        public static bool? SimulateOverride;

        const float SimulatedDelay = 0.6f;
        // A purchase that never reports back (scene change, store bug) stops blocking the shop after this long.
        const float PendingTimeout = 60f;

        static string _pending;
        static float _pendingSince;

        static bool Simulate => SimulateOverride ?? Debug.isDebugBuild;

        public static bool Busy => _pending != null && Time.realtimeSinceStartup - _pendingSince < PendingTimeout;

        public static bool IsReady => Provider != null ? Provider.IsReady : Simulate;

        /// <summary>Raised after the goods of a purchase (or restore) were granted.</summary>
        public static event Action<IapProduct> Purchased;

        public static string PriceText(IapProduct p)
        {
            if (p == null) return "";
            string s = Provider != null ? Provider.LocalizedPrice(p.id) : null;
            return string.IsNullOrEmpty(s) ? p.fallbackPrice : s;
        }

        /// <summary>True for a non-consumable the player already has.</summary>
        public static bool Owns(IapProduct p) =>
            p != null && p.kind == IapKind.RemoveAds && GameManager.I != null && GameManager.I.NoAds;

        public static void Purchase(string productId, Action<bool> done)
        {
            var p = IapCatalog.Get(productId);
            if (p == null || Busy || !IsReady || Owns(p))
            {
                done?.Invoke(false);
                return;
            }

            _pending = productId;
            _pendingSince = Time.realtimeSinceStartup;
            Action<bool> finish = ok =>
            {
                // Runs once: a provider that reports twice must not grant twice.
                if (_pending != productId) return;
                _pending = null;
                if (ok) ok = Deliver(productId);
                done?.Invoke(ok);
            };

            if (Provider != null) Provider.Purchase(productId, finish);
            else Tweener.Delay(SimulatedDelay, () => finish(true));
        }

        public static void Restore(Action<bool> done)
        {
            if (Provider != null) Provider.Restore(done);
            else done?.Invoke(Simulate);
        }

        /// <summary>
        /// Grants a purchased product and saves. Providers call this for purchases that complete outside
        /// <see cref="Purchase"/> (pending at launch, restored). Returns false when no game is loaded yet (the loading
        /// screen): leave the transaction pending and deliver it again once a world is running.
        /// </summary>
        public static bool Deliver(string productId)
        {
            var p = IapCatalog.Get(productId);
            var gm = GameManager.I;
            if (p == null || gm == null) return false;
            switch (p.kind)
            {
                case IapKind.Apples: gm.AddApples(p.amount); break;
                case IapKind.Tickets: gm.AddTickets(p.amount); break;
                case IapKind.RemoveAds: gm.SetNoAds(true); break;
            }
            Purchased?.Invoke(p);
            return true;
        }
    }
}
