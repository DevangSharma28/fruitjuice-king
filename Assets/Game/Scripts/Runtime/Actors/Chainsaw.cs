using UnityEngine;

namespace JuiceKing
{
    /// <summary>Auto-cuts the nearest ready fruit in range. Used by the player and farmer helpers.</summary>
    public class Chainsaw : MonoBehaviour
    {
        public Carrier carrier;
        public Transform owner;
        public Transform bladeVisual;
        public Renderer chainRenderer;
        public AudioSource audioSource;
        public float range = 1.1f;
        public float dps = 7f;
        public float tickInterval = 0.14f;
        [Tooltip("Farmers only cut inside their own field.")]
        public FruitField restrictTo;
        public float volumeScale = 1f;

        public FruitNode Target { get; private set; }
        public bool IsCutting => Target != null;

        float _tick;
        Vector3 _bladeBase;
        Material _chainMat;
        float _chainOffset;
        float _rev;

        void Awake()
        {
            if (owner == null) owner = transform;
            if (bladeVisual != null) _bladeBase = bladeVisual.localPosition;
            if (chainRenderer != null) _chainMat = chainRenderer.material;
            if (audioSource != null)
            {
                audioSource.clip = Sfx.ChainsawClip;
                audioSource.loop = true;
                audioSource.volume = 0f;
                audioSource.Play();
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Target = null;
            var candidate = FindTarget();
            if (candidate != null)
            {
                if (carrier != null && carrier.IsFull) carrier.NotifyFull();
                else Target = candidate;
            }

            if (Target != null)
            {
                _tick += dt;
                if (_tick >= tickInterval)
                {
                    _tick -= tickInterval;
                    Vector3 toT = Target.transform.position - owner.position;
                    toT.y = 0f;
                    Vector3 contact = Target.transform.position - toT.normalized * Target.radius * 0.8f + Vector3.up * Target.radius * 0.6f;
                    Target.Hit(dps * tickInterval, carrier, contact);
                }
            }
            else _tick = tickInterval * 0.6f; // first hit lands quickly

            _rev = Mathf.MoveTowards(_rev, IsCutting ? 1f : 0f, dt * 6f);

            if (bladeVisual != null)
            {
                float j = 0.012f + _rev * 0.03f;
                bladeVisual.localPosition = _bladeBase + new Vector3(Random.Range(-j, j), Random.Range(-j, j), Random.Range(-j, j));
            }

            if (_chainMat != null)
            {
                _chainOffset += dt * (1.5f + _rev * 8f);
                _chainMat.mainTextureOffset = new Vector2(_chainOffset, 0f);
            }

            if (audioSource != null)
            {
                audioSource.volume = (0.06f + _rev * 0.28f) * volumeScale;
                audioSource.pitch = 0.85f + _rev * 0.45f;
            }
        }

        FruitNode FindTarget()
        {
            Vector3 p = owner.position;
            // Stick with the current target while it is valid.
            FruitNode best = null;
            float bestD = float.MaxValue;
            var all = FruitNode.All;
            for (int i = 0; i < all.Count; i++)
            {
                var n = all[i];
                if (!n.IsReady) continue;
                if (restrictTo != null && n.field != restrictTo) continue;
                Vector3 d = n.transform.position - p;
                d.y = 0f;
                float dist = d.magnitude - n.radius;
                if (dist > range) continue;
                if (dist < bestD)
                {
                    bestD = dist;
                    best = n;
                }
            }
            return best;
        }
    }
}
