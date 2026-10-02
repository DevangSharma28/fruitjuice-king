using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Berry Cake Shop, stage 2: pulls a batter tin off its cake mixer's conveyor, bakes it behind a glowing door and
    /// slides the finished cake onto the cooling rack, where carriers pick it up.
    /// </summary>
    public class Oven : Processor
    {
        [Tooltip("The mixer whose conveyor feeds this oven.")]
        public CakeMixer feeder;
        [Header("Show")]
        public Transform body;
        [Tooltip("Hinged at its bottom edge: swings down to open.")]
        public Transform door;
        public Renderer glow;
        public Material glowOff, glowOn;
        public Transform chimney;
        public Transform dial;
        [Tooltip("Ring of lights round the door that fills as the cake bakes.")]
        public Transform timerFill;

        public override ItemType InputType => ItemTypes.Batter(kind);
        public override ItemType OutputType => ItemTypes.Cake(kind);
        protected override int InputPerBatch => 1;
        protected override float BatchTime => Economy.BakeTime(kind);
        protected override int OutputLayers => Economy.RackLayers;

        float _door, _doorGoal, _steamT, _heat;
        Quaternion _doorRot;
        Vector3 _bodyScale, _timerScale;
        bool _loading;
        bool _glowOn;

        protected override void Awake()
        {
            base.Awake();
            if (door != null) _doorRot = door.localRotation;
            if (body != null) _bodyScale = body.localScale;
            if (timerFill != null)
            {
                _timerScale = timerFill.localScale;
                timerFill.localScale = new Vector3(0.001f, _timerScale.y, _timerScale.z);
            }
        }

        void OnEnable()
        {
            if (GameManager.I != null) GameManager.I.RegisterOven(kind, true);
        }

        void OnDisable()
        {
            if (GameManager.I != null) GameManager.I.RegisterOven(kind, false);
        }

        protected override bool CanStart() =>
            !_loading && feeder != null && feeder.isActiveAndEnabled && outputPile.HasSpace && feeder.outputPile.CountReady(InputType) > 0;

        protected override void Begin()
        {
            // The first tin rides the rest of the conveyor, the door drops open and it slides in.
            var tin = feeder.outputPile.TakeLast(InputType);
            if (tin == null) return;
            _loading = true;
            tin.inTransit = true;
            feeder.RunBelt(1f);
            _doorGoal = 1f;
            var inside = intakePoint != null ? intakePoint : transform;
            Tweener.Arc(tin.transform, () => inside.position, 0.25f, 0.55f, () =>
            {
                tin.Despawn();
                _loading = false;
                _doorGoal = 0f;
                working = true;
                timer = 0f;
                OnBatchStart();
            }, () => inside.rotation, Vector3.one * 0.85f);
        }

        protected override void OnBatchStart()
        {
            if (NearPlayer()) Sfx.Play(SfxId.Lid, 0.3f, 0.8f);
            if (body != null) Tweener.Punch(body, 0.04f, 0.2f, _bodyScale);
        }

        protected override void AnimateWork(bool isWorking, float progress, float dt)
        {
            // Door: drops open for loading and unloading, eased with a little bounce.
            _door = Mathf.MoveTowards(_door, _doorGoal, dt * 3.2f);
            if (door != null) door.localRotation = _doorRot * Quaternion.Euler(Ease.OutBack(_door) * 80f, 0f, 0f);

            // Heat glow behind the window.
            _heat = Mathf.MoveTowards(_heat, isWorking ? 1f : 0f, dt * 2f);
            bool on = _heat > 0.3f;
            if (glow != null && on != _glowOn)
            {
                _glowOn = on;
                glow.sharedMaterial = on ? glowOn : glowOff;
            }
            if (timerFill != null)
                timerFill.localScale = new Vector3(_timerScale.x * Mathf.Max(0.001f, isWorking ? progress : 0f), _timerScale.y, _timerScale.z);
            if (dial != null && isWorking) dial.localRotation = Quaternion.Euler(0f, 0f, -progress * 300f);

            if (isWorking)
            {
                if (body != null)
                {
                    float w = 1f + Mathf.Sin(Time.time * 18f) * 0.006f;
                    body.localScale = new Vector3(_bodyScale.x * w, _bodyScale.y * (2f - w), _bodyScale.z * w);
                }
                // Steam from the chimney and shimmering heat over the top.
                _steamT -= dt;
                if (_steamT <= 0f)
                {
                    _steamT = 0.28f;
                    if (chimney != null) Fx.Smoke(chimney.position, 0.08f, 1);
                    if (Random.value < 0.5f) Fx.Bubbles(transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 2.1f, 0f), new Color(1f, 0.75f, 0.4f, 0.5f), 1);
                }
            }
            else if (body != null && body.localScale != _bodyScale) body.localScale = _bodyScale;
        }

        protected override void OnBatchDone(StackItem item)
        {
            // Ding! The door drops, the cake slides out onto the rack with a puff of steam.
            _doorGoal = 1f;
            Tweener.Delay(0.5f, () => { if (this != null && !_loading) _doorGoal = 0f; });
            item.transform.position = intakePoint != null ? intakePoint.position : outPoint.position;
            item.transform.localScale = Vector3.one * 0.85f;
            Tweener.Scale(item.transform, Vector3.one * 0.85f, Vector3.one, 0.4f, Ease.OutBack);
            Fx.Smoke(outPoint.position + Vector3.up * 0.3f, 0.05f, 3);
            Fx.Sparkle(outPoint.position + Vector3.up * 0.4f, new Color(1f, 0.9f, 0.6f), 8);
            Fx.Stars(outPoint.position + Vector3.up * 0.6f, 4, Balance.JuiceColors[(int)kind]);
            if (body != null) Tweener.Punch(body, 0.08f, 0.3f, _bodyScale);
            if (NearPlayer()) Sfx.Play(SfxId.Ding, 0.45f);
            Deliver(item, 0.4f, 0.4f);
            if (GameManager.I != null) GameManager.I.NotifyCakeBaked();
        }
    }
}
