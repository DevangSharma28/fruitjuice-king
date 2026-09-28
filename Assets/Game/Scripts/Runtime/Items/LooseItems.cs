using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Items lying on the ground (fresh fruit slices). Nearby carriers with room magnet them up.</summary>
    public class LooseItems : MonoBehaviour
    {
        static LooseItems _i;
        public float pickupRadius = 2.3f;

        readonly List<StackItem> _items = new List<StackItem>(64);

        public static IReadOnlyList<StackItem> Items => _i != null ? _i._items : (IReadOnlyList<StackItem>)System.Array.Empty<StackItem>();

        void Awake() => _i = this;

        public static void Drop(StackItem it, Vector3 landing, float delay = 0f)
        {
            it.inTransit = true;
            it.onGround = false;
            it.transform.SetParent(null, true);
            it.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Tweener.ArcTo(it.transform, landing, Random.Range(1.2f, 1.9f), Random.Range(0.4f, 0.55f), () =>
            {
                it.inTransit = false;
                it.onGround = true;
                if (_i != null) _i._items.Add(it);
                Tweener.Punch(it.transform, 0.25f, 0.25f);
            }, delay);
        }

        void Update()
        {
            if (_items.Count == 0) return;
            var carriers = Carrier.All;
            float r2 = pickupRadius * pickupRadius;

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var it = _items[i];
                if (it == null || !it.gameObject.activeInHierarchy || !it.onGround)
                {
                    _items.RemoveAt(i);
                    continue;
                }

                Carrier best = null;
                float bestD = r2;
                bool fullPlayerNear = false;
                Vector3 p = it.transform.position;
                for (int c = 0; c < carriers.Count; c++)
                {
                    var car = carriers[c];
                    if (!car.canCollectLoose) continue;
                    Vector3 d = car.transform.position - p;
                    d.y = 0f;
                    float dd = d.sqrMagnitude;
                    if (dd > r2) continue;
                    if (car.IsFull)
                    {
                        if (car.isPlayer) fullPlayerNear = true;
                        continue;
                    }
                    if (dd < bestD)
                    {
                        bestD = dd;
                        best = car;
                    }
                }

                if (best != null)
                {
                    _items.RemoveAt(i);
                    best.Add(it, 1.4f, 0.3f);
                }
                else if (fullPlayerNear)
                {
                    GameRefs.I.player.NotifyFull();
                }
            }
        }

        /// <summary>Nearest loose item of a type (used by workers).</summary>
        public static StackItem Nearest(Vector3 from, ItemType type, float maxDist)
        {
            if (_i == null) return null;
            StackItem best = null;
            float bd = maxDist * maxDist;
            foreach (var it in _i._items)
            {
                if (it == null || it.type != type) continue;
                float d = (it.transform.position - from).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = it;
                }
            }
            return best;
        }
    }
}
