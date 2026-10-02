using UnityEngine;

namespace JuiceKing
{
    /// <summary>Carriers standing here unload matching items into a receiver (juicer hopper, counter shelf).</summary>
    public class DropZone : Zone
    {
        public MonoBehaviour receiverBehaviour;
        public float entryDelay = 0.12f;
        public float interval = 0.07f;

        public IItemReceiver Receiver { get; private set; }
        System.Predicate<ItemType> _accepts;

        protected override void Awake()
        {
            base.Awake();
            Receiver = receiverBehaviour as IItemReceiver;
            // One delegate for the zone's lifetime (a method group would allocate on every transfer).
            if (Receiver != null) _accepts = Receiver.Accepts;
        }

        protected override float TickCarrier(Carrier c, float timer)
        {
            float iv = c.isPlayer ? interval / Economy.TransferSpeed : interval;
            if (Receiver == null || timer < entryDelay + iv) return timer;
            while (timer >= entryDelay + iv)
            {
                timer -= iv;
                if (!Receiver.HasSpace) break;
                var it = c.TakeLast(_accepts);
                if (it == null) break;
                Receiver.Receive(it, c);
            }
            return timer;
        }
    }
}
