using UnityEngine;

namespace JuiceKing
{
    /// <summary>Feeds movement speed and carry pose into the shared character Animator, plus a few procedural extras.</summary>
    public class CharacterAnim : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int HoldId = Animator.StringToHash("Hold");
        static readonly int CheerId = Animator.StringToHash("Cheer");

        public Animator animator;
        public Carrier hands;
        public bool alwaysHoldRight;
        [Tooltip("Scales animation playback to match the actual movement speed.")]
        public float animSpeedRef = 4.5f;
        [Tooltip("Lean into the direction of travel (degrees at full speed).")]
        public float lean = 7f;
        [Tooltip("Kick up dust puffs and footstep taps while running.")]
        public bool footsteps;

        Vector3 _last;
        float _speed;
        float _armWeight;
        float _stepT;
        float _leanCur;
        bool _hasCheer;
        Transform _model;
        Vector3 _modelBase;
        bool _hopping;
        float _hopY;

        public float Speed => _speed;

        void Awake()
        {
            if (animator == null) return;
            _model = animator.transform;
            _modelBase = _model.localPosition;
            foreach (var p in animator.parameters)
                if (p.nameHash == CheerId) _hasCheer = true;
        }

        void OnEnable() => _last = transform.position;

        void Update()
        {
            if (animator == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 d = transform.position - _last;
            d.y = 0f;
            _last = transform.position;
            _speed = Mathf.Lerp(_speed, d.magnitude / dt, 1f - Mathf.Exp(-14f * dt));
            animator.SetFloat(SpeedId, _speed);

            int hold = alwaysHoldRight ? 1 : (hands != null && hands.Count > 0 ? 2 : 0);
            animator.SetInteger(HoldId, hold);
            _armWeight = Mathf.MoveTowards(_armWeight, hold != 0 ? 1f : 0f, dt * 6f);
            if (animator.layerCount > 1) animator.SetLayerWeight(1, _armWeight);
            animator.speed = _speed > 0.3f ? Mathf.Clamp(_speed / animSpeedRef * 1.1f, 0.8f, 1.6f) : 1f;

            // Forward lean proportional to speed.
            float targetLean = Mathf.Clamp01(_speed / Mathf.Max(0.1f, animSpeedRef)) * lean;
            _leanCur = Mathf.Lerp(_leanCur, targetLean, 1f - Mathf.Exp(-10f * dt));

            if (footsteps && _speed > 1.5f)
            {
                _stepT -= dt * Mathf.Clamp(_speed / 3f, 0.8f, 2f);
                if (_stepT <= 0f)
                {
                    _stepT = 0.28f;
                    Fx.Dust(transform.position + Vector3.up * 0.05f - transform.forward * 0.25f, 1);
                    Sfx.Play(SfxId.Step, 0.08f, Random.Range(0.85f, 1.15f));
                }
            }
        }

        // Applied after the Animator so its root curves (if any) cannot overwrite the lean / hop.
        void LateUpdate()
        {
            if (_model == null) return;
            _model.localRotation = Quaternion.Euler(_leanCur, 0f, 0f);
            _model.localPosition = _modelBase + Vector3.up * _hopY;
        }

        /// <summary>Happy one-shot: the "yes" emote (if the controller has it) plus a little hop.</summary>
        public void Cheer(bool hop = true)
        {
            if (_hasCheer) animator.SetTrigger(CheerId);
            if (!hop || _model == null || _hopping) return;
            _hopping = true;
            Tweener.Value(_model, 0.42f, t => _hopY = Mathf.Sin(t * Mathf.PI) * 0.45f, () =>
            {
                _hopY = 0f;
                _hopping = false;
            });
        }
    }
}
