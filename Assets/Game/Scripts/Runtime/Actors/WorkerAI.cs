using UnityEngine;
using UnityEngine.AI;

namespace JuiceKing
{
    /// <summary>
    /// Hired helper. Farmers harvest one field and feed its juicer.
    /// Waiters serve the queue in order: they fetch exactly the flavour the next unserved customer needs, and if that
    /// juicer has nothing ready they wait beside it and raise a warning instead of grabbing other juice.
    /// </summary>
    public class WorkerAI : MonoBehaviour
    {
        public enum Role { Farmer, Waiter }
        enum Task { Idle, Gather, Deliver, Collect, Waiting }

        public Role role;
        public NavMeshAgent agent;
        public Carrier carrier;
        public Chainsaw saw;

        [Header("Farmer")]
        public FruitField field;
        public Juicer juicer;

        [Header("Waiter")]
        public Juicer[] juicers;
        public Counter counter;
        public CustomerManager customers;
        [Tooltip("Bubble over the head shown while waiting for juice.")]
        public GameObject warnBubble;
        public SpriteRenderer warnFruit;

        public Transform idlePoint;

        Task _task;
        float _think;
        Vector3 _dest;
        bool _hasDest;
        float _baseSpeed = -1f;
        float _waitT;
        bool _warnShown;
        FruitKind _warnKind;
        Vector3 _warnScale = Vector3.one;
        static float _nextToast;

        void Awake()
        {
            if (warnBubble != null)
            {
                _warnScale = warnBubble.transform.localScale;
                warnBubble.SetActive(false);
            }
        }

        void OnEnable()
        {
            if (_baseSpeed < 0f) _baseSpeed = agent.speed;
            _task = Task.Idle;
            _think = 0.3f;
            // Farmers only ever carry slices; they must not grab cups while crossing a juicer's tray pad.
            if (role == Role.Farmer) carrier.pickupFilter = t => t.IsSlice();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
                return;
            }

            agent.speed = _baseSpeed * Mathf.Lerp(1f, Boosts.WorkMult, 0.6f);

            _think -= dt;
            if (_think <= 0f)
            {
                _think = 0.25f;
                if (role == Role.Farmer) DecideFarmer();
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

        void DecideFarmer()
        {
            Vector3 pos = transform.position;
            if (_task == Task.Deliver)
            {
                if (carrier.Count == 0) _task = Task.Gather;
                else
                {
                    SetDest(juicer.inputZone.transform.position);
                    return;
                }
            }

            var fruit = field.NearestReady(pos);
            var loose = LooseItems.Nearest(pos, ItemTypes.Slice(field.kind), 9f);
            if (carrier.IsFull || (carrier.Count > 0 && fruit == null && loose == null))
            {
                _task = Task.Deliver;
                SetDest(juicer.inputZone.transform.position);
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
        /// Walk the queue front to back, covering each order from the cups already on the counter (and in our hands);
        /// the first customer still short defines the job.
        /// </summary>
        bool FindDemand(out FruitKind kind, out int need)
        {
            kind = FruitKind.Orange;
            need = 0;
            if (customers == null) return false;
            var stock = new int[ItemTypes.FruitCount];
            for (int k = 0; k < stock.Length; k++)
            {
                var jt = ItemTypes.Juice((FruitKind)k);
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

        Juicer JuicerFor(FruitKind k)
        {
            foreach (var j in juicers)
                if (j != null && j.kind == k && j.isActiveAndEnabled) return j;
            return null;
        }

        void DecideWaiter(float dt)
        {
            bool hasDemand = FindDemand(out var kind, out int need);
            var wanted = ItemTypes.Juice(kind);
            int carryingWanted = carrier.CountOf(t => t == wanted);

            // Deliver when the job is covered, the hands are full, or we are holding something we should not keep.
            bool holdingOther = carrier.Count > carryingWanted;
            bool deliver = carrier.Count > 0 && (!hasDemand || need <= 0 || carrier.IsFull || holdingOther ||
                                                 (_task == Task.Deliver && carrier.Count > 0));
            var j = hasDemand ? JuicerFor(kind) : null;
            // Partial load and nothing more coming soon: hand over what we have.
            if (!deliver && carryingWanted > 0 && j != null && j.Available == 0) deliver = true;

            if (deliver)
            {
                _task = Task.Deliver;
                carrier.maxPickup = 0;
                SetWarn(false, kind);
                SetDest(counter.dropZone.transform.position);
                return;
            }

            if (!hasDemand || j == null)
            {
                _task = Task.Idle;
                carrier.maxPickup = 0;
                SetWarn(false, kind);
                if (idlePoint != null) SetDest(idlePoint.position);
                return;
            }

            // Collect exactly this flavour, up to what the customer still needs.
            carrier.pickupFilter = t => t == wanted;
            carrier.maxPickup = Mathf.Min(carrier.capacity, carryingWanted + need);
            var pad = j.outputZone.transform.position;
            SetDest(pad);

            if (Near(pad) && j.Available == 0)
            {
                _task = Task.Waiting;
                _waitT += dt;
                if (_waitT > 0.8f) SetWarn(true, kind);
            }
            else
            {
                _task = Task.Collect;
                _waitT = 0f;
                SetWarn(false, kind);
            }
        }

        void SetWarn(bool on, FruitKind kind)
        {
            if (!on) _waitT = 0f;
            if (on == _warnShown && (!on || kind == _warnKind)) return;
            _warnShown = on;
            _warnKind = kind;
            if (warnBubble == null) return;
            if (on)
            {
                if (warnFruit != null && GameRefs.I != null) warnFruit.sprite = GameRefs.I.fruitIcons[(int)kind];
                warnBubble.SetActive(true);
                Tweener.Scale(warnBubble.transform, Vector3.zero, _warnScale, 0.35f, Ease.OutBack);
                if (Time.time >= _nextToast)
                {
                    _nextToast = Time.time + 12f;
                    Sfx.Play(SfxId.Error, 0.25f, 1.25f);
                    if (HUD.I != null)
                        HUD.I.Toast("Waiter needs " + Balance.FruitNames[(int)kind] + " juice!", GameRefs.I.fruitIcons[(int)kind]);
                }
            }
            else warnBubble.SetActive(false);
        }
    }
}
