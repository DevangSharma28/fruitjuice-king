using UnityEngine;

namespace JuiceKing
{
    /// <summary>Turns fruit slices into juice cups.</summary>
    public class Juicer : MonoBehaviour, IItemReceiver, IProducer
    {
        public FruitKind kind;
        public ItemPile inputPile;
        public ItemPile outputPile;
        public Transform intakePoint;
        public Transform spoutPoint;
        public Transform body;
        public Transform blades;
        public Transform liquid;
        public Transform hopper;
        public AudioSource hum;
        public DropZone inputZone;
        public PickupZone outputZone;
        [Header("Dressing")]
        public Renderer statusLight;
        public Material ledOn, ledOff;
        public Transform sign;

        bool _working;
        float _t;
        float _spin;
        float _bubbleT;
        Vector3 _bodyScale;
        Vector3 _liquidScale;
        Vector3 _hopperScale;
        Vector3 _signPos;
        bool _ledState;

        protected virtual void Awake()
        {
            if (body != null) _bodyScale = body.localScale;
            if (hopper != null) _hopperScale = hopper.localScale;
            if (sign != null) _signPos = sign.localPosition;
            if (liquid != null)
            {
                _liquidScale = liquid.localScale;
                SetFill(0.15f);
            }
            if (hum != null)
            {
                hum.clip = Sfx.JuicerClip;
                hum.loop = true;
                hum.volume = 0f;
                hum.Play();
            }
        }

        void OnEnable()
        {
            if (GameManager.I != null) GameManager.I.RegisterJuicer(kind);
        }

        void OnDisable()
        {
            if (GameManager.I != null) GameManager.I.UnregisterJuicer(kind);
        }

        // ---------- IItemReceiver (hopper) ----------
        public bool Accepts(ItemType t) => t == ItemTypes.Slice(kind);
        public bool HasSpace => inputPile.HasSpace;

        public void Receive(StackItem item, Carrier from)
        {
            bool player = from != null && from.isPlayer;
            inputPile.Add(item, 1.3f, 0.3f, () =>
            {
                if (hopper != null) Tweener.Punch(hopper, 0.1f, 0.18f, _hopperScale);
                if (player) Sfx.Play(SfxId.Drop, 0.4f, Random.Range(0.95f, 1.12f));
            });
        }

        // ---------- IProducer (output tray) ----------
        public FruitKind Kind => kind;
        public PickupZone OutputZone => outputZone;
        public bool IsActive => isActiveAndEnabled;
        public int Available => outputPile.Count;
        public ItemType OutputType => ItemTypes.Juice(kind);
        public StackItem Take(Carrier to) => outputPile.TakeLast();
        /// <summary>A batch is blending right now.</summary>
        public bool Working => _working;

        void Update()
        {
            float dt = Time.deltaTime;
            outputPile.layers = Economy.TrayLayers;
            int need = Balance.SlicesPerJuice[(int)kind];
            var col = Balance.JuiceColors[(int)kind];

            var slice = ItemTypes.Slice(kind);
            if (!_working && outputPile.HasSpace && inputPile.CountReady(slice) >= need)
            {
                for (int i = 0; i < need; i++)
                {
                    var s = inputPile.TakeLast(slice);
                    if (s == null) break;
                    s.inTransit = true;
                    Tweener.Arc(s.transform, () => intakePoint.position, 0.8f, 0.25f, () =>
                    {
                        s.Despawn();
                        Fx.Drops(intakePoint.position, col, 4);
                    }, null, Vector3.one * 0.4f, i * 0.06f);
                }
                _working = true;
                _t = 0f;
                OnBatchStart();
            }

            float spinTarget = _working ? 1440f * Boosts.WorkMult : 0f;
            _spin = Mathf.MoveTowards(_spin, spinTarget, dt * 3000f);
            if (blades != null) blades.Rotate(0f, _spin * dt, 0f, Space.Self);

            if (_working)
            {
                _t += dt * Boosts.WorkMult;
                float dur = Economy.JuiceTime(kind);
                float k = Mathf.Clamp01(_t / dur);
                SetFill(0.15f + 0.85f * k);
                if (body != null)
                {
                    float w = 1f + Mathf.Sin(Time.time * 40f) * 0.015f;
                    body.localScale = new Vector3(_bodyScale.x * (2f - w), _bodyScale.y * w, _bodyScale.z * (2f - w));
                }

                _bubbleT -= dt;
                if (_bubbleT <= 0f && liquid != null)
                {
                    _bubbleT = 0.12f;
                    Vector3 top = liquid.position + Vector3.up * liquid.lossyScale.y;
                    Fx.Bubbles(top, Color.Lerp(col, Color.white, 0.5f), 1);
                    if (NearPlayer() && Random.value < 0.35f) Sfx.Play(SfxId.Bubble, 0.12f, Random.Range(0.8f, 1.4f));
                }

                if (_t >= dur)
                {
                    _working = false;
                    if (body != null)
                    {
                        body.localScale = _bodyScale;
                        Tweener.Punch(body, 0.1f, 0.28f, _bodyScale);
                    }
                    SetFill(0.15f);
                    SpawnCup(col);
                    // Bonus Cup upgrade: sometimes the blend fills two cups.
                    if (Economy.BonusCupChance > 0f && Random.value < Economy.BonusCupChance && outputPile.HasSpace)
                    {
                        SpawnCup(col);
                        FloatingText.Show("x2", spoutPoint.position + Vector3.up * 0.8f, new Color(1f, 0.9f, 0.3f), 0.8f, 0.8f, 0.7f);
                    }
                    Fx.Sparkle(spoutPoint.position, col, 5);
                    Fx.Drops(spoutPoint.position, col, 5);
                    if (NearPlayer()) Sfx.Play(SfxId.Pour, 0.35f);
                    OnCupMade();
                }
            }


            AnimateWork(_working, _working ? Mathf.Clamp01(_t / Economy.JuiceTime(kind)) : 0f, dt);
            if (hum != null)
            {
                // Deactivating the object stops the source (intro previews do that), so restart it when needed.
                if (!hum.isPlaying && hum.clip != null) hum.Play();
                hum.volume = Mathf.MoveTowards(hum.volume, _working && NearPlayer() ? 0.1f : 0f, dt);
            }

            // Status light: steady green while blending, off when idle.
            if (statusLight != null && _ledState != _working)
            {
                _ledState = _working;
                statusLight.sharedMaterial = _working ? ledOn : ledOff;
            }
            if (sign != null)
                sign.localPosition = _signPos + Vector3.up * (Mathf.Sin(Time.time * (_working ? 9f : 2f)) * (_working ? 0.035f : 0.02f));
        }

        void SpawnCup(Color col)
        {
            var juice = GameRefs.I.SpawnItem(ItemTypes.Juice(kind), spoutPoint.position, spoutPoint.rotation);
            juice.transform.localScale = Vector3.one * 0.4f;
            outputPile.Add(juice, 0.9f, 0.35f, () =>
            {
                Fx.Glint(juice.transform.position + Vector3.up * 0.35f, Color.white, 2);
                if (NearPlayer()) Sfx.Play(SfxId.Tink, 0.18f, Random.Range(0.95f, 1.1f));
            });
            Tweener.Scale(juice.transform, Vector3.one * 0.4f, Vector3.one, 0.35f, Ease.OutBack);
            if (GameManager.I != null) GameManager.I.NotifyJuiceMade();
        }

        /// <summary>A batch of slices went in (advanced machines start their show here).</summary>
        protected virtual void OnBatchStart() { }

        /// <summary>Every frame: <paramref name="progress"/> 0..1 through the current batch.</summary>
        protected virtual void AnimateWork(bool working, float progress, float dt) { }

        /// <summary>A cup just came out of the spout.</summary>
        protected virtual void OnCupMade() { }

        protected bool NearPlayer()
        {
            var p = GameRefs.I != null ? GameRefs.I.player : null;
            return p != null && (p.transform.position - transform.position).sqrMagnitude < 144f;
        }

        void SetFill(float f)
        {
            if (liquid == null) return;
            liquid.localScale = new Vector3(_liquidScale.x, _liquidScale.y * f, _liquidScale.z);
        }
    }
}
