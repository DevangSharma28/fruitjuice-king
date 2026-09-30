using UnityEngine;

namespace JuiceKing
{
    /// <summary>Loading pad beside the parked truck (or its delivery box): carriers standing here hand over the ordered goods.</summary>
    public class DeliveryZone : Zone
    {
        public float entryDelay = 0.15f;
        public float interval = 0.07f;
        [Tooltip("Desk this pad loads (empty = the scene's first desk).")]
        public DeliveryManager manager;

        public DeliveryManager Manager => manager != null ? manager : DeliveryManager.I;

        protected override float TickCarrier(Carrier c, float timer)
        {
            var dm = Manager;
            if (dm == null || !dm.Loading) return timer;
            float iv = c.isPlayer ? interval / Economy.TransferSpeed : interval;
            iv /= Mathf.Lerp(1f, Boosts.WorkMult, 0.5f);
            if (timer < entryDelay + iv) return timer;
            while (timer >= entryDelay + iv)
            {
                timer -= iv;
                if (!dm.Loading || dm.Order.Remaining - dm.InFlight <= 0) break;
                var want = dm.Order.Item;
                var it = c.TakeLast(t => t == want);
                if (it == null)
                {
                    if (c.isPlayer && c.Count > 0) dm.NotifyWrongItem(c);
                    break;
                }
                dm.Load(it);
            }
            return timer;
        }
    }
}
