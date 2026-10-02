using UnityEngine;
using UnityEngine.EventSystems;

namespace JuiceKing
{
    /// <summary>Squishes a button while it is held, springs back on release.</summary>
    public class UIPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressedScale = 0.9f;
        Vector3 _base = Vector3.one;
        bool _down;

        void Awake() => _base = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            _down = true;
            Tweener.Scale(transform, transform.localScale, _base * pressedScale, 0.08f, Ease.OutQuad);
            // A light tick under the finger (rate-limited, off in Settings, device only).
            Haptics.Play(HapticKind.Selection);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!_down) return;
            _down = false;
            Tweener.Scale(transform, transform.localScale, _base, 0.3f, Ease.OutElastic);
        }
    }
}
