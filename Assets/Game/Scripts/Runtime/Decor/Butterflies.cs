using UnityEngine;

namespace JuiceKing
{
    /// <summary>A few fluttering butterflies drifting over flower beds (two flapping wing quads each).</summary>
    public class Butterflies : MonoBehaviour
    {
        [System.Serializable]
        public struct Fly
        {
            public Transform body;
            public Transform wingL, wingR;
            public Vector3 home;
            public float radius;
            [System.NonSerialized] public float seed;
        }

        public Fly[] flies;

        void Awake()
        {
            for (int i = 0; i < flies.Length; i++) flies[i].seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < flies.Length; i++)
            {
                var f = flies[i];
                if (f.body == null) continue;
                float s = f.seed;
                // Lissajous wander around home with a bobbing height.
                Vector3 p = f.home + new Vector3(
                    Mathf.Sin(t * 0.37f + s) * f.radius + Mathf.Sin(t * 1.3f + s * 2f) * 0.3f,
                    0.9f + Mathf.Sin(t * 2.1f + s) * 0.25f + Mathf.Sin(t * 5.3f + s) * 0.06f,
                    Mathf.Sin(t * 0.29f + s * 1.3f) * f.radius + Mathf.Cos(t * 1.1f + s) * 0.3f);
                Vector3 prev = f.body.position;
                f.body.position = p;
                Vector3 v = p - prev;
                v.y = 0f;
                if (v.sqrMagnitude > 0.00001f) f.body.rotation = Quaternion.Slerp(f.body.rotation, Quaternion.LookRotation(v), 0.1f);
                float flap = Mathf.Sin(t * 22f + s) * 60f;
                if (f.wingL != null) f.wingL.localRotation = Quaternion.Euler(0f, 0f, 20f + flap);
                if (f.wingR != null) f.wingR.localRotation = Quaternion.Euler(0f, 0f, -20f - flap);
            }
        }
    }
}
