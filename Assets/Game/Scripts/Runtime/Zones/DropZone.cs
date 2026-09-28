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

        protected override void Awake()
        {
            base.Awake();
            Receiver = receiverBehaviour as IItemReceiver;
        }

        protected override float TickCarrier(Carrier c, float timer)
        {
            if (Receiver == null || timer < entryDelay + interval) return timer;
            while (timer >= entryDelay + interval)
            {
                timer -= interval;
                if (!Receiver.HasSpace) break;
                var it = c.TakeLast(Receiver.Accepts);
                if (it == null) break;
                Receiver.Receive(it, c);
            }
            return timer;
        }
    }
}
