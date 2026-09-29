using UnityEngine;

namespace JuiceKing
{
    /// <summary>Carriers standing here load items from a source (juicer output tray).</summary>
    public class PickupZone : Zone
    {
        public MonoBehaviour sourceBehaviour;
        public float entryDelay = 0.12f;
        public float interval = 0.08f;

        public IItemSource Source { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Source = sourceBehaviour as IItemSource;
        }

        protected override float TickCarrier(Carrier c, float timer)
        {
            float iv = c.isPlayer ? interval / Economy.TransferSpeed : interval;
            if (Source == null || timer < entryDelay + iv) return timer;
            while (timer >= entryDelay + iv)
            {
                timer -= iv;
                if (Source.Available <= 0) break;
                var type = Source.OutputType;
                if (c.pickupFilter != null && !c.pickupFilter(type)) break;
                if (c.maxPickup >= 0 && c.CountOf(t => t == type) >= c.maxPickup) break;
                if (c.IsFull)
                {
                    c.NotifyFull();
                    break;
                }
                var it = Source.Take(c);
                if (it == null) break;
                c.Add(it, 1.1f, 0.3f);
            }
            return timer;
        }
    }
}
