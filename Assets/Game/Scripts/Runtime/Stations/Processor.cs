using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// A machine that turns <see cref="InputPerBatch"/> items of one type into one item of another (cake mixer: berries
    /// to batter; oven: batter to cake). Subclasses supply the numbers and the show: <see cref="OnBatchStart"/>,
    /// <see cref="AnimateWork"/>, <see cref="OnBatchDone"/>.
    /// </summary>
    public abstract class Processor : MonoBehaviour, IItemReceiver, IProducer
    {
        public FruitKind kind;
        [Tooltip("Where fed items wait (null when another machine feeds this one directly).")]
        public ItemPile inputPile;
        public ItemPile outputPile;
        public DropZone inputZone;
        [Tooltip("Pad where carriers take the product (null when the product moves on by conveyor).")]
        public PickupZone outputZone;
        public Transform intakePoint;
        public Transform outPoint;
        public AudioSource hum;

        protected bool working;
        protected float timer;

        public abstract ItemType InputType { get; }
        public abstract ItemType OutputType { get; }
        protected abstract int InputPerBatch { get; }
        protected abstract float BatchTime { get; }
        /// <summary>Layers of the output pile (upgrades can grow it).</summary>
        protected virtual int OutputLayers => outputPile != null ? outputPile.layers : 1;

        public bool Working => working;
        public float Progress => working ? Mathf.Clamp01(timer / Mathf.Max(0.01f, BatchTime)) : 0f;

        // ---------- IItemReceiver ----------
        public bool Accepts(ItemType t) => t == InputType;
        public bool HasSpace => inputPile != null && inputPile.HasSpace;

        public void Receive(StackItem item, Carrier from)
        {
            bool player = from != null && from.isPlayer;
            inputPile.Add(item, 1.3f, 0.3f, () =>
            {
                OnFed();
                if (player) Sfx.Play(SfxId.Drop, 0.4f, Random.Range(0.95f, 1.12f));
            });
        }

        // ---------- IProducer ----------
        public FruitKind Kind => kind;
        public PickupZone OutputZone => outputZone;
        public bool IsActive => isActiveAndEnabled && outputZone != null;
        public int Available => outputPile != null ? outputPile.Count : 0;
        public StackItem Take(Carrier to) => outputPile.TakeLast();

        protected virtual void Awake()
        {
            if (hum != null)
            {
                hum.clip = Sfx.JuicerClip;
                hum.loop = true;
                hum.volume = 0f;
                hum.Play();
            }
        }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (outputPile != null) outputPile.layers = OutputLayers;

            if (!working && CanStart()) Begin();

            if (working)
            {
                timer += dt * Boosts.WorkMult;
                if (timer >= BatchTime)
                {
                    working = false;
                    var item = GameRefs.I.SpawnItem(OutputType, outPoint.position, outPoint.rotation);
                    OnBatchDone(item);
                }
            }
            AnimateWork(working, Progress, dt);
            if (hum != null)
            {
                // Deactivating the object stops the source (intro previews do that), so restart it when needed.
                if (!hum.isPlaying && hum.clip != null) hum.Play();
                hum.volume = Mathf.MoveTowards(hum.volume, working && NearPlayer() ? 0.08f : 0f, dt);
            }
        }

        /// <summary>Enough input waiting and room for the result.</summary>
        protected virtual bool CanStart() =>
            outputPile != null && outputPile.HasSpace && inputPile != null && inputPile.CountReady(InputType) >= InputPerBatch;

        /// <summary>Consume the batch (fly the inputs into the machine) and start the timer.</summary>
        protected virtual void Begin()
        {
            for (int i = 0; i < InputPerBatch; i++)
            {
                var s = inputPile.TakeLast(InputType);
                if (s == null) break;
                s.inTransit = true;
                var target = intakePoint != null ? intakePoint : transform;
                Tweener.Arc(s.transform, () => target.position, 0.8f, 0.28f, () =>
                {
                    s.Despawn();
                    OnIntake(target.position);
                }, null, Vector3.one * 0.4f, i * 0.07f);
            }
            working = true;
            timer = 0f;
            OnBatchStart();
        }

        /// <summary>Put a finished item on the output pile (subclasses call this from <see cref="OnBatchDone"/>).</summary>
        protected void Deliver(StackItem item, float arc = 0.9f, float time = 0.35f, System.Action landed = null)
        {
            outputPile.Add(item, arc, time, () =>
            {
                Fx.Glint(item.transform.position + Vector3.up * 0.3f, Color.white, 2);
                if (NearPlayer()) Sfx.Play(SfxId.Tink, 0.18f, Random.Range(0.95f, 1.1f));
                landed?.Invoke();
            });
        }

        protected virtual void OnFed() { }
        protected virtual void OnIntake(Vector3 at) { }
        protected virtual void OnBatchStart() { }
        protected virtual void AnimateWork(bool isWorking, float progress, float dt) { }
        protected abstract void OnBatchDone(StackItem item);

        protected bool NearPlayer()
        {
            var p = GameRefs.I != null ? GameRefs.I.player : null;
            return p != null && (p.transform.position - transform.position).sqrMagnitude < 144f;
        }
    }
}
