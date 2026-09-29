using UnityEngine;

namespace JuiceKing
{
    /// <summary>Fits a RectTransform to Screen.safeArea so HUD elements avoid notches, rounded corners and home bars.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform _rt;
        Rect _applied;
        Vector2Int _screen;

        void Awake()
        {
            _rt = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _applied || _screen.x != Screen.width || _screen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var r = Screen.safeArea;
            _applied = r;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var min = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            var max = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
