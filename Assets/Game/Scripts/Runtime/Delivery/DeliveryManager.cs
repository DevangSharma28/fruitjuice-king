using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Runs one delivery desk once it is unlocked: waits for the next truck, sends it up the road, generates its order,
    /// lets the player (or the Loader helper) fill it, pays out and sends the truck away. The current order and the
    /// cooldown are saved, so a truck parked when the game closed is still there.
    /// A scene can have several desks (the Berry Blast has two), each with its own trucks and save slot. When the bay has a
    /// <see cref="DeliveryBox"/>, goods go into a box beside the truck that closes and is loaded aboard at the end;
    /// otherwise they fly straight into the cargo door (Tropical Farm).
    /// </summary>
    public class DeliveryManager : MonoBehaviour
    {
        /// <summary>The first desk (slot 0).</summary>
        public static DeliveryManager I { get; private set; }
        public static readonly List<DeliveryManager> All = new List<DeliveryManager>();

        public DeliveryBay bay;
        [Tooltip("Unlock id of this desk.")]
        public string unlockId = "t_delivery";
        [Tooltip("Save slot: 0 = SaveData.delivery, 1 = SaveData.delivery2.")]
        public int slot;
        [Tooltip("Shown in the delivery popup when a scene has more than one desk.")]
        public string deskName = "DELIVERY BAY";
        [Tooltip("This desk may also order cakes once the cake shop bakes them.")]
        public bool acceptsCakes;

        public DeliveryOrder Order { get; private set; }
        public DeliveryTruck Truck { get; private set; }
        /// <summary>Items currently flying into the truck / box (counted so the pad does not over-deliver).</summary>
        public int InFlight { get; private set; }
        public bool Loading => Order != null && Truck != null && Truck.state == DeliveryTruck.State.Parked && !_completing && (Box == null || Box.Open);
        public bool Unlocked => bay != null && GameManager.I != null && GameManager.I.IsUnlocked(unlockId);
        public float Cooldown => Save.cooldown;
        public float TimeLeft => Mathf.Max(0f, Economy.TruckWait - Save.waited);
        public bool Arriving => Order != null && Truck != null && Truck.state == DeliveryTruck.State.Arriving;
        DeliveryBox Box => bay != null ? bay.box : null;

        public static event Action Changed;
        public static event Action<DeliveryOrder> Completed;

        DeliverySave Save => slot == 1 ? GameManager.I.data.delivery2 : GameManager.I.data.delivery;
        /// <summary>The order sign at the bay (falls back to a board carried by the truck).</summary>
        TruckBoard Board => bay != null && bay.board != null ? bay.board : Truck != null ? Truck.board : null;
        bool _completing;
        bool _started;
        float _saveT;
        float _wrongT;

        void Awake()
        {
            if (slot == 0 || I == null) I = this;
            if (!All.Contains(this)) All.Add(this);
            All.Sort((a, b) => a.slot.CompareTo(b.slot));
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (I == this) I = All.Count > 0 ? All[0] : null;
        }

        void Start()
        {
            if (Unlocked) OnBayOpened(bay);
        }

        /// <summary>The desk that most needs the player right now (loading first, then arriving, then the next due).</summary>
        public static DeliveryManager Focus()
        {
            DeliveryManager best = null;
            float score = float.MaxValue;
            foreach (var m in All)
            {
                if (m == null || !m.Unlocked) continue;
                float s = m.Loading ? m.TimeLeft : m.Arriving ? 1000f : 2000f + Mathf.Max(0f, m.Cooldown);
                if (s < score)
                {
                    score = s;
                    best = m;
                }
            }
            return best != null ? best : I;
        }

        public static bool AnyUnlocked()
        {
            foreach (var m in All)
                if (m != null && m.Unlocked) return true;
            return false;
        }

        public static bool AnyLoading()
        {
            foreach (var m in All)
                if (m != null && m.Unlocked && m.Loading) return true;
            return false;
        }

        /// <summary>Called when the bay unlocks (or on load if it already was).</summary>
        public void OnBayOpened(DeliveryBay b)
        {
            bay = b;
            if (_started || !Unlocked) return;
            _started = true;
            foreach (var t in bay.trucks)
                if (t != null) t.gameObject.SetActive(false);
            if (Box != null) Box.HideNow();

            var s = Save;
            if (s.active)
            {
                // Resume the parked truck exactly where the save left it.
                Order = new DeliveryOrder
                {
                    truck = (TruckKind)s.truck, kind = (FruitKind)s.kind, line = (ProductLine)s.line, qty = s.qty, delivered = s.delivered,
                    reward = s.reward, client = s.client
                };
                Truck = bay.trucks[s.truck];
                Hook(Truck);
                Truck.ParkAt(bay.park.position, bay.park.rotation, Box != null ? 0f : Order.Progress);
                if (Box != null) Box.ShowOpen(Order);
                Board?.Show(Order);
            }
            else if (GameManager.I.data.stats.deliveries == 0) s.cooldown = Mathf.Min(s.cooldown, 5f);
            Changed?.Invoke();
        }

        void Hook(DeliveryTruck t)
        {
            t.Arrived -= OnArrived;
            t.Gone -= OnGone;
            t.Arrived += OnArrived;
            t.Gone += OnGone;
        }

        void Update()
        {
            if (!_started || !Unlocked) return;
            float dt = Time.deltaTime;
            var s = Save;

            if (Order == null)
            {
                s.cooldown -= dt;
                if (s.cooldown <= 0f) SendTruck();
            }
            else if (Truck != null && Truck.state == DeliveryTruck.State.Parked && !_completing)
            {
                s.waited += dt;
                if (Order.Done && InFlight == 0) Complete();
                else if (s.waited >= Economy.TruckWait && InFlight == 0) Expire();
            }

            _saveT += dt;
            if (_saveT > 1f)
            {
                _saveT = 0f;
                Changed?.Invoke();
            }
        }

        /// <summary>Something can be ordered right now (juice, or cakes at a desk that takes them).</summary>
        public bool CanOrder
        {
            get
            {
                var gm = GameManager.I;
                return gm != null && (gm.OrderableKinds.Count > 0 || (acceptsCakes && gm.OrderableCakes.Count > 0));
            }
        }

        void SendTruck()
        {
            var gm = GameManager.I;
            var juices = gm.OrderableKinds;
            var cakes = gm.OrderableCakes;
            if (!CanOrder)
            {
                Save.cooldown = 5f;
                return;
            }
            // The previous truck is still driving off: wait for it (the same truck may be needed again).
            if (Truck != null && Truck.state != DeliveryTruck.State.Hidden)
            {
                Save.cooldown = 1f;
                return;
            }
            bool first = gm.data.stats.deliveries == 0 || (slot == 1 && !Save.anyDone);
            // Cake orders once the shop bakes: rarer, smaller, richer.
            // The very first truck is an easy juice order that teaches loading.
            var line = acceptsCakes && cakes.Count > 0 && (juices.Count == 0 || (!first && UnityEngine.Random.value < 0.4f)) ? ProductLine.Cake : ProductLine.Juice;
            var kinds = line == ProductLine.Cake ? cakes : juices;
            Order = first && line == ProductLine.Juice ? DeliveryOrders.First(kinds[0]) : DeliveryOrders.Generate(line, kinds, gm.IsUnlocked("t_premium") || gm.IsUnlocked("b_premium"));
            Truck = bay.trucks[(int)Order.truck];
            if (Truck == null) Truck = bay.trucks[0];
            Hook(Truck);
            var path = new List<Vector3>();
            foreach (var p in bay.arrivePath) path.Add(p.position);
            path.Add(bay.park.position);
            Truck.Arrive(path, 0f, bay.park.rotation);
            if (Board != null)
            {
                Board.ResetOrder();
                Board.Show(Order);
            }

            var s = Save;
            s.active = true;
            s.truck = (int)Order.truck;
            s.kind = (int)Order.kind;
            s.line = (int)Order.line;
            s.qty = Order.qty;
            s.delivered = 0;
            s.reward = Order.reward;
            s.client = Order.client;
            s.waited = 0f;
            InFlight = 0;
            _completing = false;

            if (HUD.I != null)
                HUD.I.Toast("Truck incoming: " + Order.qty + " " + Order.Name + "!", Icon(Order));
            Changed?.Invoke();
        }

        void OnArrived()
        {
            Sfx.Play(SfxId.Whoosh, 0.25f, 1.2f);
            Board?.Punch();
            if (Box != null && Order != null) Box.Appear(Order);
            Changed?.Invoke();
        }

        /// <summary>A carrier on the pad hands over one item.</summary>
        public void Load(StackItem item)
        {
            if (!Loading) return;
            InFlight++;
            var order = Order;
            Action landed = () =>
            {
                InFlight = Mathf.Max(0, InFlight - 1);
                if (order != Order) return;
                order.delivered = Mathf.Min(order.qty, order.delivered + 1);
                Save.delivered = order.delivered;
                if (Box != null) Box.SetFill(order);
                else Truck.SetFill(order.Progress);
                Board?.Show(order);
                var at = Box != null ? Box.transform.position + Vector3.up * 1.6f : Truck.dropPoint.position + Vector3.up * 1.2f;
                if (order.delivered % 5 == 0 || order.Done)
                    FloatingText.Show(order.delivered + "/" + order.qty, at, new Color(0.6f, 1f, 0.6f), 0.8f, 0.8f, 0.7f);
                Changed?.Invoke();
            };
            if (Box != null) Box.Receive(item, landed);
            else Truck.Receive(item, landed);
        }

        public void NotifyWrongItem(Carrier c)
        {
            if (Time.time < _wrongT) return;
            _wrongT = Time.time + 2.5f;
            FloatingText.Show("Needs " + Order.Name, c.TopWorld() + Vector3.up * 0.6f, new Color(1f, 0.55f, 0.35f), 0.8f, 0.8f, 1f);
            Sfx.Play(SfxId.Error, 0.3f);
        }

        // ---------------------------------------------------------------- golden apple actions

        /// <summary>Premium: bring the next truck right now.</summary>
        public bool CallTruckNow()
        {
            if (Order != null || !Unlocked || !CanOrder) return false;
            if (!GameManager.I.TrySpendApples(Economy.ApplesCallTruck)) return false;
            Analytics.Log(Analytics.ApplesSpent, "amount", Economy.ApplesCallTruck, "what", "truck_call");
            Save.cooldown = 0f;
            AppleFx.Spend(bay != null ? bay.park.position + Vector3.up * 2f : transform.position);
            return true;
        }

        /// <summary>Items still missing that are not already on their way in.</summary>
        public int Missing => Order != null ? Mathf.Max(0, Order.Remaining - InFlight) : 0;

        public int FinishCost => Missing > 0 ? Economy.ApplesFinishOrder(Missing) : 0;

        /// <summary>Premium: fill what is missing at once (the box / truck fills with a flourish).</summary>
        public bool FinishNow()
        {
            if (!Loading || Order.Done || Missing <= 0) return false;
            int cost = FinishCost;
            if (!GameManager.I.TrySpendApples(cost)) return false;
            Analytics.Log(Analytics.ApplesSpent, "amount", cost, "what", "truck_finish");
            var order = Order;
            order.delivered = order.qty;
            Save.delivered = order.qty;
            var at = Box != null ? Box.transform.position : Truck.transform.position;
            AppleFx.Spend(at + Vector3.up * 1.5f);
            Fx.Sparkle(at + Vector3.up, new Color(1f, 0.85f, 0.3f), 20);
            if (Box != null) Box.SetFill(order);
            else Truck.SetFill(1f);
            Board?.Show(order);
            Changed?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- finish

        /// <summary>The order is settled: the save no longer holds it, so a restart during the send-off can never pay twice.</summary>
        void CloseOrderInSave()
        {
            var s = Save;
            s.active = false;
            s.waited = 0f;
            s.cooldown = Economy.TruckInterval;
        }

        void Complete()
        {
            _completing = true;
            var order = Order;
            long pay = (long)(order.reward * Economy.DeliveryBoostMult);
            var gm = GameManager.I;
            gm.data.stats.deliveries++;
            gm.data.stats.deliveryEarned += pay;
            Save.anyDone = true;
            Board?.ShowDone();
            // Premium trucks tip Golden Apples (the Berry Blast gives one for every delivery).
            int apples = order.truck == TruckKind.Premium ? 2 : Economy.World >= 2 ? 1 : 0;

            // Settle first, celebrate after: the money and apples are granted and saved at once.
            CloseOrderInSave();
            gm.AddMoney(pay);
            gm.NotifyIncome(pay);
            if (apples > 0) gm.AddApples(apples);
            gm.Save();
            Analytics.Log(Analytics.DeliveryComplete, "truck", order.truck.ToString(), "line", order.line.ToString(), "reward", pay);
            Haptics.Play(HapticKind.Success);

            Action payout = () =>
            {
                if (this == null) return;
                Vector3 at = Truck.transform.position + Vector3.up * 2.5f;
                Sfx.Play(SfxId.Fanfare, 0.7f);
                Fx.Confetti(at, 60);
                Fx.Stars(at, 18);
                Fx.Ring(Truck.transform.position, new Color(1f, 0.85f, 0.25f, 0.9f), 9f);
                CameraFollow.Punch(0.06f);
                FloatingText.Show("DELIVERY COMPLETE!", at + Vector3.up * 0.8f, new Color(1f, 0.9f, 0.3f), 1.5f, 1.4f, 1.6f);
                Tweener.Delay(0.6f, () =>
                {
                    if (this == null) return;
                    Sfx.Play(SfxId.BigCash, 0.7f);
                    Fx.Coins(at, 24);
                    FloatingText.Show("+$" + Economy.Money(pay), at, new Color(0.5f, 1f, 0.5f), 1.4f, 1.6f, 1.4f);
                    if (HUD.I != null) HUD.I.FlyCoins(at, 16);
                    // The apples were granted above: this is only their flight to the counter.
                    if (apples > 0) AppleFx.Show(at, apples);
                });
                Tweener.Delay(1.7f, () =>
                {
                    if (this == null) return;
                    SendAway();
                    Completed?.Invoke(order);
                    // A natural break: the only moment a forced ad may show around deliveries (paced, never after Remove Ads).
                    Tweener.Delay(1.2f, () => Ads.TryShowInterstitial(Ads.InterstitialDeliveryDone));
                });
            };

            if (Box != null) Box.CloseAndLoad(Truck, () => { if (this != null) payout(); });
            else payout();
        }

        void Expire()
        {
            // Out of time: pay the plain shop value of what was loaded and leave.
            _completing = true;
            var order = Order;
            long pay = (long)order.delivered * GameManager.I.ProductPrice(order.line, order.kind);
            CloseOrderInSave();
            if (pay > 0)
            {
                GameManager.I.AddMoney(pay);
                GameManager.I.NotifyIncome(pay);
            }
            GameManager.I.Save();
            Analytics.Log(Analytics.DeliveryExpired, "delivered", order.delivered, "qty", order.qty);
            if (HUD.I != null) HUD.I.Toast(order.client + " couldn't wait any longer" + (pay > 0 ? "  +$" + Economy.Money(pay) : ""), Icon(order));
            Sfx.Play(SfxId.Error, 0.4f, 0.8f);
            if (Box != null && order.delivered > 0) Box.CloseAndLoad(Truck, () => { if (this != null) SendAway(); }, true);
            else
            {
                if (Box != null) Box.Vanish();
                SendAway();
            }
        }

        void SendAway()
        {
            var path = new List<Vector3>();
            foreach (var p in bay.departPath) path.Add(p.position);
            if (Truck.board != null) Truck.board.gameObject.SetActive(false);
            Truck.Depart(path);
            var s = Save;
            s.active = false;
            s.cooldown = Economy.TruckInterval;
            s.waited = 0f;
            Order = null;
            GameManager.I.Save();
            Changed?.Invoke();
        }

        void OnGone()
        {
            // Only forget the truck once it is really gone (a new truck may already be on its way in).
            if (Order == null && Truck != null && Truck.state == DeliveryTruck.State.Hidden) Truck = null;
            Changed?.Invoke();
        }

        public static Sprite Icon(DeliveryOrder o) => GameRefs.I != null && o != null ? GameRefs.I.ProductIcon(o.line, o.kind) : null;
    }
}
