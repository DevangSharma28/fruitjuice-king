using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>World-space pop-up text ("+$12", "MAX").</summary>
    public class FloatingText : MonoBehaviour
    {
        public TextMeshPro text;

        public static void Show(string msg, Vector3 pos, Color color, float size = 1f, float rise = 1.2f, float life = 0.9f)
        {
            var refs = GameRefs.I;
            if (refs == null || refs.floatingTextPrefab == null) return;
            var ft = Pool.Spawn(refs.floatingTextPrefab, pos, Quaternion.identity);
            ft.Play(msg, color, size, rise, life);
        }

        public void Play(string msg, Color color, float size, float rise, float life)
        {
            text.text = msg;
            text.color = color;
            var cam = GameRefs.I != null ? GameRefs.I.mainCamera : Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            Vector3 start = transform.position;
            Vector3 baseScale = Vector3.one * size;
            Tweener.Scale(transform, Vector3.zero, baseScale, 0.25f, Ease.OutBack);
            Tweener.Value(transform, life, t =>
            {
                transform.position = start + Vector3.up * (Ease.OutCubic(t) * rise);
                var c = text.color;
                c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                text.color = c;
            }, () => Pool.Despawn(gameObject));
        }
    }
}
