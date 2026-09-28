using UnityEngine;

namespace JuiceKing
{
    /// <summary>Turns fruit slices into juice cups.</summary>
    public class Juicer : MonoBehaviour, IItemReceiver, IItemSource
    {
        public FruitKind kind;
        public ItemPile inputPile;
        public ItemPile outputPile;
        public Transform intakePoint;
        public Transform spoutPoint;
        public Transform body;
        public Transform blades;
        public Transform liquid;
        public AudioSource hum;
        public DropZone inputZone;
        public PickupZone outputZone;

        bool _working;
        float _t;
        float _spin;
        Vector3 _bodyScale;
        Vector3 _liquidScale;

        void Awake()
        {
            if (body != null) _bodyScale = body.localScale;
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
            inputPile.Add(item, 1.3f, 0.3f);
            if (from != null && from.isPlayer) Sfx.Play(SfxId.Drop, 0.4f, Random.Range(0.95f, 1.1f));
        }

        // ---------- IItemSource (output tray) ----------
        public int Available => outputPile.Count;
        public StackItem Take(Carrier to) => outputPile.TakeLast();

        void Update()
        {
            float dt = Time.deltaTime;
            int need = Balance.SlicesPerJuice[(int)kind];

            if (!_working && outputPile.HasSpace && inputPile.CountOf(Accepts) >= need)
            {
                for (int i = 0; i < need; i++)
                {
                    var s = inputPile.TakeLast(Accepts);
                    if (s == null) break;
                    s.inTransit = true;
                    Tweener.Arc(s.transform, () => intakePoint.position, 0.8f, 0.25f, s.Despawn, null, Vector3.one * 0.4f, i * 0.06f);
                }
                _working = true;
                _t = 0f;
            }

            float spinTarget = _working ? 1440f : 0f;
            _spin = Mathf.MoveTowards(_spin, spinTarget, dt * 3000f);
            if (blades != null) blades.Rotate(0f, _spin * dt, 0f, Space.Self);

            if (_working)
            {
                _t += dt;
                float dur = Balance.JuiceTime[(int)kind];
                float k = Mathf.Clamp01(_t / dur);
                SetFill(0.15f + 0.85f * k);
                if (body != null)
                {
                    float w = 1f + Mathf.Sin(Time.time * 40f) * 0.015f;
                    body.localScale = new Vector3(_bodyScale.x * (2f - w), _bodyScale.y * w, _bodyScale.z * (2f - w));
                }

                if (_t >= dur)
                {
                    _working = false;
                    if (body != null)
                    {
                        body.localScale = _bodyScale;
                        Tweener.Punch(body, 0.08f, 0.25f, _bodyScale);
                    }
                    SetFill(0.15f);
                    var juice = GameRefs.I.SpawnItem(ItemTypes.Juice(kind), spoutPoint.position, spoutPoint.rotation);
                    juice.transform.localScale = Vector3.one * 0.4f;
                    outputPile.Add(juice, 0.9f, 0.35f);
                    Tweener.Scale(juice.transform, Vector3.one * 0.4f, Vector3.one, 0.35f, Ease.OutBack);
                    Fx.Sparkle(spoutPoint.position, Balance.JuiceColors[(int)kind], 5);
                    if (NearPlayer()) Sfx.Play(SfxId.Pour, 0.35f);
                }
            }

            if (hum != null) hum.volume = Mathf.MoveTowards(hum.volume, _working && NearPlayer() ? 0.12f : 0f, dt);
        }

        bool NearPlayer()
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
