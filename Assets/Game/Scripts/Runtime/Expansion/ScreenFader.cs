using System;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    /// <summary>Full-screen fade used between worlds. A fade-out sets a flag so the next scene starts black and fades in.</summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader _i;
        static bool _startBlack;

        public Image image;

        void Awake()
        {
            _i = this;
            SetAlpha(_startBlack ? 1f : 0f);
        }

        void SetAlpha(float a)
        {
            if (image == null) return;
            var c = image.color;
            c.a = a;
            image.color = c;
            image.raycastTarget = a > 0.01f;
            image.enabled = a > 0.001f;
        }

        public static void FadeOut(float duration, Action then)
        {
            _startBlack = true;
            if (_i == null || _i.image == null)
            {
                then?.Invoke();
                return;
            }
            Tweener.Kill(_i.image.transform);
            float from = _i.image.enabled ? _i.image.color.a : 0f;
            Tweener.Value(_i.image.transform, duration, t => _i.SetAlpha(Mathf.Lerp(from, 1f, t)), then);
        }

        /// <summary>The loading screen covers the scene change itself, so the next scene need not start black.</summary>
        public static void ClearStartBlack() => _startBlack = false;

        public static void FadeInIfBlack(float duration)
        {
            if (!_startBlack) return;
            _startBlack = false;
            if (_i == null || _i.image == null) return;
            _i.SetAlpha(1f);
            Tweener.Value(_i.image.transform, duration, t => _i.SetAlpha(1f - Ease.InQuad(t)), null, 0.15f);
        }
    }
}
