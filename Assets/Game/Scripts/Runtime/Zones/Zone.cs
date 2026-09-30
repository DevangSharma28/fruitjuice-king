using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    public interface IItemReceiver
    {
        bool Accepts(ItemType t);
        bool HasSpace { get; }
        void Receive(StackItem item, Carrier from);
    }

    public interface IItemSource
    {
        int Available { get; }
        /// <summary>Type of item <see cref="Take"/> would hand out.</summary>
        ItemType OutputType { get; }
        StackItem Take(Carrier to);
    }

    /// <summary>A machine that makes one product (juicer, berry press, oven). Helpers fetch its output from <see cref="OutputZone"/>.</summary>
    public interface IProducer : IItemSource
    {
        FruitKind Kind { get; }
        PickupZone OutputZone { get; }
        bool IsActive { get; }
    }

    /// <summary>
    /// Rectangular floor area that reacts to carriers (player / workers) standing on it.
    /// Uses cheap distance tests instead of physics triggers.
    /// </summary>
    public abstract class Zone : MonoBehaviour
    {
        public Vector2 size = new Vector2(2f, 2f);
        public bool playerOnly;
        public Transform padVisual;

        protected readonly List<Carrier> inside = new List<Carrier>(4);
        readonly Dictionary<Carrier, float> _timers = new Dictionary<Carrier, float>();
        Vector3 _padBaseScale;
        float _highlight;

        public bool PlayerInside { get; private set; }
        public bool AnyInside => inside.Count > 0;

        protected virtual void Awake()
        {
            if (padVisual != null) _padBaseScale = padVisual.localScale;
        }

        public bool Contains(Vector3 worldPos, float margin = 0f)
        {
            // A pad still popping in from zero scale has a singular matrix: it must not report everyone as inside.
            var s = transform.lossyScale;
            if (Mathf.Abs(s.x) < 0.05f || Mathf.Abs(s.z) < 0.05f) return false;
            Vector3 l = transform.InverseTransformPoint(worldPos);
            return Mathf.Abs(l.x) <= size.x * 0.5f + margin && Mathf.Abs(l.z) <= size.y * 0.5f + margin && Mathf.Abs(l.y) < 2.5f;
        }

        protected virtual void Update()
        {
            inside.Clear();
            bool player = false;
            var all = Carrier.All;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i];
                if (!c.interactsWithZones || (playerOnly && !c.isPlayer)) continue;
                if (!Contains(c.transform.position)) continue;
                inside.Add(c);
                if (c.isPlayer) player = true;
            }

            if (player != PlayerInside)
            {
                PlayerInside = player;
                if (player) OnPlayerEnter();
                else OnPlayerExit();
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < inside.Count; i++)
            {
                var c = inside[i];
                _timers.TryGetValue(c, out float t);
                t += dt;
                t = TickCarrier(c, t);
                _timers[c] = t;
            }

            // Drop timers of carriers that left so the first action on re-entry has the entry delay again.
            if (_timers.Count > inside.Count)
            {
                _tmpKeys.Clear();
                foreach (var k in _timers.Keys)
                    if (!inside.Contains(k)) _tmpKeys.Add(k);
                foreach (var k in _tmpKeys) _timers.Remove(k);
            }

            if (padVisual != null)
            {
                _highlight = Mathf.MoveTowards(_highlight, inside.Count > 0 ? 1f : 0f, dt * 6f);
                float pulse = 1f + _highlight * (0.06f + Mathf.Sin(Time.time * 8f) * 0.02f);
                padVisual.localScale = new Vector3(_padBaseScale.x * pulse, _padBaseScale.y, _padBaseScale.z * pulse);
            }
        }

        static readonly List<Carrier> _tmpKeys = new List<Carrier>();

        /// <summary>Process a carrier inside the zone. <paramref name="timer"/> is time accumulated; return what is left.</summary>
        protected abstract float TickCarrier(Carrier c, float timer);

        protected virtual void OnPlayerEnter() { }
        protected virtual void OnPlayerExit() { }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.05f, size.y));
        }
    }
}
