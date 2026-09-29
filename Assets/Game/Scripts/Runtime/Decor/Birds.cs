using UnityEngine;

namespace JuiceKing
{
    /// <summary>Birds gliding in lazy circles with occasional flaps (one Update drives the whole flock).</summary>
    public class Birds : MonoBehaviour
    {
        [System.Serializable]
        public struct Bird
        {
            public Transform body;
            public Transform wingL, wingR;
            public Vector3 center;
            public float radius;
            public float height;
            public float speed;
            public float phase;
        }

        public Bird[] birds;
        public float flapSpeed = 9f;

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < birds.Length; i++)
            {
                var b = birds[i];
                if (b.body == null) continue;
                float a = t * b.speed / Mathf.Max(1f, b.radius) + b.phase;
                // Slightly egg-shaped loop with a gentle rise and fall.
                Vector3 p = b.center + new Vector3(Mathf.Cos(a) * b.radius, b.height + Mathf.Sin(a * 2f + b.phase) * 0.8f, Mathf.Sin(a) * b.radius * 0.7f);
                Vector3 tangent = new Vector3(-Mathf.Sin(a) * b.radius, 0f, Mathf.Cos(a) * b.radius * 0.7f) * Mathf.Sign(b.speed);
                b.body.position = p;
                if (tangent.sqrMagnitude > 0.0001f)
                    b.body.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0f, 0f, -18f * Mathf.Sign(b.speed));
                // Flap in bursts, glide in between.
                float burst = Mathf.Clamp01(Mathf.Sin(t * 0.7f + b.phase * 3f) * 2f);
                float flap = Mathf.Sin(t * flapSpeed + b.phase) * (8f + 32f * burst);
                if (b.wingL != null) b.wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                if (b.wingR != null) b.wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
        }
    }
}
