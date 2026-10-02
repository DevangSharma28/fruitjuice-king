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

        // Cached once: lambdas without captures, so binning a stack allocates nothing.
        static readonly System.Predicate<ItemType> Unusable = t =>
            t.IsSlice() && !GameManager.I.HasJuicer(t.Fruit()) && !GameManager.I.HasCakeMixer(t.Fruit());
        static readonly System.Predicate<ItemType> IsSlice = t => t.IsSlice();
        static readonly System.Predicate<ItemType> IsJuice = t => t.IsJuice();
        static readonly System.Predicate<ItemType> IsCake = t => t.IsCake();

        protected override float TickCarrier(Carrier c, float timer)
        {
            if (bin == null || timer < entryDelay + interval) return timer;
            while (timer >= entryDelay + interval)
            {
                timer -= interval;
                var it = c.TakeLast(Unusable) ?? c.TakeLast(IsSlice) ?? c.TakeLast(IsJuice) ?? c.TakeLast(IsCake);
                if (it == null) break;
                bin.Swallow(it, c);
            }
            return timer;
        }
    }
}
