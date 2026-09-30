using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// A berry bush. The chainsaw shakes it (it rustles and the berries jiggle); when it gives, the ripe berries pop off
    /// in a spray of leaves and juice and roll onto the path. The bush stays and fruits again. A fox raid leaves it wilted
    /// (drooping, faded leaves) until the farm is restored.
    /// </summary>
    public class BerryBushNode : FruitNode
    {
        [Tooltip("The foliage: shakes on every hit, wilts after a fox raid.")]
        public Transform bush;
        public Renderer[] leaves;
        public Material wiltedLeaves;
        [Tooltip("Broken twigs / torn leaves shown while damaged.")]
        public GameObject damageDecor;

        Quaternion _bushRot;
        Vector3 _bushScale;
        float _shake;
        Material[] _healthy;

        protected override void Awake()
        {
            base.Awake();
            if (bush != null)
            {
                _bushRot = bush.localRotation;
                _bushScale = bush.localScale;
            }
            if (leaves != null)
            {
                _healthy = new Material[leaves.Length];
                for (int i = 0; i < leaves.Length; i++) _healthy[i] = leaves[i] != null ? leaves[i].sharedMaterial : null;
            }
            if (damageDecor != null) damageDecor.SetActive(false);
        }

        void LateUpdate()
        {
            if (bush == null || _shake <= 0f) return;
            _shake = Mathf.Max(0f, _shake - Time.deltaTime * 3.2f);
            float a = Mathf.Sin(Time.time * 42f) * 7f * _shake;
            bush.localRotation = _bushRot * Quaternion.Euler(a, a * 0.4f, a * 0.7f);
            if (_shake <= 0f) bush.localRotation = Damaged ? _bushRot * Quaternion.Euler(10f, 0f, 6f) : _bushRot;
        }

        protected override void OnHit(bool player)
        {
            _shake = 1f;
            if (player && Random.value < 0.4f) Fx.Leaves(transform.position + Vector3.up * 0.8f, 1);
            if (player && Random.value < 0.3f) Sfx.Play(SfxId.Rustle, 0.25f, Random.Range(0.9f, 1.2f));
        }

        protected override void OnBreak(bool loud)
        {
            _shake = 1.5f;
            var col = Balance.JuiceColors[(int)kind];
            var fruitCol = Balance.FruitColors[(int)kind];
            Vector3 center = visual.position + Vector3.up * 0.1f;
            Fx.Leaves(center + Vector3.up * 0.2f, loud ? 6 : 2);
            Fx.Chips(center, fruitCol, loud ? 10 : 4);
            Fx.Drops(center, col, loud ? 6 : 2);
            Sfx.Play(SfxId.Leafy, loud ? 0.7f : 0.18f, Random.Range(1f, 1.2f));
            if (loud)
            {
                Sfx.Play(SfxId.Squish, 0.35f, Random.Range(1.1f, 1.3f));
                Fx.Ring(transform.position, new Color(1f, 1f, 1f, 0.5f), 2.6f);
                CameraFollow.Shake(0.04f, 0.1f);
            }
            // The berries pop off the branches...
            Tweener.Kill(visual);
            Tweener.Scale(visual, visual.localScale * 1.15f, Vector3.zero, 0.18f, Ease.InQuad, () => visual.gameObject.SetActive(false));
            // ...and bounce out onto the soil round the bush.
            SpawnPieces(center, transform.position, radius + 0.15f, radius + 0.85f);
        }

        protected override void Grow()
        {
            base.Grow();
            Fx.Sparkle(visual.position + Vector3.up * 0.2f, Color.Lerp(Balance.FruitColors[(int)kind], Color.white, 0.4f), 3);
        }

        protected override void OnDamaged()
        {
            _shake = 1.2f;
            if (bush != null)
            {
                Tweener.Kill(bush);
                Tweener.Scale(bush, bush.localScale, _bushScale * 0.82f, 0.4f, Ease.OutBack);
            }
            SetLeaves(true);
            if (damageDecor != null)
            {
                damageDecor.SetActive(true);
                Tweener.Scale(damageDecor.transform, Vector3.zero, Vector3.one, 0.35f, Ease.OutBack);
            }
            Fx.Leaves(transform.position + Vector3.up * 0.7f, 5);
        }

        protected override void OnRestored()
        {
            if (bush != null)
            {
                Tweener.Kill(bush);
                bush.localRotation = _bushRot;
                Tweener.Scale(bush, _bushScale * 0.7f, _bushScale, 0.8f, Ease.OutElastic);
            }
            SetLeaves(false);
            if (damageDecor != null) damageDecor.SetActive(false);
            Fx.Sparkle(transform.position + Vector3.up * 0.8f, new Color(0.6f, 1f, 0.5f), 8);
            Fx.Leaves(transform.position + Vector3.up * 0.9f, 3);
        }

        void SetLeaves(bool wilted)
        {
            if (leaves == null) return;
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i] != null) leaves[i].sharedMaterial = wilted && wiltedLeaves != null ? wiltedLeaves : _healthy[i];
        }
    }
}
