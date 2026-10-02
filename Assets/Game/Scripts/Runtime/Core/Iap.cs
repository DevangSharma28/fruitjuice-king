using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Outcome of a purchase as the shop shows it.</summary>
    public enum IapResult
    {
        Success,
        /// <summary>The player closed the store sheet.</summary>
        Cancelled,
        Failed,
        /// <summary>Waiting for approval (Ask to Buy, slow payment method): goods arrive later through <see cref="Iap.HandlePending"/>.</summary>
        Deferred,
        /// <summary>Non-consumable already owned (it was re-applied).</summary>
        AlreadyOwned,
        /// <summary>No store connection or the product is not sold in this store.</summary>
        Unavailable,
        /// <summary>Another purchase is still running.</summary>
        Busy,
    }

    /// <summary>
    /// A real store (Unity IAP, a portal SDK...). Implement this and assign <see cref="Iap.Provider"/> at startup.
    /// The provider never grants goods itself; it reports store events to <see cref="Iap"/>:
    /// <list type="bullet">
    /// <item>a paid (or restored, or re-delivered) transaction: <see cref="Iap.HandlePending"/>, and it confirms the
    /// transaction with the store only once that returns true (the grant is saved by then);</item>
    /// <item>a failed or cancelled purchase: <see cref="Iap.HandleFailed"/>;</item>
    /// <item>a purchase waiting for approval: <see cref="Iap.HandleDeferred"/>.</item>
    /// </list>
    /// Product ids are <see cref="IapCatalog"/> ids (map store-specific ids in the provider, see <see cref="ReleaseConfig"/>).
    /// </summary>
    public interface IIapProvider
    {
        /// <summary>Connected and the product list has been fetched.</summary>
        bool IsReady { get; }
        /// <summary>Still connecting / fetching products (the shop shows a loading state).</summary>
        bool IsInitializing { get; }
        /// <summary>The store's localized price string ("1,09 €"), or null while it is unknown.</summary>
        string LocalizedPrice(string productId);
        /// <summary>False when the store did not return the product or it cannot be bought.</summary>
        bool IsAvailable(string productId);
        /// <summary>Open the store's purchase sheet. The outcome arrives through the Iap.Handle* calls.</summary>
        void Purchase(string productId);
        /// <summary>Restore non-consumables (iOS needs a button for it). Restored items arrive through <see cref="Iap.HandlePending"/>.</summary>
        void Restore(Action<bool> done);
        /// <summary>Retry connecting after a failure (called when the shop opens).</summary>
        void Reconnect();
    }

    /// <summary>
    /// Entry point for in-app purchases: prices, purchase flow and granting the goods exactly once. Every transaction id
    /// that was granted is kept in the save (<see cref="SaveData.iapTransactions"/>), so a store that reports the same
    /// purchase twice (restart before confirmation, duplicate callback) never grants twice. Without a provider, purchases
    /// are simulated in the Editor and development builds only, so a release build never hands out free premium currency.
    /// </summary>
    public static class Iap
    {
        /// <summary>Real store. Null = simulated (debug builds) or unavailable (release).</summary>
        public static IIapProvider Provider;

        /// <summary>Forces simulation on or off; null = simulate in the Editor and development builds.</summary>
        public static bool? SimulateOverride;

        /// <summary>Editor / QA: the next simulated purchase ends with this result instead of succeeding.</summary>
        public static IapResult? SimulateNextResult;

        const float SimulatedDelay = 0.6f;
        // A purchase that never reports back (scene change, store bug) stops blocking the shop after this long.
        const float PendingTimeout = 90f;
        const int LedgerSize = 200;

        static string _pending;
        static float _pendingSince;
        static Action<IapResult> _pendingDone;

        struct Queued
        {
            public string productId, transactionId;
            public Action confirm;
        }

        // Transactions reported while no world is loaded (boot screen): granted as soon as a GameManager exists.
        static readonly List<Queued> Queue = new List<Queued>();

        static bool Simulate => SimulateOverride ?? Debug.isDebugBuild;

        public static bool Busy => _pending != null && Time.realtimeSinceStartup - _pendingSince < PendingTimeout;

        public static bool IsReady => Provider != null ? Provider.IsReady : Simulate;

        public static bool IsInitializing => Provider != null && Provider.IsInitializing;

        /// <summary>Raised after the goods of a purchase (or restore) were granted and saved.</summary>
        public static event Action<IapProduct> Purchased;

        /// <summary>Raised when a purchase is waiting for approval (shop status line).</summary>
        public static event Action<IapProduct> Deferred;

        public static string PriceText(IapProduct p)
        {
            if (p == null) return "";
            string s = Provider != null ? Provider.LocalizedPrice(p.id) : null;
            // The catalog price is only a stand-in for the simulated store (Editor / dev builds); a real store that has
            // not answered yet shows "..." rather than a price that may be wrong for the player's country.
            if (string.IsNullOrEmpty(s)) return Provider != null ? "..." : p.fallbackPrice;
            return s;
        }

        public static bool IsAvailable(IapProduct p) =>
            p != null && (Provider != null ? Provider.IsReady && Provider.IsAvailable(p.id) : Simulate);

        /// <summary>True for a non-consumable the player already has.</summary>
        public static bool Owns(IapProduct p) =>
            p != null && p.kind == IapKind.RemoveAds && GameManager.I != null && GameManager.I.NoAds;

        public static void Purchase(string productId, Action<IapResult> done)
        {
            var p = IapCatalog.Get(productId);
            if (p == null)
            {
                done?.Invoke(IapResult.Unavailable);
                return;
            }
            if (Owns(p))
            {
                done?.Invoke(IapResult.AlreadyOwned);
                return;
            }
            if (Busy)
            {
                done?.Invoke(IapResult.Busy);
                return;
            }
            if (!IsReady || !IsAvailable(p))
            {
                done?.Invoke(IapResult.Unavailable);
                return;
            }

            _pending = productId;
            _pendingSince = Time.realtimeSinceStartup;
            _pendingDone = done;
            Analytics.Log(Analytics.IapStarted, "product", productId);

            if (Provider != null)
            {
                try { Provider.Purchase(productId); }
                catch (Exception e)
                {
                    Debug.LogWarning("[Iap] purchase failed to start: " + e.Message);
                    HandleFailed(productId, IapResult.Failed);
                }
                return;
            }

            // Simulated store (Editor / development builds).
            var forced = SimulateNextResult;
            SimulateNextResult = null;
            Tweener.Delay(SimulatedDelay, () =>
            {
                if (forced.HasValue && forced.Value != IapResult.Success)
                {
                    if (forced.Value == IapResult.Deferred) HandleDeferred(productId);
                    else HandleFailed(productId, forced.Value);
                    return;
                }
                HandlePending(productId, "sim-" + Guid.NewGuid().ToString("N"));
            });
        }

        public static void Restore(Action<bool> done)
        {
            Analytics.Log(Analytics.IapRestored, "provider", Provider != null);
            if (Provider != null)
            {
                Provider.Restore(done);
                return;
            }
            done?.Invoke(Simulate);
        }

        /// <summary>
        /// A paid / restored transaction from the store. Grants the goods once per transaction id and saves. Returns true
        /// when the provider may confirm (finish) the transaction with the store right away: the grant is persisted, or
        /// it had already been granted. Returns false while no world is loaded: the grant is queued and
        /// <paramref name="confirm"/> runs once it has been granted and saved (until then the store keeps the
        /// transaction pending, so nothing can be lost even if the app is killed).
        /// </summary>
        public static bool HandlePending(string productId, string transactionId, Action confirm = null)
        {
            var p = IapCatalog.Get(productId);
            if (p == null)
            {
                // Never finish a transaction we cannot grant: Google Play refunds unacknowledged purchases, and the App
                // Store re-delivers it once a build knows the id again. (Shipped ids must never be renamed.)
                Debug.LogError("[Iap] unknown product " + productId + " (not in IapCatalog): left unconfirmed");
                return false;
            }
            var gm = GameManager.I;
            if (gm == null || GameManager.Redirecting)
            {
                bool queued = false;
                foreach (var q in Queue)
                    if (q.transactionId == transactionId) queued = true;
                if (!queued) Queue.Add(new Queued { productId = productId, transactionId = transactionId, confirm = confirm });
                return false;
            }

            bool fresh = Grant(gm, p, transactionId);
            ResolvePending(productId, fresh || p.kind == IapKind.RemoveAds ? IapResult.Success : IapResult.AlreadyOwned, p, fresh);
            return true;
        }

        public static void HandleFailed(string productId, IapResult reason)
        {
            if (reason == IapResult.Success) reason = IapResult.Failed;
            Analytics.Log(Analytics.IapFailed, "product", productId, "reason", reason.ToString());
            ResolvePending(productId, reason, IapCatalog.Get(productId), false);
        }

        public static void HandleDeferred(string productId)
        {
            var p = IapCatalog.Get(productId);
            if (p != null) Deferred?.Invoke(p);
            ResolvePending(productId, IapResult.Deferred, p, false);
        }

        /// <summary>Grants transactions that arrived before a world was loaded. Called by GameManager once it is running.</summary>
        public static void FlushQueued()
        {
            if (Queue.Count == 0 || GameManager.I == null) return;
            var copy = Queue.ToArray();
            Queue.Clear();
            foreach (var q in copy)
                if (HandlePending(q.productId, q.transactionId)) q.confirm?.Invoke();
        }

        /// <summary>
        /// Grants one transaction. Returns false when this transaction id was already granted (duplicate report).
        /// Non-consumables are idempotent anyway (Remove Ads is a flag).
        /// </summary>
        static bool Grant(GameManager gm, IapProduct p, string transactionId)
        {
            var ledger = gm.data.iapTransactions;
            if (!string.IsNullOrEmpty(transactionId) && ledger.Contains(transactionId)) return false;
            if (p.kind == IapKind.RemoveAds && gm.NoAds)
            {
                Remember(ledger, transactionId);
                gm.Save();
                return false;
            }
            switch (p.kind)
            {
                case IapKind.Apples: gm.AddApples(p.amount); break;
                case IapKind.Tickets: gm.AddTickets(p.amount); break;
                case IapKind.RemoveAds: gm.SetNoAds(true); break;
            }
            Remember(ledger, transactionId);
            gm.Save();
            Analytics.Log(Analytics.IapCompleted, "product", p.id, "kind", p.kind.ToString());
            Haptics.Play(HapticKind.Success);
            Purchased?.Invoke(p);
            return true;
        }

        static void Remember(List<string> ledger, string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            ledger.Add(transactionId);
            if (ledger.Count > LedgerSize) ledger.RemoveRange(0, ledger.Count - LedgerSize);
        }

        static void ResolvePending(string productId, IapResult result, IapProduct p, bool fresh)
        {
            if (_pending == null || _pending != productId) return;
            var done = _pendingDone;
            _pending = null;
            _pendingDone = null;
            done?.Invoke(result);
        }
    }
}
