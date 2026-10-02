using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JuiceKing
{
    /// <summary>Floating joystick: touch/click anywhere and drag. WASD / arrow keys also work.</summary>
    public class InputJoystick : MonoBehaviour
    {
        public static Vector2 Direction { get; private set; }
        public static bool IsTouching { get; private set; }
        static readonly HashSet<string> Blocks = new HashSet<string>();

        /// <summary>True while any popup / cinematic freezes the player (camera peeks also block input).</summary>
        public static bool Blocked => Blocks.Count > 0;

        /// <summary>Freeze or release the player for one reason ("shop", "intro"...). Overlapping popups each hold their
        /// own key, so closing one never frees the player while another is still up.</summary>
        public static void Block(string reason, bool on)
        {
            if (on) Blocks.Add(reason);
            else Blocks.Remove(reason);
        }

        /// <summary>Drops every block (a new scene starts clean).</summary>
        public static void ClearBlocks() => Blocks.Clear();

        public RectTransform area;
        public RectTransform baseRect;
        public RectTransform knob;
        public CanvasGroup group;
        public float radius = 120f;

        bool _active;
        Vector2 _start;
        readonly List<RaycastResult> _hits = new List<RaycastResult>();

        void Start() => SetVisible(false);

        void Update()
        {
            Vector2 dir = Vector2.zero;
            if (Blocked || CameraFollow.Busy)
            {
                if (_active)
                {
                    _active = false;
                    SetVisible(false);
                }
                IsTouching = false;
                Direction = Vector2.zero;
                return;
            }

            bool pressed = false, pressedThisFrame = false;
            Vector2 pos = default;
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed)
            {
                pressed = true;
                pressedThisFrame = ts.primaryTouch.press.wasPressedThisFrame;
                pos = ts.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                pressed = true;
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                pos = Mouse.current.position.ReadValue();
            }

            if (pressedThisFrame && !OverUI(pos))
            {
                _active = true;
                ScreenToArea(pos, out _start);
                baseRect.anchoredPosition = _start;
                knob.anchoredPosition = Vector2.zero;
                SetVisible(true);
            }

            if (_active && pressed)
            {
                ScreenToArea(pos, out var cur);
                Vector2 delta = cur - _start;
                // Drag the base along when pulling past the rim (feels better on phones).
                if (delta.magnitude > radius)
                {
                    _start += delta - delta.normalized * radius;
                    baseRect.anchoredPosition = _start;
                    delta = cur - _start;
                }
                knob.anchoredPosition = delta;
                dir = delta / radius;
                if (dir.magnitude < 0.12f) dir = Vector2.zero;
            }
            else if (_active)
            {
                _active = false;
                SetVisible(false);
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (k != Vector2.zero) dir = k.normalized;
            }

            IsTouching = _active;
            Direction = Vector2.ClampMagnitude(dir, 1f);
            if (dir != Vector2.zero) Platform.NotifyFirstInput();
        }

        void ScreenToArea(Vector2 screen, out Vector2 local)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out local);
        }

        bool OverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            _hits.Clear();
            es.RaycastAll(data, _hits);
            return _hits.Count > 0;
        }

        void SetVisible(bool v)
        {
            if (group != null) group.alpha = v ? 1f : 0f;
        }

        void OnDisable() => Direction = Vector2.zero;
    }
}
