using UnityEngine;

namespace JuiceKing
{
    /// <summary>Feeds movement speed and carry pose into the shared character Animator.</summary>
    public class CharacterAnim : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int HoldId = Animator.StringToHash("Hold");

        public Animator animator;
        public Carrier hands;
        public bool alwaysHoldRight;
        [Tooltip("Scales animation playback to match the actual movement speed.")]
        public float animSpeedRef = 4.5f;

        Vector3 _last;
        float _speed;
        float _armWeight;

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
        }
    }
}
