using System;
using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// The shipping box beside a parked truck (Berry Blast desks). It pops up open when the truck stops; goods fly in and
    /// pile up inside while a ring fills; when the order is complete the flaps fold shut, tape seals it, and it is lifted
    /// into the truck's cargo door.
    /// </summary>
    public class DeliveryBox : MonoBehaviour
    {
        public Transform visual;
        [Tooltip("Four flaps hinged on the rim (rotated outwards when open).")]
        public Transform[] flaps;
        public Vector3[] flapOpenEuler;
        [Tooltip("Stuff inside, scaled up from the bottom as the order fills.")]
        public Transform contents;
        public Renderer contentsRenderer;
        public GameObject tape;
        public Transform intake;
        public TextMeshPro countText;
        public SpriteRenderer icon;
        [Tooltip("World-space progress ring (Image, radial fill).")]
        public UnityEngine.UI.Image ring;
        public Transform label;

        public bool Open { get; private set; }

        Quaternion[] _flapClosed;
        Vector3 _home, _visualScale, _contentsScale;
        float _fill, _fillGoal;
        MaterialPropertyBlock _mpb;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        void Awake() => Init();

        bool _init;

        void Init()
        {
            if (_init) return;
            _init = true;
            _home = transform.position;
            if (visual != null) _visualScale = visual.localScale;
            if (contents != null) _contentsScale = contents.localScale;
            if (flaps != null)
            {
                _flapClosed = new Quaternion[flaps.Length];
                for (int i = 0; i < flaps.Length; i++) _flapClosed[i] = flaps[i] != null ? flaps[i].localRotation : Quaternion.identity;
            }
        }

        public void HideNow()
        {
            Init();
            Open = false;
            gameObject.SetActive(false);
        }

        void SetFlaps(float open)
        {
            if (flaps == null) return;
            for (int i = 0; i < flaps.Length; i++)
            {
                if (flaps[i] == null) continue;
                var e = flapOpenEuler != null && i < flapOpenEuler.Length ? flapOpenEuler[i] : Vector3.zero;
                flaps[i].localRotation = _flapClosed[i] * Quaternion.Euler(e * open);
            }
        }

        void Prepare(DeliveryOrder o)
        {
            Init();
            transform.position = _home;
            gameObject.SetActive(true);
            if (tape != null) tape.SetActive(false);
            if (visual != null) visual.localScale = _visualScale;
            SetFlaps(1f);
            if (icon != null && GameRefs.I != null) icon.sprite = GameRefs.I.ProductIcon(o.line, o.kind);
            if (contentsRenderer != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                contentsRenderer.GetPropertyBlock(_mpb);
                var c = o.line == ProductLine.Cake ? Color.Lerp(Balance.JuiceColors[(int)o.kind], Color.white, 0.55f) : Balance.JuiceColors[(int)o.kind];
                _mpb.SetColor(ColorId, c);
                contentsRenderer.SetPropertyBlock(_mpb);
            }
            _fill = _fillGoal = o.Progress;
            ApplyFill();
            UpdateCount(o);
            Open = true;
        }

        /// <summary>The truck has stopped: the box pops up beside it, flaps open.</summary>
        public void Appear(DeliveryOrder o)
        {
            Prepare(o);
            if (visual != null) Tweener.Scale(visual, Vector3.zero, _visualScale, 0.5f, Ease.OutBack);
            Fx.Poof(transform.position + Vector3.up * 0.2f, 8);
            Fx.Ring(transform.position, new Color(1f, 0.9f, 0.5f, 0.8f), 3.5f);
            Sfx.Play(SfxId.Thud, 0.4f, 1.1f);
        }

        /// <summary>Loading a save mid-order: the box is already there with what was delivered.</summary>
        public void ShowOpen(DeliveryOrder o) => Prepare(o);

        public void Receive(StackItem item, Action onLanded)
        {
            item.inTransit = true;
            item.onGround = false;
            item.transform.SetParent(null, true);
            var target = intake != null ? intake : transform;
            Tweener.Arc(item.transform, () => target.position, 1.1f, 0.32f, () =>
            {
                item.Despawn();
                if (visual != null) Tweener.Punch(visual, 0.05f, 0.14f, _visualScale);
                Fx.Glint(target.position + Vector3.up * 0.2f, new Color(1f, 0.95f, 0.6f), 1);
                Sfx.Play(SfxId.Load, 0.28f, UnityEngine.Random.Range(0.95f, 1.15f));
                onLanded?.Invoke();
            }, null, Vector3.one * 0.55f);
        }

        public void SetFill(DeliveryOrder o)
        {
            _fillGoal = o.Progress;
            UpdateCount(o);
            if (label != null) Tweener.Punch(label, 0.12f, 0.15f, Vector3.one);
        }

        void UpdateCount(DeliveryOrder o)
        {
            if (countText != null) countText.text = o.Done ? "FULL!" : o.delivered + "/" + o.qty;
        }

        void ApplyFill()
        {
            if (ring != null) ring.fillAmount = _fill;
            if (contents != null)
            {
                contents.gameObject.SetActive(_fill > 0.01f);
                contents.localScale = new Vector3(_contentsScale.x, _contentsScale.y * Mathf.Max(0.02f, _fill), _contentsScale.z);
            }
        }

        void Update()
        {
            if (Mathf.Abs(_fill - _fillGoal) > 0.0005f)
            {
                _fill = Mathf.MoveTowards(_fill, _fillGoal, Time.deltaTime * 1.4f);
                ApplyFill();
            }
        }

        /// <summary>Order complete: fold the flaps, tape it shut, lift it into the truck.</summary>
        public void CloseAndLoad(DeliveryTruck truck, Action onLoaded)
        {
            Open = false;
            _fill = _fillGoal = 1f;
            ApplyFill();
            if (countText != null) countText.text = "SEALED!";
            // Flaps fold one after another.
            Tweener.Value(transform, 0.55f, t =>
            {
                if (flaps == null) return;
                for (int i = 0; i < flaps.Length; i++)
                {
                    if (flaps[i] == null) continue;
                    float k = Mathf.Clamp01(t * 1.6f - i * 0.2f);
                    var e = flapOpenEuler != null && i < flapOpenEuler.Length ? flapOpenEuler[i] : Vector3.zero;
                    flaps[i].localRotation = _flapClosed[i] * Quaternion.Euler(e * (1f - Ease.InOutQuad(k)));
                }
            }, () =>
            {
                if (tape != null)
                {
                    tape.SetActive(true);
                    Tweener.Scale(tape.transform, new Vector3(0f, 1f, 1f), Vector3.one, 0.25f, Ease.OutQuad);
                }
                Sfx.Play(SfxId.Tape, 0.45f);
                Fx.Sparkle(transform.position + Vector3.up * 0.9f, new Color(1f, 0.95f, 0.7f), 8);
                if (visual != null) Tweener.Punch(visual, 0.15f, 0.25f, _visualScale);
            });

            // Up it goes, into the cargo door; the truck dips as it takes the weight.
            Tweener.Delay(0.95f, () =>
            {
                if (this == null) return;
                if (label != null) label.gameObject.SetActive(false);
                Vector3 door = truck.dropPoint != null ? truck.dropPoint.position : truck.transform.position + Vector3.up;
                Sfx.Play(SfxId.Whoosh, 0.35f, 0.9f);
                Tweener.Arc(transform, () => truck.dropPoint != null ? truck.dropPoint.position : door, 1.4f, 0.7f, () =>
                {
                    truck.TakeCargo();
                    Fx.Poof(door, 6);
                    Sfx.Play(SfxId.Thud, 0.55f, 0.85f);
                    if (label != null) label.gameObject.SetActive(true);
                    gameObject.SetActive(false);
                    transform.position = _home;
                    transform.localScale = Vector3.one;
                    if (visual != null) visual.localScale = _visualScale;
                    onLoaded?.Invoke();
                }, null, Vector3.one * 0.6f);
            });
        }

        /// <summary>Timed out with nothing inside: the box just folds away.</summary>
        public void Vanish()
        {
            Open = false;
            if (visual != null)
                Tweener.Scale(visual, visual.localScale, Vector3.zero, 0.3f, Ease.InQuad, () =>
                {
                    gameObject.SetActive(false);
                    visual.localScale = _visualScale;
                });
            else gameObject.SetActive(false);
        }
    }
}
