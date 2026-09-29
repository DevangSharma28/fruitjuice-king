using UnityEngine;

namespace JuiceKing
{
    /// <summary>Loading pad beside the parked truck: carriers standing here hand over cups of the ordered juice.</summary>
    public class DeliveryZone : Zone
    {
        public float entryDelay = 0.15f;
        public float interval = 0.07f;

        protected override float TickCarrier(Carrier c, float timer)
        {
            var dm = DeliveryManager.I;
            if (dm == null || !dm.Loading) return timer;
            float iv = c.isPlayer ? interval / Economy.TransferSpeed : interval;
            iv /= Mathf.Lerp(1f, Boosts.WorkMult, 0.5f);
            if (timer < entryDelay + iv) return timer;
            while (timer >= entryDelay + iv)
            {
                timer -= iv;
                if (!dm.Loading || dm.Order.Remaining - dm.InFlight <= 0) break;
                var want = ItemTypes.Juice(dm.Order.kind);
                var it = c.TakeLast(t => t == want);
                if (it == null)
                {
                    if (c.isPlayer && c.Count > 0) dm.NotifyWrongJuice(c);
                    break;
                }
                dm.Load(it);
            }
            return timer;
        }
    }
}
