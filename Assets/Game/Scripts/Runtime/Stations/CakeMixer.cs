using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Berry Cake Shop, stage 1: a stand mixer whips berries, flour and cream into batter. Each batch is poured into a
    /// baking tin that rides the conveyor to the oven (the oven pulls tins off <see cref="Processor.outputPile"/>).
    /// </summary>
    public class CakeMixer : Processor
    {
        [Header("Show")]
        public Transform bowl;
        public Transform whisk;
        public Transform head;
        public Transform batter;
        public Transform flourSack;
        [Tooltip("Conveyor rollers (spin while a tin moves).")]
        public Transform[] rollers;
        public Renderer belt;
        public Renderer statusLight;
        public Material ledOn, ledOff;

        public override ItemType InputType => ItemTypes.Slice(kind);
        public override ItemType OutputType => ItemTypes.Batter(kind);
        protected override int InputPerBatch => Balance.BerriesPerBatter;
        protected override float BatchTime => Economy.BatterTime(kind);

        float _spin, _belt, _beltGoal;
        Vector3 _batterScale, _headPos, _bowlScale, _sackScale;
        Quaternion _bowlRot;
        bool _led;
        MaterialPropertyBlock _mpb;
        static readonly int BaseST = Shader.PropertyToID("_BaseMap_ST");

        protected override void Awake()
        {
            base.Awake();
            if (batter != null)
            {
                _batterScale = batter.localScale;
                SetBatter(0f);
            }
            if (head != null) _headPos = head.localPosition;
            if (bowl != null)
            {
                _bowlScale = bowl.localScale;
                _bowlRot = bowl.localRotation;
            }
            if (flourSack != null) _sackScale = flourSack.localScale;
        }

        void OnEnable()
        {
            if (GameManager.I != null) GameManager.I.RegisterCakeMixer(kind, true);
        }

        void OnDisable()
        {
            if (GameManager.I != null) GameManager.I.RegisterCakeMixer(kind, false);
        }

        void SetBatter(float f)
        {
            if (batter == null) return;
            batter.localScale = new Vector3(_batterScale.x * Mathf.Lerp(0.6f, 1f, f), _batterScale.y * Mathf.Max(0.02f, f), _batterScale.z * Mathf.Lerp(0.6f, 1f, f));
        }

        protected override void OnFed()
        {
            if (bowl != null) Tweener.Punch(bowl, 0.06f, 0.18f, _bowlScale);
        }

        protected override void OnIntake(Vector3 at)
        {
            Fx.Drops(at, Balance.JuiceColors[(int)kind], 3);
            if (NearPlayer()) Sfx.Play(SfxId.Squish, 0.15f, Random.Range(1.1f, 1.3f));
        }

        protected override void OnBatchStart()
        {
            // A puff of flour from the sack as it tips into the bowl.
            if (flourSack != null)
            {
                Tweener.Punch(flourSack, 0.12f, 0.3f, _sackScale);
                Fx.Poof(flourSack.position + Vector3.up * 0.35f, 5);
            }
            if (head != null) Tweener.MoveLocal(head, _headPos + Vector3.down * 0.12f, 0.25f, Ease.OutBack);
        }

        protected override void AnimateWork(bool isWorking, float progress, float dt)
        {
            float goal = isWorking ? 1800f * Boosts.WorkMult : 0f;
            _spin = Mathf.MoveTowards(_spin, goal, dt * 3600f);
            if (whisk != null) whisk.Rotate(0f, _spin * dt, 0f, Space.Self);
            if (bowl != null && isWorking) bowl.localRotation = _bowlRot * Quaternion.Euler(0f, Time.time * 90f, 0f);
            if (isWorking)
            {
                SetBatter(progress);
                if (head != null) head.localPosition = _headPos + Vector3.down * 0.12f + Vector3.up * Mathf.Sin(Time.time * 26f) * 0.012f;
                if (Random.value < dt * 3f && batter != null)
                    Fx.Bubbles(batter.position + Vector3.up * 0.05f, Color.Lerp(Balance.JuiceColors[(int)kind], Color.white, 0.55f), 1);
            }

            // Conveyor belt scrolls while a tin travels.
            _belt = Mathf.MoveTowards(_belt, _beltGoal, dt * 3f);
            _beltGoal = Mathf.MoveTowards(_beltGoal, 0f, dt * 0.8f);
            if (_belt > 0.001f)
            {
                _scroll += dt * _belt * 0.8f;
                if (belt != null)
                {
                    _mpb ??= new MaterialPropertyBlock();
                    belt.GetPropertyBlock(_mpb);
                    _mpb.SetVector(BaseST, new Vector4(1f, 4f, 0f, -_scroll));
                    belt.SetPropertyBlock(_mpb);
                }
                if (rollers != null)
                    foreach (var r in rollers)
                        if (r != null) r.Rotate(_belt * 360f * dt, 0f, 0f, Space.Self);
            }

            if (statusLight != null && _led != isWorking)
            {
                _led = isWorking;
                statusLight.sharedMaterial = isWorking ? ledOn : ledOff;
            }
        }

        float _scroll;

        /// <summary>The conveyor runs while a tin is carried (called by the oven too).</summary>
        public void RunBelt(float seconds) => _beltGoal = Mathf.Max(_beltGoal, Mathf.Clamp01(seconds));

        protected override void OnBatchDone(StackItem item)
        {
            if (head != null) Tweener.MoveLocal(head, _headPos, 0.3f, Ease.OutBack);
            SetBatter(0.05f);
            if (bowl != null)
            {
                bowl.localRotation = _bowlRot;
                Tweener.Punch(bowl, 0.12f, 0.3f, _bowlScale);
            }
            var col = Balance.JuiceColors[(int)kind];
            Fx.Sparkle(outPoint.position + Vector3.up * 0.2f, Color.Lerp(col, Color.white, 0.4f), 6);
            Fx.Poof(outPoint.position, 3);
            item.transform.localScale = Vector3.one * 0.3f;
            Tweener.Scale(item.transform, Vector3.one * 0.3f, Vector3.one, 0.35f, Ease.OutBack);
            RunBelt(1f);
            Deliver(item, 0.5f, 0.45f);
            if (NearPlayer()) Sfx.Play(SfxId.Pour, 0.3f, 1.15f);
        }
    }
}
