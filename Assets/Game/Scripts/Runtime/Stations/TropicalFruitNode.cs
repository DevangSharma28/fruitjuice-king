using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Tree-borne tropical fruit. The chainsaw bites the plant; when it gives, the fruit drops from the tree and
    /// breaks open on the ground in a style of its own:
    /// coconuts crack and spill milk, mangoes split, banana bunches thump and scatter, papayas burst into wedges.
    /// </summary>
    public class TropicalFruitNode : FruitNode
    {
        public enum Style { CoconutCrack, MangoSplit, BananaBunch, PapayaBurst }

        public Style style;
        [Tooltip("The tree / plant that shakes when hit (not the fruit).")]
        public Transform plant;
        [Tooltip("Where the fruit lands, relative to the node.")]
        public Vector3 landOffset = new Vector3(0f, 0f, -0.9f);

        Quaternion _plantRot;
        float _shake;

        protected override void Awake()
        {
            base.Awake();
            if (plant != null) _plantRot = plant.localRotation;
        }

        void LateUpdate()
        {
            if (plant == null || _shake <= 0f) return;
            _shake = Mathf.Max(0f, _shake - Time.deltaTime * 3.5f);
            float a = Mathf.Sin(Time.time * 38f) * 5f * _shake;
            plant.localRotation = _plantRot * Quaternion.Euler(a, 0f, a * 0.5f);
            if (_shake <= 0f) plant.localRotation = _plantRot;
        }

        protected override void OnHit(bool player)
        {
            _shake = 1f;
            if (player && Random.value < 0.35f) Fx.Leaves(visual.position + Vector3.up * 0.4f, 1);
        }

        protected override void OnBreak(bool loud)
        {
            var land = transform.position + landOffset;
            land.y = 0.05f;
            var col = Balance.JuiceColors[(int)kind];
            var fruitCol = Balance.FruitColors[(int)kind];
            _shake = 1.4f;
            Fx.Leaves(visual.position + Vector3.up * 0.3f, loud ? 6 : 2);

            // The fruit tumbles out of the tree...
            Tweener.Kill(visual);
            float spin = Random.Range(-1f, 1f) * 200f;
            var start = visual.position;
            var startRot = visual.rotation;
            Tweener.Value(visual, 0.42f, t =>
            {
                float e = t * t;
                var p = Vector3.Lerp(start, land + Vector3.up * 0.35f, e);
                p.y += Mathf.Sin(t * Mathf.PI) * 0.35f;
                visual.position = p;
                visual.rotation = startRot * Quaternion.Euler(spin * t, 0f, spin * 0.5f * t);
            }, () =>
            {
                // ...and breaks open on impact.
                Vector3 center = land + Vector3.up * 0.4f;
                switch (style)
                {
                    case Style.CoconutCrack:
                        Fx.JuiceBurst(center, land, col, loud ? 0.9f : 0.5f);
                        Fx.Chips(center, fruitCol, 10);
                        Sfx.Play(SfxId.Crack, loud ? 0.8f : 0.2f, Random.Range(0.92f, 1.08f));
                        break;
                    case Style.MangoSplit:
                        Fx.JuiceBurst(center, land, col, loud ? 1f : 0.55f);
                        Fx.Chips(center, new Color(0.45f, 0.7f, 0.25f), 5);
                        Sfx.Play(SfxId.Slice, loud ? 0.75f : 0.2f, Random.Range(0.92f, 1.08f));
                        break;
                    case Style.BananaBunch:
                        Fx.Poof(center, 6);
                        Fx.Chips(center, fruitCol, 8);
                        Sfx.Play(SfxId.Leafy, loud ? 0.8f : 0.2f, Random.Range(0.9f, 1.1f));
                        break;
                    default:
                        Fx.JuiceBurst(center, land, col, loud ? 1.1f : 0.6f);
                        Fx.Chips(center, new Color(0.15f, 0.1f, 0.08f), 8);
                        Sfx.Play(SfxId.Squish, loud ? 0.8f : 0.2f, Random.Range(0.92f, 1.08f));
                        break;
                }
                if (loud)
                {
                    Fx.Ring(land, new Color(1f, 1f, 1f, 0.55f), 3f);
                    CameraFollow.Shake(0.07f, 0.14f);
                }
                Tweener.Scale(visual, visual.localScale * 1.2f, Vector3.zero, 0.14f, Ease.InQuad, () => visual.gameObject.SetActive(false));
                SpawnPieces(center, land, 0.5f, 1.4f);
            });
        }
    }
}
