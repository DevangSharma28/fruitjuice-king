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
        public bool IsReady => state == State.Ready && !Damaged;
        /// <summary>Raided by a fox: stays bare until <see cref="Restore"/>.</summary>
        public bool Damaged { get; private set; }
        public float Hp01 => _hp / _maxHp;

        float _hp, _maxHp, _regrow, _lastHit;
        protected Vector3 _baseScale;
        protected Vector3 _basePos;
        protected Quaternion _baseRot;
        float _wobble;
        float _idlePhase;

        protected virtual void Awake()
        {
            _baseScale = visual.localScale;
            _basePos = visual.localPosition;
            _baseRot = visual.localRotation;
            _maxHp = Balance.FruitHp[(int)kind];
            _hp = _maxHp;
            _idlePhase = Random.value * 10f;
            if (hpBar != null) hpBar.gameObject.SetActive(false);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;
            if (state == State.Empty && !Damaged)
            {
                _regrow -= dt * Boosts.WorkMult;
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
            else if (state == State.Ready)
            {
                // Gentle idle sway so the field feels alive.
                float s = Mathf.Sin(Time.time * 1.3f + _idlePhase) * 1.6f;
                visual.localRotation = _baseRot * Quaternion.Euler(s, 0f, s * 0.7f);
            }
        }

        public void Hit(float damage, Carrier by, Vector3 contact)
        {
            if (state != State.Ready || Damaged) return;
            _hp -= damage;
            _lastHit = Time.time;
            _wobble = 1f;

            var col = Balance.JuiceColors[(int)kind];
            bool player = by != null && by.isPlayer;
            Fx.Chips(contact, col, 4);
            if (player) Fx.Drops(contact, col, 2);
            Sfx.Play(SfxId.Chop, player ? 0.45f : 0.1f, Random.Range(0.9f, 1.15f));

            // Squash a little on every hit.
            float s = 0.92f + 0.08f * Mathf.Clamp01(_hp / _maxHp);
            Tweener.Punch(visual, 0.07f, 0.15f, _baseScale * s);
            OnHit(player);

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
            _regrow = Economy.RegrowDelay(kind);
            if (GameManager.I != null) GameManager.I.NotifyFruitHarvested();
            if (hpBar != null) hpBar.gameObject.SetActive(false);
            if (blocker != null) blocker.enabled = false;

            OnBreak(by != null && by.isPlayer);
        }

        /// <summary>A fox tore through: the fruit is gone and nothing grows until the farm is restored.</summary>
        public void Damage()
        {
            if (Damaged) return;
            Damaged = true;
            if (hpBar != null) hpBar.gameObject.SetActive(false);
            if (blocker != null) blocker.enabled = false;
            if (state != State.Empty)
            {
                state = State.Empty;
                Tweener.Kill(visual);
                Tweener.Scale(visual, visual.localScale, Vector3.zero, 0.2f, Ease.InQuad, () => visual.gameObject.SetActive(false));
            }
            OnDamaged();
        }

        /// <summary>Back to health: grows again after <paramref name="delay"/> seconds.</summary>
        public void Restore(float delay)
        {
            if (!Damaged) return;
            Damaged = false;
            _regrow = delay;
            OnRestored();
        }

        protected virtual void OnDamaged() { }
        protected virtual void OnRestored() { }

        /// <summary>Extra feedback per chainsaw hit (tree shake, falling leaves...).</summary>
        protected virtual void OnHit(bool player) { }

        /// <summary>Default harvest: the giant fruit bursts where it grows and scatters its pieces.</summary>
        protected virtual void OnBreak(bool loud)
        {
            Vector3 center = transform.position + Vector3.up * (radius * 0.8f);
            var col = Balance.JuiceColors[(int)kind];
            Fx.JuiceBurst(center, transform.position, col, loud ? 1f : 0.6f);
            Fx.Chips(center, Balance.FruitColors[(int)kind], 8);
            Fx.Leaves(center + Vector3.up * 0.3f, loud ? 5 : 2);
            if (loud)
            {
                Fx.Ring(transform.position, new Color(1f, 1f, 1f, 0.55f), radius * 3.2f);
                CameraFollow.Shake(0.06f, 0.12f);
            }
            Sfx.Play(SfxId.Splat, loud ? 0.75f : 0.18f, Random.Range(0.92f, 1.08f));

            Tweener.Scale(visual, _baseScale * 1.18f, Vector3.zero, 0.16f, Ease.InQuad, () => visual.gameObject.SetActive(false));
            SpawnPieces(center, transform.position, radius + 0.2f, radius + 0.9f);
        }

        /// <summary>Scatter this fruit's harvest pieces from <paramref name="from"/> onto the ground around <paramref name="around"/>.</summary>
        protected void SpawnPieces(Vector3 from, Vector3 around, float minDist, float maxDist)
        {
            int n = Economy.SlicesPerFruit(kind);
            var refs = GameRefs.I;
            for (int i = 0; i < n; i++)
            {
                var it = refs.SpawnItem(ItemTypes.Slice(kind), from, Quaternion.identity);
                float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.4f, 0.4f);
                float dist = Random.Range(minDist, maxDist);
                Vector3 land = new Vector3(around.x + Mathf.Cos(ang) * dist, 0.02f, around.z + Mathf.Sin(ang) * dist);
                LooseItems.Drop(it, land, i * 0.03f);
            }
        }

        protected virtual void Grow()
        {
            state = State.Growing;
            _hp = _maxHp;
            visual.gameObject.SetActive(true);
            visual.localRotation = _baseRot;
            visual.localPosition = _basePos;
            Fx.Leaves(visual.position + Vector3.up * 0.3f, 2);
            Tweener.Scale(visual, Vector3.zero, _baseScale, 0.9f, Ease.OutElastic, () =>
            {
                state = State.Ready;
                if (blocker != null) blocker.enabled = true;
            });
        }
    }
}
