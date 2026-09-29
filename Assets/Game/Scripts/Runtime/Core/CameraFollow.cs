using UnityEngine;

namespace JuiceKing
{
    /// <summary>Angled top-down follow camera that adapts its FOV to portrait / landscape.</summary>
    public class CameraFollow : MonoBehaviour
    {
        static CameraFollow _i;

        public Transform target;
        public Vector3 offset = new Vector3(0f, 14f, -9.5f);
        public float followSharpness = 7f;
        public float portraitFov = 52f;
        public float landscapeFov = 38f;
        [Tooltip("How far ahead of the player (in the direction of travel) the camera leads.")]
        public float lookAhead = 1.2f;

        public const int BigDecorLayer = 8;
        public const int SmallDecorLayer = 9;
        [Tooltip("Per-layer cull distances (mobile draw-call budget): trees / props, and grass / flowers.")]
        public float bigDecorCull = 70f;
        public float smallDecorCull = 44f;

        Camera _cam;
        Vector3 _base;
        Vector3 _lastTarget;
        Vector3 _lead;
        float _shakeT, _shakeDur, _shakeAmp;
        float _punch;

        void Awake()
        {
            _i = this;
            _cam = GetComponent<Camera>();
        }

        void Start()
        {
            if (_cam != null)
            {
                var d = new float[32];
                d[BigDecorLayer] = bigDecorCull;
                d[SmallDecorLayer] = smallDecorCull;
                _cam.layerCullDistances = d;
                _cam.layerCullSpherical = true;
            }
            _base = target != null ? target.position + offset : transform.position;
            if (target != null) _lastTarget = target.position;
            transform.SetPositionAndRotation(_base, Quaternion.LookRotation(-offset.normalized, Vector3.up));
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _punch = Mathf.MoveTowards(_punch, 0f, dt * 0.5f);
            if (_cam != null)
            {
                float aspect = _cam.aspect;
                float fov = aspect < 1f
                    ? Mathf.Lerp(portraitFov, landscapeFov, Mathf.InverseLerp(0.45f, 1f, aspect))
                    : landscapeFov;
                _cam.fieldOfView = fov * (1f - _punch);
            }

            Vector3 vel = (target.position - _lastTarget) / dt;
            _lastTarget = target.position;
            vel.y = 0f;
            Vector3 leadGoal = Vector3.ClampMagnitude(vel * 0.2f, 1f) * lookAhead;
            _lead = Vector3.Lerp(_lead, leadGoal, 1f - Mathf.Exp(-2.5f * dt));

            _base = Vector3.Lerp(_base, target.position + _lead + offset, 1f - Mathf.Exp(-followSharpness * dt));

            Vector3 shake = Vector3.zero;
            if (_shakeT > 0f)
            {
                _shakeT -= dt;
                shake = Random.insideUnitSphere * (_shakeAmp * Mathf.Clamp01(_shakeT / _shakeDur));
            }

            transform.SetPositionAndRotation(_base + shake, Quaternion.LookRotation(-offset.normalized, Vector3.up));
        }

        public static void Shake(float amplitude, float duration)
        {
            if (_i == null) return;
            if (_i._shakeT > 0f && _i._shakeAmp > amplitude) return;
            _i._shakeAmp = amplitude;
            _i._shakeDur = Mathf.Max(0.01f, duration);
            _i._shakeT = duration;
        }

        /// <summary>Quick zoom-in bump (fraction of FOV) that eases back.</summary>
        public static void Punch(float amount)
        {
            if (_i == null) return;
            _i._punch = Mathf.Max(_i._punch, amount);
        }
    }
}
