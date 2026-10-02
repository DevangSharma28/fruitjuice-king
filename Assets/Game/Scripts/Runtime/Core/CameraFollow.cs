using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        // Zoom-aware distances: while a shot pulls the camera back, shadows and decor culling reach further so nothing
        // pops in or out as it glides (and the see-through dither fades out, it only makes sense around the player).
        readonly float[] _cullDistances = new float[32];
        float _cullZoom = -1f;
        UniversalRenderPipelineAsset _urp;
        float _baseShadowDistance;
        float _occluder = 1f;
        Vector3 _base;
        Vector3 _lastTarget;
        Vector3 _lead;
        float _shakeT, _shakeDur, _shakeAmp;
        float _punch;

        class Shot
        {
            public Vector3 point;
            public float dist, moveIn, hold, moveOut;
            public Action onArrive, onDone;
        }

        readonly Queue<Shot> _shots = new Queue<Shot>();
        Shot _shot;
        int _phase;
        float _pt;
        Vector3 _from, _goal, _render;

        /// <summary>True while a camera peek / cinematic shot is running (player input is blocked).</summary>
        public static bool Busy => _i != null && (_i._shot != null || _i._shots.Count > 0);

        void Awake()
        {
            _i = this;
            _cam = GetComponent<Camera>();
        }

        void Start()
        {
            if (_cam != null)
            {
                // (layerCullSpherical is built-in-renderer only: URP ignores it and logs a warning.)
                ApplyZoom(1f);
            }
            _urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (_urp == null) _urp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (_urp != null) _baseShadowDistance = _urp.shadowDistance;
            _base = target != null ? target.position + offset : transform.position;
            _render = _base;
            if (target != null) _lastTarget = target.position;
            transform.SetPositionAndRotation(_base, Quaternion.LookRotation(-offset.normalized, Vector3.up));
        }

        static readonly int OccluderId = Shader.PropertyToID("_JKOccluder");

        void OnDisable()
        {
            Shader.SetGlobalVector(OccluderId, Vector4.zero);
            // The pipeline asset is shared (and saved in the Editor): put its shadow distance back.
            if (_urp != null && _baseShadowDistance > 0f) _urp.shadowDistance = _baseShadowDistance;
        }

        /// <summary>Scales decor cull distances and the shadow distance with how far the camera is pulled back.</summary>
        void ApplyZoom(float zoom)
        {
            if (Mathf.Abs(zoom - _cullZoom) < 0.02f) return;
            _cullZoom = zoom;
            if (_cam != null)
            {
                _cullDistances[BigDecorLayer] = bigDecorCull * zoom;
                _cullDistances[SmallDecorLayer] = smallDecorCull * zoom;
                _cam.layerCullDistances = _cullDistances;
            }
            if (_urp != null && _baseShadowDistance > 0f) _urp.shadowDistance = _baseShadowDistance * zoom;
        }

        void LateUpdate()
        {
            if (target == null) return;
            // Tall decor between the camera and the player dithers out around them (JuiceKing/Stylized, _SEE_THROUGH).
            var tp = target.position;
            _occluder = Mathf.MoveTowards(_occluder, _shot != null ? 0f : 1f, Time.unscaledDeltaTime * 3f);
            Shader.SetGlobalVector(OccluderId, new Vector4(tp.x, tp.y + 1f, tp.z, _occluder));
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
                shake = UnityEngine.Random.insideUnitSphere * (_shakeAmp * Mathf.Clamp01(_shakeT / _shakeDur));
            }

            Vector3 pos = UpdateShot(_base);
            _render = pos;
            // 1 at the normal follow height, 2.3 at the intro's wide opening shot.
            float zoom = offset.y > 0.01f ? Mathf.Max(1f, (pos.y - tp.y) / offset.y) : 1f;
            ApplyZoom(zoom);
            transform.SetPositionAndRotation(pos + shake, Quaternion.LookRotation(-offset.normalized, Vector3.up));
        }

        Vector3 UpdateShot(Vector3 follow)
        {
            if (_shot == null)
            {
                if (_shots.Count == 0) return follow;
                StartShot(_shots.Dequeue(), _render == Vector3.zero ? follow : _render);
            }
            _pt += Time.unscaledDeltaTime;
            switch (_phase)
            {
                case 0:
                {
                    float t = _shot.moveIn <= 0f ? 1f : Mathf.Clamp01(_pt / _shot.moveIn);
                    if (t >= 1f)
                    {
                        _phase = 1;
                        _pt = 0f;
                        _shot.onArrive?.Invoke();
                    }
                    return Vector3.LerpUnclamped(_from, _goal, Ease.InOutQuad(t));
                }
                case 1:
                    if (_pt >= _shot.hold)
                    {
                        var done = _shot.onDone;
                        if (_shots.Count > 0) StartShot(_shots.Dequeue(), _goal);
                        else
                        {
                            _phase = 2;
                            _pt = 0f;
                        }
                        done?.Invoke();
                    }
                    return _goal;
                default:
                {
                    float t = _shot.moveOut <= 0f ? 1f : Mathf.Clamp01(_pt / _shot.moveOut);
                    if (t >= 1f)
                    {
                        _shot = null;
                        return follow;
                    }
                    return Vector3.LerpUnclamped(_goal, follow, Ease.InOutQuad(t));
                }
            }
        }

        void StartShot(Shot s, Vector3 from)
        {
            _shot = s;
            _phase = 0;
            _pt = 0f;
            _from = from;
            _goal = s.point + offset * s.dist;
        }

        /// <summary>
        /// Glide the camera to look at a world point, hold, then glide back to the player. Calls queue up and chain
        /// without returning to the player in between (used by the world intro tour and unlock reveals).
        /// </summary>
        public static void Peek(Vector3 point, float hold = 1.2f, float distScale = 1f, Action onArrive = null, Action onDone = null,
            float moveIn = 0.8f, float moveOut = 0.7f)
        {
            if (_i == null)
            {
                onArrive?.Invoke();
                onDone?.Invoke();
                return;
            }
            _i._shots.Enqueue(new Shot { point = point, dist = distScale, hold = hold, moveIn = moveIn, moveOut = moveOut, onArrive = onArrive, onDone = onDone });
        }

        /// <summary>Drops any queued shots and heads straight back to the player.</summary>
        public static void CancelPeeks()
        {
            if (_i == null) return;
            _i._shots.Clear();
            if (_i._shot != null && _i._phase < 2)
            {
                _i._goal = _i._render;
                _i._phase = 2;
                _i._pt = 0f;
            }
        }

        /// <summary>Snap the follow position to the player (after a scene load or teleport).</summary>
        public static void SnapToTarget()
        {
            if (_i == null || _i.target == null) return;
            _i._base = _i.target.position + _i.offset;
            _i._lastTarget = _i.target.position;
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
