using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Drives all the small ambient motions in one Update: wind sway on plants, spinning parts,
    /// drifting cloud shadows and scrolling water. Cheaper than one MonoBehaviour per prop.
    /// </summary>
    public class Ambient : MonoBehaviour
    {
        [System.Serializable]
        public struct Swayer
        {
            public Transform t;
            public float amount;
            public float speed;
            public float phase;
            [System.NonSerialized] public Quaternion baseRot;
        }

        [System.Serializable]
        public struct Spinner
        {
            public Transform t;
            public Vector3 axis;
            public float speed;
        }

        public List<Swayer> swayers = new List<Swayer>();
        public List<Spinner> spinners = new List<Spinner>();

        [Header("Cloud shadows")]
        public Transform[] clouds;
        public Vector3 windDir = new Vector3(1f, 0f, 0.35f);
        public float cloudSpeed = 1.1f;
        public Vector2 cloudAreaX = new Vector2(-40f, 40f);

        [Header("Water")]
        public Renderer[] water;
        public Vector2 waterScroll = new Vector2(0.03f, 0.02f);

        readonly List<Material> _waterMats = new List<Material>();

        void Awake()
        {
            for (int i = 0; i < swayers.Count; i++)
            {
                var s = swayers[i];
                if (s.t == null) continue;
                s.baseRot = s.t.localRotation;
                swayers[i] = s;
            }
            if (water != null)
                foreach (var r in water)
                    if (r != null) _waterMats.Add(r.material);
        }

        void Update()
        {
            float time = Time.time;
            float dt = Time.deltaTime;
            // A slow gust envelope shared by everything so the wind feels coherent.
            float gust = 0.7f + 0.3f * Mathf.Sin(time * 0.35f) * Mathf.Sin(time * 0.21f + 1f);

            for (int i = 0; i < swayers.Count; i++)
            {
                var s = swayers[i];
                if (s.t == null) continue;
                float a = Mathf.Sin(time * s.speed + s.phase) * s.amount * gust;
                float b = Mathf.Sin(time * s.speed * 0.73f + s.phase * 1.7f) * s.amount * 0.5f * gust;
                s.t.localRotation = s.baseRot * Quaternion.Euler(a, 0f, b);
            }

            for (int i = 0; i < spinners.Count; i++)
            {
                var s = spinners[i];
                if (s.t != null) s.t.Rotate(s.axis, s.speed * dt, Space.Self);
            }

            if (clouds != null)
            {
                Vector3 wd = windDir.normalized;
                Vector3 step = wd * (cloudSpeed * dt);
                // Wrap back along the wind direction so clouds stay on their track.
                Vector3 wrap = wd * ((cloudAreaX.y - cloudAreaX.x) / Mathf.Max(0.01f, wd.x));
                foreach (var c in clouds)
                {
                    if (c == null) continue;
                    var p = c.position + step;
                    if (p.x > cloudAreaX.y) p -= wrap;
                    c.position = p;
                }
            }

            foreach (var m in _waterMats)
                m.SetTextureOffset("_BaseMap", waterScroll * time);
        }
    }
}
