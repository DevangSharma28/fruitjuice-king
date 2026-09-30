using System.Collections;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// The berry thief. A little procedural fox: bounding gait (body bob, leg swing, tail wave), sniffing, pouncing into a
    /// bush and trotting off with berries in its mouth. <see cref="FoxRaid"/> scripts it with the coroutines below.
    /// </summary>
    public class FoxActor : MonoBehaviour
    {
        public Transform body;
        public Transform head;
        public Transform tail;
        public Transform[] legs;
        [Tooltip("Berries in its mouth, shown after the raid.")]
        public GameObject loot;
        public AudioSource steps;

        float _gait;      // 0 idle .. 1 running
        float _phase;
        float _sniff;
        Vector3 _bodyPos, _headPos;
        Quaternion _headRot, _tailRot;
        Quaternion[] _legRot;
        Vector3 _scale;

        void Awake()
        {
            _scale = transform.localScale;
            if (body != null) _bodyPos = body.localPosition;
            if (head != null)
            {
                _headPos = head.localPosition;
                _headRot = head.localRotation;
            }
            if (tail != null) _tailRot = tail.localRotation;
            if (legs != null)
            {
                _legRot = new Quaternion[legs.Length];
                for (int i = 0; i < legs.Length; i++) _legRot[i] = legs[i] != null ? legs[i].localRotation : Quaternion.identity;
            }
            if (loot != null) loot.SetActive(false);
        }

        public void Appear(Vector3 at, Vector3 facing)
        {
            gameObject.SetActive(true);
            transform.position = at;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(facing);
            Tweener.Scale(transform, Vector3.zero, _scale, 0.35f, Ease.OutBack);
            Fx.Leaves(at + Vector3.up * 0.4f, 4);
            Fx.Dust(at, 3);
            Sfx.Play(SfxId.Yip, 0.55f, Random.Range(1f, 1.1f));
            SetLoot(false);
        }

        public void Vanish()
        {
            Fx.Leaves(transform.position + Vector3.up * 0.4f, 4);
            Fx.Dust(transform.position, 3);
            Tweener.Scale(transform, transform.localScale, Vector3.zero, 0.25f, Ease.InQuad, () => gameObject.SetActive(false));
        }

        public void SetLoot(bool on)
        {
            if (loot == null) return;
            loot.SetActive(on);
            if (on) Tweener.Scale(loot.transform, Vector3.zero, Vector3.one, 0.3f, Ease.OutBack);
        }

        /// <summary>Run to a point on the ground (speed in m/s), easing in and out.</summary>
        public IEnumerator RunTo(Vector3 target, float speed)
        {
            target.y = transform.position.y;
            float v = 0f;
            float dustT = 0f;
            while (true)
            {
                Vector3 d = target - transform.position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist < 0.05f) break;
                float want = Mathf.Min(speed, Mathf.Sqrt(2f * 18f * dist) + 0.5f);
                v = Mathf.MoveTowards(v, want, Time.deltaTime * 22f);
                float step = Mathf.Min(dist, v * Time.deltaTime);
                transform.position += d / dist * step;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-14f * Time.deltaTime));
                _gait = Mathf.MoveTowards(_gait, Mathf.Clamp01(v / 3f), Time.deltaTime * 6f);
                dustT -= Time.deltaTime;
                if (dustT <= 0f)
                {
                    dustT = 0.16f;
                    Fx.Dust(transform.position, 1);
                }
                yield return null;
            }
            _gait = 0f;
        }

        public IEnumerator Sniff(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                _sniff = Mathf.Clamp01(Mathf.Min(t, seconds - t) * 5f);
                yield return null;
            }
            _sniff = 0f;
        }

        /// <summary>Pounce onto a bush: a hop in, a frantic rummage, a hop back out.</summary>
        public IEnumerator Pounce(Vector3 bush, System.Action onHit)
        {
            Vector3 start = transform.position;
            Vector3 d = bush - start;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
            Vector3 into = Vector3.Lerp(start, new Vector3(bush.x, start.y, bush.z), 0.55f);
            Sfx.Play(SfxId.Rustle, 0.5f, Random.Range(0.9f, 1.1f));
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.22f);
                transform.position = Vector3.Lerp(start, into, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.45f;
                yield return null;
            }
            onHit?.Invoke();
            t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                if (body != null) body.localRotation = Quaternion.Euler(Mathf.Sin(t * 60f) * 10f, Mathf.Sin(t * 45f) * 18f, 0f);
                yield return null;
            }
            if (body != null) body.localRotation = Quaternion.identity;
            t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.2f);
                transform.position = Vector3.Lerp(into, start, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.3f;
                yield return null;
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            _phase += dt * Mathf.Lerp(3f, 15f, _gait);
            float bob = Mathf.Abs(Mathf.Sin(_phase)) * 0.12f * _gait;
            if (body != null) body.localPosition = _bodyPos + Vector3.up * (bob + Mathf.Sin(Time.time * 2f) * 0.01f);
            if (legs != null && _legRot != null)
                for (int i = 0; i < legs.Length; i++)
                {
                    if (legs[i] == null) continue;
                    // Front and back pairs move in a bounding (gallop) rhythm.
                    float off = i < 2 ? 0f : Mathf.PI * 0.8f;
                    float side = i % 2 == 0 ? 0f : 0.25f;
                    float a = Mathf.Sin(_phase + off + side) * 45f * _gait;
                    legs[i].localRotation = _legRot[i] * Quaternion.Euler(a, 0f, 0f);
                }
            if (tail != null)
            {
                float wave = Mathf.Sin(Time.time * (4f + _gait * 6f)) * (12f + 10f * _gait);
                tail.localRotation = _tailRot * Quaternion.Euler(-10f * _gait, wave, 0f);
            }
            if (head != null)
            {
                float sniffBob = _sniff > 0f ? Mathf.Sin(Time.time * 24f) * 0.03f * _sniff : 0f;
                head.localPosition = _headPos + Vector3.down * (0.12f * _sniff) + Vector3.up * sniffBob + Vector3.up * bob * 0.4f;
                head.localRotation = _headRot * Quaternion.Euler(28f * _sniff, Mathf.Sin(Time.time * 1.7f) * 12f * (1f - _gait), 0f);
            }
        }
    }
}
