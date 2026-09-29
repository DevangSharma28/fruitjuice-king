using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Moves a prop along a waypoint path (boats, background traffic). Loops by jumping back to the start, or ping-pongs.
    /// </summary>
    public class PathMover : MonoBehaviour
    {
        public Vector3[] points;
        public float speed = 3f;
        public bool pingPong;
        [Tooltip("Pause at each end (seconds).")]
        public float endPause = 1f;
        public float startOffset;
        [Tooltip("Optional child bobbed up and down (boats).")]
        public Transform bob;
        public float bobAmount;
        public Transform[] wheels;
        public float wheelRadius = 0.3f;

        int _i = 1;
        int _dir = 1;
        float _wait;
        Vector3 _bobBase;

        void Start()
        {
            if (points == null || points.Length < 2)
            {
                enabled = false;
                return;
            }
            if (bob != null) _bobBase = bob.localPosition;
            transform.position = points[0];
            // Spread several movers along the same road.
            float skip = startOffset;
            while (skip > 0f && enabled)
            {
                float seg = Vector3.Distance(transform.position, points[_i]);
                if (seg > skip)
                {
                    transform.position = Vector3.MoveTowards(transform.position, points[_i], skip);
                    break;
                }
                skip -= seg;
                transform.position = points[_i];
                Advance();
            }
            Face(points[_i] - transform.position, 1f);
        }

        void Advance()
        {
            _i += _dir;
            if (_i >= 0 && _i < points.Length) return;
            if (pingPong)
            {
                _dir = -_dir;
                _i += _dir * 2;
                _wait = endPause;
            }
            else
            {
                transform.position = points[0];
                _i = 1;
                _wait = endPause;
            }
        }

        void Face(Vector3 d, float k)
        {
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), k);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (bob != null) bob.localPosition = _bobBase + Vector3.up * (Mathf.Sin(Time.time * 1.3f + startOffset) * bobAmount);
            if (_wait > 0f)
            {
                _wait -= dt;
                return;
            }
            Vector3 target = points[_i];
            Vector3 pos = transform.position;
            float step = speed * dt;
            Vector3 d = target - pos;
            if (d.magnitude <= step)
            {
                transform.position = target;
                Advance();
            }
            else transform.position = pos + d.normalized * step;
            Face(d, 1f - Mathf.Exp(-4f * dt));
            if (wheels != null)
                foreach (var w in wheels)
                    if (w != null) w.Rotate(Vector3.right, step / wheelRadius * Mathf.Rad2Deg, Space.Self);
        }
    }
}
