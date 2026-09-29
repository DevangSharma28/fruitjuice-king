using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Spawns customers (pooled), keeps the queue in front of the counter topped up and handles payment.
    /// Orders are random: any open flavour, 1..N cups where N grows with sales. The front customer loses
    /// patience if they get nothing for a while, so one unmet order never blocks the line for good.
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        public Transform[] entryPath;
        public Transform[] queueSlots;
        public Transform[] exitPath;
        public Counter counter;
        public CashPile cash;
        public float serveInterval = 0.22f;

        readonly List<Customer> _queue = new List<Customer>();
        readonly List<Vector3> _entry = new List<Vector3>();
        readonly List<Vector3> _exit = new List<Vector3>();
        float _spawnT = 0.3f;
        float _serveT;
        int _lastPrefab = -1;

        public int SlotCount => queueSlots.Length;
        public Vector3 SlotPosition(int i) => queueSlots[Mathf.Clamp(i, 0, queueSlots.Length - 1)].position;
        public Customer Front => _queue.Count > 0 ? _queue[0] : null;
        public IReadOnlyList<Customer> Queue => _queue;

        void Awake()
        {
            foreach (var t in entryPath) _entry.Add(t.position);
            foreach (var t in exitPath) _exit.Add(t.position);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var gm = GameManager.I;

            // Keep the line full: a new customer every fraction of a second while there is room.
            if (gm.OrderableKinds.Count > 0 && _queue.Count < queueSlots.Length)
            {
                _spawnT -= dt * Boosts.WorkMult;
                if (_spawnT <= 0f)
                {
                    _spawnT = Random.Range(Balance.CustomerSpawnGap.x, Balance.CustomerSpawnGap.y);
                    Spawn();
                }
            }

            var front = Front;
            if (front != null && front.AtSlot && front.slot == 0)
            {
                _serveT += dt * Boosts.WorkMult;
                if (_serveT >= serveInterval)
                {
                    _serveT = 0f;
                    if (!front.Done)
                    {
                        var juice = counter.TakeJuice(front.want);
                        if (juice != null)
                        {
                            front.Give(juice);
                            Sfx.Play(SfxId.Pop, 0.22f, 1.3f + front.got * 0.08f);
                        }
                    }

                    if (front.Done) CompleteSale(front, true);
                    else if (front.OutOfPatience) CompleteSale(front, false);
                }
            }
            else _serveT = 0f;
        }

        void Spawn()
        {
            var refs = GameRefs.I;
            var kinds = GameManager.I.OrderableKinds;
            FruitKind kind = kinds[Random.Range(0, kinds.Count)];
            int max = Balance.MaxOrder(GameManager.I.data.totalSold);
            int count = Random.Range(1, max + 1);

            // Avoid the same look twice in a row.
            int pi = Random.Range(0, refs.customerPrefabs.Length);
            if (pi == _lastPrefab) pi = (pi + 1) % refs.customerPrefabs.Length;
            _lastPrefab = pi;
            var prefab = refs.customerPrefabs[pi];
            var rot = Quaternion.LookRotation(_entry.Count > 1 ? _entry[1] - _entry[0] : Vector3.forward);
            var go = Pool.Spawn(prefab, _entry[0], rot);
            var c = go.GetComponent<Customer>();
            c.Init(this, kind, count, _entry, _queue.Count);
            _queue.Add(c);
        }

        /// <summary>Pay for what was handed over (a full order, or a partial one when patience ran out).</summary>
        void CompleteSale(Customer c, bool happy)
        {
            var gm = GameManager.I;
            if (c.got > 0)
            {
                int price = gm.JuicePrice(c.want) * c.got;
                int tip = happy && Random.value < 0.3f ? Random.Range(1, 4) : 0;
                float mult = Boosts.MoneyMult;
                int total = Mathf.RoundToInt((price + tip) * mult);
                Vector3 from = counter.servePoint.position + Vector3.up * 1.2f;
                cash.Deposit(total, from);
                gm.NotifyJuiceSold(c.got);
                Sfx.Play(SfxId.Cash, 0.32f);
                if (mult > 1f)
                    FloatingText.Show("x2!", from + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.2f), 0.9f, 1f, 0.9f);
                else if (tip > 0)
                    FloatingText.Show("TIP!", from + Vector3.up * 0.8f, new Color(0.55f, 1f, 0.6f), 0.7f, 0.9f, 0.8f);
            }

            _queue.Remove(c);
            c.Leave(_exit, happy);
            for (int i = 0; i < _queue.Count; i++) _queue[i].slot = i;
        }
    }
}
