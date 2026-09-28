using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Spawns customers, manages the queue in front of the counter and handles payment.</summary>
    public class CustomerManager : MonoBehaviour
    {
        public Transform[] entryPath;
        public Transform[] queueSlots;
        public Transform[] exitPath;
        public Counter counter;
        public CashPile cash;
        public Vector2 spawnInterval = new Vector2(2.2f, 4.5f);
        public float serveInterval = 0.3f;

        readonly List<Customer> _queue = new List<Customer>();
        readonly List<Vector3> _entry = new List<Vector3>();
        readonly List<Vector3> _exit = new List<Vector3>();
        float _spawnT = 0.5f;
        float _serveT;

        public int SlotCount => queueSlots.Length;
        public Vector3 SlotPosition(int i) => queueSlots[Mathf.Clamp(i, 0, queueSlots.Length - 1)].position;
        public Customer Front => _queue.Count > 0 ? _queue[0] : null;

        void Awake()
        {
            foreach (var t in entryPath) _entry.Add(t.position);
            foreach (var t in exitPath) _exit.Add(t.position);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var gm = GameManager.I;

            if (gm.ActiveJuicers.Count > 0 && _queue.Count < queueSlots.Length)
            {
                _spawnT -= dt;
                if (_spawnT <= 0f)
                {
                    _spawnT = Random.Range(spawnInterval.x, spawnInterval.y);
                    Spawn();
                }
            }

            var front = Front;
            if (front != null && front.AtSlot && front.slot == 0)
            {
                _serveT += dt;
                if (_serveT >= serveInterval)
                {
                    _serveT = 0f;
                    if (!front.Done)
                    {
                        var juice = counter.TakeJuice(front.want);
                        if (juice != null)
                        {
                            front.Give(juice);
                            Sfx.Play(SfxId.Pop, 0.25f, 1.3f);
                        }
                    }

                    if (front.Done) CompleteSale(front);
                }
            }
            else _serveT = 0f;
        }

        void Spawn()
        {
            var refs = GameRefs.I;
            var kinds = GameManager.I.ActiveJuicers;
            // Favour the newest fruit a bit so fresh unlocks matter.
            FruitKind kind = kinds[Random.value < 0.4f ? kinds.Count - 1 : Random.Range(0, kinds.Count)];
            int sold = GameManager.I.data.totalSold;
            int maxCount = sold < 4 ? 1 : sold < 20 ? 2 : 3;
            int count = Random.Range(1, maxCount + 1);

            var prefab = refs.customerPrefabs[Random.Range(0, refs.customerPrefabs.Length)];
            var go = Instantiate(prefab, _entry[0], Quaternion.LookRotation(_entry.Count > 1 ? _entry[1] - _entry[0] : Vector3.forward));
            var c = go.GetComponent<Customer>();
            c.Init(this, kind, count, _entry, _queue.Count);
            _queue.Add(c);
        }

        void CompleteSale(Customer c)
        {
            int price = Balance.JuicePrice[(int)c.want] * c.wantCount;
            int tip = Random.value < 0.3f ? Random.Range(1, 4) : 0;
            cash.Deposit(price + tip, counter.servePoint.position + Vector3.up * 1.2f);
            GameManager.I.NotifyJuiceSold(c.wantCount);
            Sfx.Play(SfxId.Cash, 0.35f);

            _queue.Remove(c);
            c.Leave(_exit);
            for (int i = 0; i < _queue.Count; i++) _queue[i].slot = i;
        }
    }
}
