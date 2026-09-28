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

        Camera _cam;
        Vector3 _base;
        float _shakeT, _shakeDur, _shakeAmp;

        void Awake()
        {
            _i = this;
            _cam = GetComponent<Camera>();
        }

        void Start()
        {
            _base = target != null ? target.position + offset : transform.position;
            transform.SetPositionAndRotation(_base, Quaternion.LookRotation(-offset.normalized, Vector3.up));
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            if (_cam != null)
            {
                float aspect = _cam.aspect;
                _cam.fieldOfView = aspect < 1f
                    ? Mathf.Lerp(portraitFov, landscapeFov, Mathf.InverseLerp(0.45f, 1f, aspect))
                    : landscapeFov;
            }

            _base = Vector3.Lerp(_base, target.position + offset, 1f - Mathf.Exp(-followSharpness * dt));

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
            _i._shakeAmp = amplitude;
            _i._shakeDur = Mathf.Max(0.01f, duration);
            _i._shakeT = duration;
        }
    }
}
