using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Runs the delivery loop once the Delivery Bay is unlocked: waits for the next truck, sends it up the road,
    /// generates its order, lets the player (or the Loader helper) fill it at the loading pad, pays out and sends the
    /// truck away. The current order and the cooldown are saved, so a truck parked when the game closed is still there.
    /// </summary>
    public class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager I { get; private set; }

        public DeliveryBay bay;
        [Tooltip("Unlock id of the delivery bay.")]
        public string unlockId = "t_delivery";

        public DeliveryOrder Order { get; private set; }
        public DeliveryTruck Truck { get; private set; }
        /// <summary>Cups currently flying into the truck (counted so the pad does not over-deliver).</summary>
        public int InFlight { get; private set; }
        public bool Loading => Order != null && Truck != null && Truck.state == DeliveryTruck.State.Parked && !_completing;
        public bool Unlocked => bay != null && GameManager.I != null && GameManager.I.IsUnlocked(unlockId);
        public float Cooldown => Save.cooldown;
        public float TimeLeft => Mathf.Max(0f, Economy.TruckWait - Save.waited);

        public static event Action Changed;
        public static event Action<DeliveryOrder> Completed;

        DeliverySave Save => GameManager.I.data.delivery;
        /// <summary>The order sign at the bay (falls back to a board carried by the truck).</summary>
        TruckBoard Board => bay != null && bay.board != null ? bay.board : Truck != null ? Truck.board : null;
        bool _completing;
        bool _started;
        float _saveT;
        float _wrongT;

        void Awake() => I = this;

        void Start()
        {
            if (Unlocked) OnBayOpened(bay);
        }

        /// <summary>Called when the bay unlocks (or on load if it already was).</summary>
        public void OnBayOpened(DeliveryBay b)
        {
            bay = b;
            if (_started || !Unlocked) return;
            _started = true;
            foreach (var t in bay.trucks)
                if (t != null) t.gameObject.SetActive(false);

            var s = Save;
            if (s.active)
            {
                // Resume the parked truck exactly where the save left it.
                Order = new DeliveryOrder { truck = (TruckKind)s.truck, kind = (FruitKind)s.kind, qty = s.qty, delivered = s.delivered, reward = s.reward, client = s.client };
                Truck = bay.trucks[s.truck];
                Hook(Truck);
                Truck.ParkAt(bay.park.position, bay.park.rotation, Order.Progress);
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
                if (Order.Done) Complete();
                else if (s.waited >= Economy.TruckWait && InFlight == 0) Expire();
            }

            _saveT += dt;
            if (_saveT > 1f)
            {
                _saveT = 0f;
                Changed?.Invoke();
            }
        }

        void SendTruck()
        {
            var kinds = GameManager.I.OrderableKinds;
            if (kinds.Count == 0)
            {
                Save.cooldown = 5f;
                return;
            }
            bool first = GameManager.I.data.stats.deliveries == 0;
            Order = first ? DeliveryOrders.First(kinds[0]) : DeliveryOrders.Generate(kinds, GameManager.I.IsUnlocked("t_premium"));
            Truck = bay.trucks[(int)Order.truck];
            if (Truck == null) Truck = bay.trucks[0];
            Hook(Truck);
            var path = new List<Vector3>();
            foreach (var p in bay.arrivePath) path.Add(p.position);
            path.Add(bay.park.position);
            Truck.Arrive(path, 0f);
            if (Board != null)
            {
                Board.ResetOrder();
                Board.Show(Order);
            }

            var s = Save;
            s.active = true;
            s.truck = (int)Order.truck;
            s.kind = (int)Order.kind;
            s.qty = Order.qty;
            s.delivered = 0;
            s.reward = Order.reward;
            s.client = Order.client;
            s.waited = 0f;
            InFlight = 0;
            _completing = false;

            if (HUD.I != null)
                HUD.I.Toast("Truck incoming: " + Order.qty + " " + Balance.JuiceNames[(int)Order.kind] + "!", JuiceIcon(Order.kind));
            Changed?.Invoke();
        }

        void OnArrived()
        {
            Sfx.Play(SfxId.Whoosh, 0.25f, 1.2f);
            Board?.Punch();
            Changed?.Invoke();
        }

        /// <summary>A carrier on the pad hands over one cup.</summary>
        public void Load(StackItem cup)
        {
            if (!Loading) return;
            InFlight++;
            var order = Order;
            Truck.Receive(cup, () =>
            {
                InFlight = Mathf.Max(0, InFlight - 1);
                if (order != Order) return;
                order.delivered = Mathf.Min(order.qty, order.delivered + 1);
                Save.delivered = order.delivered;
                Truck.SetFill(order.Progress);
                Board?.Show(order);
                if (order.delivered % 5 == 0 || order.Done)
                    FloatingText.Show(order.delivered + "/" + order.qty, Truck.dropPoint.position + Vector3.up * 1.2f, new Color(0.6f, 1f, 0.6f), 0.8f, 0.8f, 0.7f);
                Changed?.Invoke();
            });
        }

        public void NotifyWrongJuice(Carrier c)
        {
            if (Time.time < _wrongT) return;
            _wrongT = Time.time + 2.5f;
            FloatingText.Show("Needs " + Balance.JuiceNames[(int)Order.kind], c.TopWorld() + Vector3.up * 0.6f, new Color(1f, 0.55f, 0.35f), 0.8f, 0.8f, 1f);
            Sfx.Play(SfxId.Error, 0.3f);
        }

        void Complete()
        {
            _completing = true;
            var order = Order;
            long pay = (long)(order.reward * Economy.DeliveryBoostMult);
            var gm = GameManager.I;
            gm.data.stats.deliveries++;
            gm.data.stats.deliveryEarned += pay;
            Board?.ShowDone();

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
                gm.AddMoney(pay);
                Sfx.Play(SfxId.BigCash, 0.7f);
                Fx.Coins(at, 24);
                FloatingText.Show("+$" + Economy.Money(pay), at, new Color(0.5f, 1f, 0.5f), 1.4f, 1.6f, 1.4f);
                if (HUD.I != null) HUD.I.FlyCoins(at, 16);
            });
            Tweener.Delay(1.6f, () =>
            {
                if (this == null) return;
                SendAway();
                Completed?.Invoke(order);
            });
        }

        void Expire()
        {
            // Out of time: pay the plain shop value of what was loaded and leave.
            _completing = true;
            long pay = (long)Order.delivered * GameManager.I.JuicePrice(Order.kind);
            if (pay > 0) GameManager.I.AddMoney(pay);
            if (HUD.I != null) HUD.I.Toast(Order.client + " couldn't wait any longer" + (pay > 0 ? "  +$" + Economy.Money(pay) : ""), JuiceIcon(Order.kind));
            Sfx.Play(SfxId.Error, 0.4f, 0.8f);
            SendAway();
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
            _completing = false;
            if (Order == null) Truck = null;
            Changed?.Invoke();
        }

        static Sprite JuiceIcon(FruitKind k) => GameRefs.I != null ? GameRefs.I.JuiceIcon(k) : null;
    }
}
