using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace JuiceKing
{
    /// <summary>
    /// <see cref="IIapProvider"/> on Unity IAP 5 (Google Play Billing / StoreKit). Installed by <see cref="AppServices"/>
    /// on Android and iOS. Follows the v5 two-step flow: a pending order is granted through <see cref="Iap.HandlePending"/>
    /// (which saves before returning) and only then confirmed with the store, so a crash between payment and grant
    /// re-delivers the order on the next launch instead of losing it. Remove Ads is re-applied from the store's purchase
    /// list on every launch (reinstall / new device) and through the Restore button.
    /// </summary>
    public class UnityIapProvider : IIapProvider
    {
        StoreController _store;
        bool _connecting, _connected, _fetched;
        readonly Dictionary<string, Product> _products = new Dictionary<string, Product>();
        readonly List<ProductDefinition> _definitions = new List<ProductDefinition>();
        // Restore Purchases reports back only once the store's purchase list has been applied.
        Action<bool> _restoreDone;

        public bool IsReady => _connected && _fetched;
        public bool IsInitializing => _connecting || (_connected && !_fetched);
        public string LastError { get; private set; }

        public UnityIapProvider()
        {
            bool apple = Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer;
            foreach (var p in IapCatalog.Products)
            {
                var overrides = apple ? ReleaseConfig.AppleProductIds : ReleaseConfig.GooglePlayProductIds;
                string storeId = overrides.TryGetValue(p.id, out var sid) && !string.IsNullOrEmpty(sid) ? sid : p.id;
                _definitions.Add(new ProductDefinition(p.id, storeId, p.Consumable ? ProductType.Consumable : ProductType.NonConsumable));
            }
        }

        public async void Connect()
        {
            if (_connecting || _connected) return;
            _connecting = true;
            LastError = null;
            try
            {
                if (_store == null)
                {
                    _store = UnityIAPServices.StoreController();
                    // Every event is subscribed before Connect: pending orders from a previous session may fire at once.
                    _store.OnStoreConnected += OnConnected;
                    _store.OnStoreDisconnected += OnDisconnected;
                    _store.OnProductsFetched += OnProductsFetched;
                    _store.OnProductsFetchFailed += OnProductsFetchFailed;
                    _store.OnPurchasesFetched += OnPurchasesFetched;
                    _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                    _store.OnPurchasePending += OnPurchasePending;
                    _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                    _store.OnPurchaseFailed += OnPurchaseFailed;
                    _store.OnPurchaseDeferred += OnPurchaseDeferred;
                }
                await _store.Connect();
            }
            catch (Exception e)
            {
                LastError = e.Message;
                _connecting = false;
                Debug.LogWarning("[Iap] store connection failed: " + e.Message);
            }
        }

        public void Reconnect()
        {
            if (!_connected && !_connecting) Connect();
            else if (_connected && !_fetched && !_connecting) _store.FetchProducts(_definitions);
        }

        void OnConnected()
        {
            _connected = true;
            _store.FetchProducts(_definitions);
            _store.FetchPurchases();
        }

        void OnDisconnected(StoreConnectionFailureDescription f)
        {
            _connected = false;
            _connecting = false;
            _fetched = false;
            LastError = f.Message;
            Debug.LogWarning("[Iap] store disconnected: " + f.Message);
        }

        void OnProductsFetched(List<Product> list)
        {
            foreach (var p in list)
                if (p != null && p.definition != null) _products[p.definition.id] = p;
            _fetched = true;
            _connecting = false;
        }

        void OnProductsFetchFailed(ProductFetchFailed f)
        {
            // Products that did come back stay purchasable; the missing ones show as unavailable.
            _fetched = true;
            _connecting = false;
            LastError = f.FailureReason;
            Debug.LogWarning("[Iap] product fetch failed: " + f.FailureReason);
        }

        void OnPurchasesFetched(Orders orders)
        {
            // Non-consumables the account already owns (reinstall, second device): re-apply. Iap is idempotent for them.
            foreach (var o in orders.ConfirmedOrders)
                foreach (var item in o.CartOrdered.Items())
                {
                    var p = IapCatalog.Get(item.Product.definition.id);
                    if (p != null && !p.Consumable) Iap.HandlePending(p.id, o.Info.TransactionID);
                }
            FinishRestore(true);
        }

        void OnPurchasesFetchFailed(PurchasesFetchFailureDescription f)
        {
            Debug.LogWarning("[Iap] purchase fetch failed: " + f.Message);
            FinishRestore(false);
        }

        void FinishRestore(bool ok)
        {
            var done = _restoreDone;
            _restoreDone = null;
            done?.Invoke(ok);
        }

        void OnPurchasePending(PendingOrder order)
        {
            string txn = order.Info.TransactionID;
            bool all = true;
            foreach (var item in order.CartOrdered.Items())
            {
                string id = item.Product.definition.id;
                var captured = order;
                // Confirm only after the grant is saved; a queued grant (boot screen) confirms when it is applied.
                if (!Iap.HandlePending(id, txn, () => _store.ConfirmPurchase(captured))) all = false;
            }
            if (all) _store.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                Debug.LogWarning("[Iap] confirmation failed: " + failed.FailureReason + " " + failed.Details);
        }

        void OnPurchaseFailed(FailedOrder order)
        {
            var reason = order.FailureReason == PurchaseFailureReason.UserCancelled ? IapResult.Cancelled
                : order.FailureReason == PurchaseFailureReason.ProductUnavailable || order.FailureReason == PurchaseFailureReason.PurchasingUnavailable
                  || order.FailureReason == PurchaseFailureReason.StoreNotConnected ? IapResult.Unavailable
                : IapResult.Failed;
            foreach (var item in order.CartOrdered.Items()) Iap.HandleFailed(item.Product.definition.id, reason);
        }

        void OnPurchaseDeferred(DeferredOrder order)
        {
            foreach (var item in order.CartOrdered.Items()) Iap.HandleDeferred(item.Product.definition.id);
        }

        public string LocalizedPrice(string productId) =>
            _products.TryGetValue(productId, out var p) && p.metadata != null ? p.metadata.localizedPriceString : null;

        public bool IsAvailable(string productId) => _products.TryGetValue(productId, out var p) && p.availableToPurchase;

        public void Purchase(string productId)
        {
            if (!IsReady || !_products.TryGetValue(productId, out var p) || !p.availableToPurchase)
            {
                Iap.HandleFailed(productId, IapResult.Unavailable);
                return;
            }
            _store.PurchaseProduct(p);
        }

        public void Restore(Action<bool> done)
        {
            if (!_connected)
            {
                done?.Invoke(false);
                return;
            }
            // Owned non-consumables arrive through OnPurchasesFetched (unfinished ones through OnPurchasePending), both
            // de-duplicated by Iap. The caller hears back once that list has been applied, so "VIP restored" is accurate.
            FinishRestore(false);
            _restoreDone = done;
            _store.RestoreTransactions((ok, error) =>
            {
                if (ok) return;
                Debug.LogWarning("[Iap] restore failed: " + error);
                FinishRestore(false);
            });
        }
    }
}
