using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace JuiceKing
{
    /// <summary>
    /// Hired helper. Farmers harvest one field and feed its machines (the juicer, and in the Berry Blast the cake mixer too).
    /// Waiters serve a counter's queue in order: they fetch exactly the product the next unserved customer needs from the
    /// machine that makes it, and if it has nothing ready they wait beside it and raise a warning instead of grabbing
    /// something else. The same role serves the juice counter (juicers / presses) or the cake shop (ovens).
    /// Loaders do the same for trucks parked at the delivery desks.
    /// </summary>
    public class WorkerAI : MonoBehaviour
    {
        public enum Role { Farmer, Waiter, Loader }
        enum Task { Idle, Gather, Deliver, Collect, Waiting }

        public Role role;
        public NavMeshAgent agent;
        public Carrier carrier;
        public Chainsaw saw;

        [Header("Farmer")]
        public FruitField field;
        public Juicer juicer;
        [Tooltip("More drop pads this farmer feeds (e.g. the cake mixer). Trips alternate between pads with room.")]
        public DropZone[] feedZones;

        [Header("Waiter")]
        public Juicer[] juicers;
        [Tooltip("Machines to fetch from (any IProducer: juicers, presses, ovens). Empty = use Juicers.")]
        public MonoBehaviour[] producers;
        public Counter counter;
        [Tooltip("Loader: where leftover cakes go (the pastry case) when Counter sells juice.")]
        public Counter altCounter;
        public CustomerManager customers;
        [Tooltip("Bubble over the head shown while waiting for juice.")]
        public GameObject warnBubble;
        public SpriteRenderer warnFruit;

        [Header("Loader")]
        public DeliveryZone deliveryZone;
        [Tooltip("Every desk this loader serves (empty = Delivery Zone).")]
        public DeliveryZone[] deliveryZones;

        public Transform idlePoint;

        Task _task;
        float _think;
        Vector3 _dest;
        bool _hasDest;
        float _baseSpeed = -1f;
        int _baseCapacity = -1;
        float _baseDps;
        float _waitT;
        bool _warnShown;
        ItemType _warnType;
        Vector3 _warnScale = Vector3.one;
        DropZone _feedTarget;
        int _trip;
        readonly List<IProducer> _producers = new List<IProducer>();
        static float _nextToast;

        void Awake()
        {
            if (warnBubble != null)
            {
                _warnScale = warnBubble.transform.localScale;
                warnBubble.SetActive(false);
            }
            if (producers != null)
                foreach (var p in producers)
                    if (p is IProducer ip) _producers.Add(ip);
            if (_producers.Count == 0 && juicers != null)
                foreach (var j in juicers)
                    if (j != null) _producers.Add(j);
        }

        void OnEnable()
        {
            if (_baseSpeed < 0f)
            {
                _baseSpeed = agent.speed;
                _baseDps = saw != null ? saw.dps : 0f;
            }
            _task = Task.Idle;
            _think = 0.3f;
            // Farmers only ever carry slices; they must not grab cups while crossing a juicer's tray pad.
            // and only their own field's kind, or a neighbour's stray berries could never be delivered (the farmer would
            // stay in Deliver forever).
            if (role == Role.Farmer)
            {
                if (field != null)
                {
                    var own = ItemTypes.Slice(field.kind);
                    carrier.pickupFilter = t => t == own;
                }
                else carrier.pickupFilter = t => t.IsSlice();
            }
            else if (role == Role.Loader) carrier.pickupFilter = t => t.IsSellable();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
                return;
            }

            agent.speed = _baseSpeed * Mathf.Lerp(1f, Boosts.WorkMult, 0.6f) * Economy.WorkerSpeedMult;
            if (_baseCapacity < 0) _baseCapacity = carrier.capacity;
            carrier.capacity = _baseCapacity + Economy.WorkerCarryBonus;
            if (saw != null) saw.dps = _baseDps * Economy.WorkerSpeedMult;

            _think -= dt;
            if (_think <= 0f)
            {
                _think = 0.25f;
                if (role == Role.Farmer) DecideFarmer();
                else if (role == Role.Loader) DecideLoader(0.25f);
                else DecideWaiter(0.25f);
                if (_hasDest) agent.SetDestination(_dest);
            }

            // Face the fruit while sawing.
            if (saw != null && saw.Target != null && agent.velocity.sqrMagnitude < 0.2f)
            {
                Vector3 d = saw.Target.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * dt));
            }

            if (_warnShown && warnBubble != null)
                warnBubble.transform.localScale = _warnScale * (1f + Mathf.Sin(Time.time * 6f) * 0.06f);
        }

        void SetDest(Vector3 p)
        {
            _dest = p;
            _hasDest = true;
        }

        bool Near(Vector3 p, float r = 1.1f)
        {
            Vector3 d = transform.position - p;
            d.y = 0f;
            return d.sqrMagnitude < r * r;
        }

        // ---------------------------------------------------------------- farmer

        /// <summary>Which pad this trip goes to: the juicer, or (alternating) another machine this farmer feeds.</summary>
        DropZone PickFeedTarget()
        {
            var main = juicer != null ? juicer.inputZone : null;
            if (feedZones == null || feedZones.Length == 0) return main;
            var options = new List<DropZone>(feedZones.Length + 1);
            if (main != null && main.isActiveAndEnabled && juicer.isActiveAndEnabled) options.Add(main);
            foreach (var z in feedZones)
                if (z != null && z.isActiveAndEnabled && z.Receiver != null) options.Add(z);
            if (options.Count == 0) return main;
            for (int i = 0; i < options.Count; i++)
            {
                var z = options[(_trip + i) % options.Count];
                if (z.Receiver == null || z.Receiver.HasSpace) return z;
            }
            return options[_trip % options.Count];
        }

        void DecideFarmer()
        {
            Vector3 pos = transform.position;
            if (_task == Task.Deliver)
            {
                if (carrier.Count == 0)
                {
                    _task = Task.Gather;
                    _feedTarget = null;
                    _trip++;
                }
                else
                {
                    if (_feedTarget == null || !_feedTarget.isActiveAndEnabled) _feedTarget = PickFeedTarget();
                    if (_feedTarget != null) SetDest(_feedTarget.transform.position);
                    return;
                }
            }

            var fruit = field.NearestReady(pos);
            var loose = LooseItems.Nearest(pos, ItemTypes.Slice(field.kind), 9f);
            if (carrier.IsFull || (carrier.Count > 0 && fruit == null && loose == null))
            {
                _task = Task.Deliver;
                _feedTarget = PickFeedTarget();
                if (_feedTarget != null) SetDest(_feedTarget.transform.position);
                return;
            }

            _task = Task.Gather;
            if (loose != null) SetDest(loose.transform.position);
            else if (fruit != null)
            {
                Vector3 away = pos - fruit.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                SetDest(fruit.transform.position + away.normalized * (fruit.radius + 0.45f));
            }
            else if (idlePoint != null) SetDest(idlePoint.position);
        }

        // ---------------------------------------------------------------- waiter

        /// <summary>
        /// Walk the queue front to back, covering each order from the stock already on the counter (and in our hands);
        /// the first customer still short defines the job.
        /// </summary>
        bool FindDemand(out FruitKind kind, out int need)
        {
            kind = FruitKind.Orange;
            need = 0;
            if (customers == null || counter == null) return false;
            var stock = new int[ItemTypes.FruitCount];
            for (int k = 0; k < stock.Length; k++)
            {
                var jt = counter.ProductOf((FruitKind)k);
                stock[k] = counter.display.CountOf(t => t == jt) + carrier.CountOf(t => t == jt);
            }
            var q = customers.Queue;
            for (int i = 0; i < q.Count; i++)
            {
                var c = q[i];
                int k = (int)c.want;
                int rem = c.wantCount - c.got;
                int use = Mathf.Min(rem, stock[k]);
                stock[k] -= use;
                rem -= use;
                if (rem <= 0) continue;
                kind = c.want;
                need = rem;
                return true;
            }
            return false;
        }

        /// <summary>The active machine that makes <paramref name="wanted"/>.</summary>
        IProducer ProducerFor(ItemType wanted)
        {
            foreach (var p in _producers)
                if (p != null && p.IsActive && p.OutputType == wanted) return p;
            return null;
        }

        void DecideWaiter(float dt)
        {
            bool hasDemand = FindDemand(out var kind, out int need);
            var wanted = counter != null ? counter.ProductOf(kind) : ItemTypes.Juice(kind);
            int carryingWanted = carrier.CountOf(t => t == wanted);

            // Deliver when the job is covered, the hands are full, or we are holding something we should not keep.
            bool holdingOther = carrier.Count > carryingWanted;
            bool deliver = carrier.Count > 0 && (!hasDemand || need <= 0 || carrier.IsFull || holdingOther ||
                                                 (_task == Task.Deliver && carrier.Count > 0));
            var j = hasDemand ? ProducerFor(wanted) : null;
            // Partial load and nothing more coming soon: hand over what we have.
            if (!deliver && carryingWanted > 0 && j != null && j.Available == 0) deliver = true;

            if (deliver)
            {
                _task = Task.Deliver;
                carrier.maxPickup = 0;
                SetWarn(false, wanted);
                SetDest(counter.dropZone.transform.position);
                return;
            }

            if (!hasDemand || j == null)
            {
                _task = Task.Idle;
                carrier.maxPickup = 0;
                SetWarn(false, wanted);
                if (idlePoint != null) SetDest(idlePoint.position);
                return;
            }

            // Collect exactly this product, up to what the customer still needs.
            carrier.pickupFilter = t => t == wanted;
            carrier.maxPickup = Mathf.Min(carrier.capacity, carryingWanted + need);
            var pad = j.OutputZone.transform.position;
            SetDest(pad);

            if (Near(pad) && j.Available == 0)
            {
                _task = Task.Waiting;
                _waitT += dt;
                if (_waitT > 0.8f) SetWarn(true, wanted);
            }
            else
            {
                _task = Task.Collect;
                _waitT = 0f;
                SetWarn(false, wanted);
            }
        }

        // ---------------------------------------------------------------- loader

        /// <summary>The desk that needs goods most: a parked truck being loaded (fewest remaining first).</summary>
        DeliveryZone PickDesk()
        {
            DeliveryZone best = null;
            int bestLeft = int.MaxValue;
            if (deliveryZones != null && deliveryZones.Length > 0)
            {
                foreach (var z in deliveryZones)
                {
                    if (z == null || !z.isActiveAndEnabled) continue;
                    var m = z.Manager;
                    if (m == null || !m.Loading) continue;
                    int left = m.Order.Remaining - m.InFlight;
                    if (left > 0 && left < bestLeft)
                    {
                        bestLeft = left;
                        best = z;
                    }
                }
                return best;
            }
            return deliveryZone != null && deliveryZone.Manager != null && deliveryZone.Manager.Loading ? deliveryZone : null;
        }

        DeliveryZone _desk;

        void DecideLoader(float dt)
        {
            if (_desk == null || _desk.Manager == null || !_desk.Manager.Loading) _desk = PickDesk();
            var dm = _desk != null ? _desk.Manager : null;
            bool loading = dm != null && dm.Loading;
            var wanted = loading ? dm.Order.Item : ItemType.CoconutJuice;
            int carryingWanted = carrier.CountOf(t => t == wanted);
            int need = loading ? dm.Order.Remaining - dm.InFlight - carryingWanted : 0;

            // Leftovers from a finished order go back to the shop counter that sells them.
            if (carrier.Count > carryingWanted || (!loading && carrier.Count > 0))
            {
                _task = Task.Deliver;
                carrier.maxPickup = 0;
                SetWarn(false, wanted);
                var back = altCounter != null && carrier.Contains(t => t.IsCake()) ? altCounter : counter;
                if (back != null) SetDest(back.dropZone.transform.position);
                return;
            }
            if (!loading)
            {
                _task = Task.Idle;
                carrier.maxPickup = 0;
                SetWarn(false, wanted);
                if (idlePoint != null) SetDest(idlePoint.position);
                return;
            }

            var j = ProducerFor(wanted);
            bool deliver = carryingWanted > 0 && (carrier.IsFull || need <= 0 || _task == Task.Deliver || (j != null && j.Available == 0 && Near(j.OutputZone.transform.position)));
            if (deliver)
            {
                _task = Task.Deliver;
                carrier.maxPickup = 0;
                SetWarn(false, wanted);
                SetDest(_desk.transform.position);
                return;
            }
            if (j == null || need <= 0)
            {
                _task = Task.Idle;
                carrier.maxPickup = 0;
                if (idlePoint != null) SetDest(idlePoint.position);
                return;
            }

            carrier.pickupFilter = t => t == wanted;
            carrier.maxPickup = Mathf.Min(carrier.capacity, carryingWanted + need);
            var pad = j.OutputZone.transform.position;
            SetDest(pad);
            if (Near(pad) && j.Available == 0)
            {
                _task = Task.Waiting;
                _waitT += dt;
                if (_waitT > 0.8f) SetWarn(true, wanted);
            }
            else
            {
                _task = Task.Collect;
                _waitT = 0f;
                SetWarn(false, wanted);
            }
        }

        void SetWarn(bool on, ItemType wanted)
        {
            if (!on) _waitT = 0f;
            if (on == _warnShown && (!on || wanted == _warnType)) return;
            _warnShown = on;
            _warnType = wanted;
            if (warnBubble == null) return;
            if (on)
            {
                var kind = wanted.Fruit();
                bool cake = wanted.IsCake();
                if (warnFruit != null && GameRefs.I != null) warnFruit.sprite = cake ? GameRefs.I.CakeIcon(kind) : GameRefs.I.fruitIcons[(int)kind];
                warnBubble.SetActive(true);
                Tweener.Scale(warnBubble.transform, Vector3.zero, _warnScale, 0.35f, Ease.OutBack);
                if (Time.time >= _nextToast)
                {
                    _nextToast = Time.time + 12f;
                    Sfx.Play(SfxId.Error, 0.25f, 1.25f);
                    string who = role == Role.Loader ? "Loader" : counter != null && counter.line == ProductLine.Cake ? "Baker" : "Waiter";
                    string what = cake ? Balance.CakeNames[(int)kind] : Balance.FruitNames[(int)kind] + " juice";
                    if (HUD.I != null)
                        HUD.I.Toast(who + " needs " + what + "!", cake ? GameRefs.I.CakeIcon(kind) : GameRefs.I.fruitIcons[(int)kind]);
                }
            }
            else warnBubble.SetActive(false);
        }
    }
}
