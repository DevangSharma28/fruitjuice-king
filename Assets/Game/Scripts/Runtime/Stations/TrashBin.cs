using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Bin for anything the player cannot use (e.g. slices of a fruit whose juicer is still locked).
    /// The lid pops open while the player stands on the pad and items fly in one by one.
    /// </summary>
    public class TrashBin : MonoBehaviour
    {
        public static TrashBin I { get; private set; }

        public Transform body;
        public Transform lid;
        public Transform mouth;
        public TrashZone zone;
        public float openAngle = -110f;

        Vector3 _bodyScale;
        float _open, _lidVel;
        bool _wasOpen;
        int _eaten;

        void Awake()
        {
            I = this;
            if (body != null) _bodyScale = body.localScale;
        }

        public void Swallow(StackItem item, Carrier from)
        {
            item.inTransit = true;
            item.onGround = false;
            item.transform.SetParent(null, true);
            var col = item.type.IsSlice() ? Balance.FruitColors[(int)item.type.Fruit()]
                : item.type.IsJuice() ? Balance.JuiceColors[(int)item.type.Fruit()] : Color.white;
            float spin = Random.Range(-1f, 1f);
            Tweener.Arc(item.transform, () => mouth.position, 1.6f, 0.34f, () =>
            {
                item.Despawn();
                _eaten++;
                if (body != null) Tweener.Punch(body, 0.12f, 0.22f, _bodyScale);
                Fx.Chips(mouth.position + Vector3.up * 0.1f, col, 3);
                if (_eaten % 3 == 1) Fx.Poof(mouth.position + Vector3.up * 0.2f, 2);
                Sfx.Play(SfxId.Trash, 0.45f, Random.Range(0.9f, 1.15f));
            }, () => Quaternion.Euler(0f, spin * 360f, spin * 180f), Vector3.one * 0.35f);
        }

        void Update()
        {
            bool open = zone != null && zone.PlayerInside;
            if (open != _wasOpen)
            {
                _wasOpen = open;
                Sfx.Play(SfxId.Lid, open ? 0.35f : 0.28f, open ? 1.1f : 0.9f);
                if (!open && _eaten > 0)
                {
                    Fx.Poof(mouth.position + Vector3.up * 0.3f, 5);
                    _eaten = 0;
                }
            }

            // Springy lid.
            float target = open ? 1f : 0f;
            float dt = Time.deltaTime;
            _lidVel += (target - _open) * 220f * dt;
            _lidVel *= Mathf.Exp(-14f * dt);
            _open += _lidVel * dt;
            if (lid != null) lid.localRotation = Quaternion.Euler(_open * openAngle, 0f, 0f);
        }
    }
}
