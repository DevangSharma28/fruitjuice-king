using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Player-only pad in front of the <see cref="TrashBin"/>. After a short hold it bins the stack one item at a time,
    /// unusable items first (slices with no juicer), then other slices, and juice cups last.
    /// </summary>
    public class TrashZone : Zone
    {
        public TrashBin bin;
        public float entryDelay = 0.45f;
        public float interval = 0.06f;

        protected override void Awake()
        {
            base.Awake();
            playerOnly = true;
        }

        protected override float TickCarrier(Carrier c, float timer)
        {
            if (bin == null || timer < entryDelay + interval) return timer;
            while (timer >= entryDelay + interval)
            {
                timer -= interval;
                var gm = GameManager.I;
                var it = c.TakeLast(t => t.IsSlice() && !gm.HasJuicer(t.Fruit()))
                         ?? c.TakeLast(t => t.IsSlice())
                         ?? c.TakeLast(t => t.IsJuice())
                         ?? c.TakeLast(t => t.IsCake());
                if (it == null) break;
                bin.Swallow(it, c);
            }
            return timer;
        }
    }
}
