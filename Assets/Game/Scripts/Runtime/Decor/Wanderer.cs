using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Idle critter: strolls to random points inside a rectangle, pauses, pecks / looks around.
    /// Feeds the ithappy animal controllers (Vert = moving 0..1, State = walk 0 / run 1).
    /// Optionally floats on water (ducks) instead of walking.
    /// </summary>
    public class Wanderer : MonoBehaviour
    {
        static readonly int VertId = Animator.StringToHash("Vert");
        static readonly int StateId = Animator.StringToHash("State");

        public Animator animator;
        public Vector3 areaCenter;
        public Vector2 areaSize = new Vector2(4f, 4f);
        public float speed = 1.2f;
        public Vector2 pause = new Vector2(1.5f, 4f);
        [Tooltip("Run away from the player when closer than this (0 = never).")]
        public float skittish = 2.2f;
        [Tooltip("Bob on water instead of walking animation.")]
        public bool floats;
        public Transform bobTarget;

        Vector3 _dest;
        float _wait;
        float _run;
        float _vert;
        Vector3 _bobBase;

        void Start()
        {
            _wait = Random.Range(0f, pause.y);
            _dest = transform.position;
            if (bobTarget != null) _bobBase = bobTarget.localPosition;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;

            // Scatter when the player barges through.
            var player = GameRefs.I != null ? GameRefs.I.player : null;
            if (skittish > 0f && player != null)
            {
                Vector3 away = pos - player.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < skittish * skittish && _run <= 0f)
                {
                    _run = 1.2f;
                    _dest = Clamp(pos + away.normalized * 3f + Random.insideUnitSphere * 0.8f);
                    _wait = 0f;
                }
            }
            if (_run > 0f) _run -= dt;

            Vector3 d = _dest - pos;
            d.y = 0f;
            float dist = d.magnitude;
            bool moving = dist > 0.08f && _wait <= 0f;
            if (moving)
            {
                float sp = speed * (_run > 0f ? 2.4f : 1f);
                Vector3 dir = d / dist;
                transform.position = pos + dir * Mathf.Min(dist, sp * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-8f * dt));
            }
            else if (_wait > 0f) _wait -= dt;
            else
            {
                _wait = Random.Range(pause.x, pause.y);
                _dest = Clamp(areaCenter + new Vector3(Random.Range(-0.5f, 0.5f) * areaSize.x, 0f, Random.Range(-0.5f, 0.5f) * areaSize.y));
            }

            _vert = Mathf.MoveTowards(_vert, moving ? 1f : 0f, dt * 5f);
            if (animator != null)
            {
                animator.SetFloat(VertId, _vert);
                animator.SetFloat(StateId, _run > 0f ? 1f : 0f);
            }

            if (floats && bobTarget != null)
                bobTarget.localPosition = _bobBase + Vector3.up * (Mathf.Sin(Time.time * 2.2f + areaCenter.x) * 0.03f);
        }

        Vector3 Clamp(Vector3 p)
        {
            float hx = areaSize.x * 0.5f, hz = areaSize.y * 0.5f;
            p.x = Mathf.Clamp(p.x, areaCenter.x - hx, areaCenter.x + hx);
            p.z = Mathf.Clamp(p.z, areaCenter.z - hz, areaCenter.z + hz);
            p.y = transform.position.y;
            return p;
        }
    }
}
