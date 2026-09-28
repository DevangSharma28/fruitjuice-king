using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>A giant fruit that grows on a plot. Chainsaws damage it until it bursts into slices.</summary>
    public class FruitNode : MonoBehaviour
    {
        public enum State { Growing, Ready, Empty }

        public static readonly List<FruitNode> All = new List<FruitNode>();

        public FruitKind kind;
        public Transform visual;
        public float radius = 0.8f;
        public FruitField field;
        public Transform hpBar;
        public Transform hpFill;
        public Collider blocker;

        public State state { get; private set; } = State.Ready;
        public bool IsReady => state == State.Ready;
        public float Hp01 => _hp / _maxHp;

        float _hp, _maxHp, _regrow, _lastHit;
        Vector3 _baseScale;
        Quaternion _baseRot;
        float _wobble;

        void Awake()
        {
            _baseScale = visual.localScale;
            _baseRot = visual.localRotation;
            _maxHp = Balance.FruitHp[(int)kind];
            _hp = _maxHp;
            if (hpBar != null) hpBar.gameObject.SetActive(false);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;
            if (state == State.Empty)
            {
                _regrow -= dt;
                if (_regrow <= 0f) Grow();
            }

            if (hpBar != null && hpBar.gameObject.activeSelf && Time.time - _lastHit > 1.2f)
                hpBar.gameObject.SetActive(false);

            if (_wobble > 0f)
            {
                _wobble = Mathf.Max(0f, _wobble - dt * 4f);
                float a = Mathf.Sin(Time.time * 55f) * 6f * _wobble;
                visual.localRotation = _baseRot * Quaternion.Euler(a, 0f, a * 0.6f);
            }
        }

        public void Hit(float damage, Carrier by, Vector3 contact)
        {
            if (state != State.Ready) return;
            _hp -= damage;
            _lastHit = Time.time;
            _wobble = 1f;

            var col = Balance.JuiceColors[(int)kind];
            Fx.Chips(contact, col, 4);
            Sfx.Play(SfxId.Chop, by != null && by.isPlayer ? 0.45f : 0.12f, Random.Range(0.9f, 1.15f));

            // Squash a little on every hit.
            float s = 0.92f + 0.08f * Mathf.Clamp01(_hp / _maxHp);
            Tweener.Punch(visual, 0.07f, 0.15f, _baseScale * s);

            if (hpBar != null)
            {
                hpBar.gameObject.SetActive(true);
                var fs = hpFill.localScale;
                fs.x = Mathf.Clamp01(_hp / _maxHp);
                hpFill.localScale = fs;
                hpFill.localPosition = new Vector3(-0.5f + fs.x * 0.5f, 0f, -0.001f);
            }

            if (_hp <= 0f) Break(by);
        }

        void Break(Carrier by)
        {
            state = State.Empty;
            _regrow = Balance.RegrowDelay[(int)kind];
            if (hpBar != null) hpBar.gameObject.SetActive(false);
            if (blocker != null) blocker.enabled = false;

            Vector3 center = transform.position + Vector3.up * (radius * 0.8f);
            var col = Balance.JuiceColors[(int)kind];
            Fx.Splash(center, col, 26);
            Fx.Chips(center, Balance.FruitColors[(int)kind], 8);
            bool loud = by != null && by.isPlayer;
            Sfx.Play(SfxId.Splat, loud ? 0.7f : 0.2f);

            Tweener.Scale(visual, _baseScale * 1.15f, Vector3.zero, 0.18f, Ease.InQuad, () => visual.gameObject.SetActive(false));

            int n = Balance.SlicesPerFruit[(int)kind];
            var refs = GameRefs.I;
            for (int i = 0; i < n; i++)
            {
                var it = refs.SpawnItem(ItemTypes.Slice(kind), center, Quaternion.identity);
                float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.4f, 0.4f);
                float dist = Random.Range(radius + 0.2f, radius + 0.9f);
                Vector3 land = transform.position + new Vector3(Mathf.Cos(ang) * dist, 0.02f, Mathf.Sin(ang) * dist);
                LooseItems.Drop(it, land);
            }
        }

        void Grow()
        {
            state = State.Growing;
            _hp = _maxHp;
            visual.gameObject.SetActive(true);
            visual.localRotation = _baseRot;
            Tweener.Scale(visual, Vector3.zero, _baseScale, 0.9f, Ease.OutBack, () =>
            {
                state = State.Ready;
                if (blocker != null) blocker.enabled = true;
            });
        }
    }
}
