using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InQuad(float t) => t * t;
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }

    /// <summary>
    /// Tiny allocation-light tween engine. Tweens are keyed by target transform so they can be killed.
    /// </summary>
    public class Tweener : MonoBehaviour
    {
        abstract class TweenBase
        {
            public Transform target;
            public float duration, time, delay;
            public Action onComplete;
            public int channel; // 0 = position, 1 = scale, 2 = rotation, 3 = other
            public bool requiresTarget = true;
            public bool dead;
            // Returns true when finished.
            public abstract bool Step(float t);
        }

        class ArcTween : TweenBase
        {
            public Vector3 start;
            public Func<Vector3> end;
            public Vector3 fixedEnd;
            public float height;
            public Quaternion startRot;
            public Func<Quaternion> endRot;
            public Vector3 startScale, endScale;
            public bool scale;

            public override bool Step(float t)
            {
                float e = Ease.InOutQuad(t);
                Vector3 to = end != null ? end() : fixedEnd;
                Vector3 p = Vector3.LerpUnclamped(start, to, e);
                p.y += Mathf.Sin(t * Mathf.PI) * height;
                target.position = p;
                if (endRot != null) target.rotation = Quaternion.Slerp(startRot, endRot(), e);
                if (scale) target.localScale = Vector3.LerpUnclamped(startScale, endScale, e);
                return t >= 1f;
            }
        }

        class ScaleTween : TweenBase
        {
            public Vector3 from, to;
            public Func<float, float> ease;
            public override bool Step(float t)
            {
                target.localScale = Vector3.LerpUnclamped(from, to, ease(t));
                return t >= 1f;
            }
        }

        class PunchTween : TweenBase
        {
            public Vector3 baseScale;
            public float amount;
            public override bool Step(float t)
            {
                float s = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t) * amount;
                target.localScale = baseScale * (1f + s);
                if (t >= 1f) target.localScale = baseScale;
                return t >= 1f;
            }
        }

        class ShakeTween : TweenBase
        {
            public Vector3 basePos;
            public float amount;
            public override bool Step(float t)
            {
                float a = amount * (1f - t);
                target.localPosition = basePos + new Vector3(UnityEngine.Random.Range(-a, a), 0f, UnityEngine.Random.Range(-a, a));
                if (t >= 1f) target.localPosition = basePos;
                return t >= 1f;
            }
        }

        class LocalMoveTween : TweenBase
        {
            public Vector3 from, to;
            public Func<float, float> ease;
            public override bool Step(float t)
            {
                target.localPosition = Vector3.LerpUnclamped(from, to, ease(t));
                return t >= 1f;
            }
        }

        class ActionTween : TweenBase
        {
            public Action<float> update;
            public override bool Step(float t)
            {
                update(t);
                return t >= 1f;
            }
        }

        static Tweener _instance;
        readonly List<TweenBase> _tweens = new List<TweenBase>(256);
        readonly List<TweenBase> _adding = new List<TweenBase>(64);

        static Tweener Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[Tweener]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Tweener>();
                }
                return _instance;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_adding.Count > 0)
            {
                _tweens.AddRange(_adding);
                _adding.Clear();
            }

            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                var tw = _tweens[i];
                if (tw.dead || (tw.requiresTarget && tw.target == null))
                {
                    _tweens.RemoveAt(i);
                    continue;
                }

                if (tw.delay > 0f)
                {
                    tw.delay -= dt;
                    continue;
                }

                tw.time += dt;
                float t = tw.duration <= 0f ? 1f : Mathf.Clamp01(tw.time / tw.duration);
                bool done;
                try { done = tw.Step(t); }
                catch (Exception e) { Debug.LogException(e); done = true; }

                if (done)
                {
                    tw.dead = true;
                    _tweens.RemoveAt(i);
                    tw.onComplete?.Invoke();
                }
            }
        }

        static void Add(TweenBase tw, bool killSameChannel)
        {
            var inst = Instance;
            if (killSameChannel && tw.target != null) Kill(tw.target, tw.channel);
            inst._adding.Add(tw);
        }

        public static void Kill(Transform target, int channel = -1)
        {
            if (_instance == null || target == null) return;
            // Mark instead of removing so Kill is safe to call from inside a tween callback.
            var list = _instance._tweens;
            for (int i = 0; i < list.Count; i++)
                if (list[i].target == target && (channel < 0 || list[i].channel == channel)) list[i].dead = true;
            list = _instance._adding;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].target == target && (channel < 0 || list[i].channel == channel)) list.RemoveAt(i);
        }

        /// <summary>Parabolic jump from the current position to a (possibly moving) world target.</summary>
        public static void Arc(Transform t, Func<Vector3> end, float height, float duration, Action onComplete = null,
            Func<Quaternion> endRot = null, Vector3? endScale = null, float delay = 0f)
        {
            var tw = new ArcTween
            {
                target = t, start = t.position, end = end, height = height, duration = duration,
                onComplete = onComplete, startRot = t.rotation, endRot = endRot, delay = delay, channel = 0
            };
            if (endScale.HasValue)
            {
                tw.scale = true;
                tw.startScale = t.localScale;
                tw.endScale = endScale.Value;
            }
            if (delay > 0f) tw.start = t.position; // start sampled now; objects are static while delayed
            Add(tw, true);
        }

        public static void ArcTo(Transform t, Vector3 end, float height, float duration, Action onComplete = null, float delay = 0f)
        {
            var tw = new ArcTween
            {
                target = t, start = t.position, fixedEnd = end, height = height, duration = duration,
                onComplete = onComplete, delay = delay, channel = 0
            };
            Add(tw, true);
        }

        public static void Scale(Transform t, Vector3 from, Vector3 to, float duration, Func<float, float> ease = null,
            Action onComplete = null, float delay = 0f)
        {
            t.localScale = from;
            Add(new ScaleTween
            {
                target = t, from = from, to = to, duration = duration, ease = ease ?? Ease.OutBack,
                onComplete = onComplete, delay = delay, channel = 1
            }, true);
        }

        public static void Punch(Transform t, float amount = 0.2f, float duration = 0.3f, Vector3? baseScale = null)
        {
            Add(new PunchTween
            {
                target = t, baseScale = baseScale ?? t.localScale, amount = amount, duration = duration, channel = 1
            }, true);
        }

        public static void Shake(Transform t, float amount = 0.08f, float duration = 0.2f, Vector3? basePos = null)
        {
            Add(new ShakeTween
            {
                target = t, basePos = basePos ?? t.localPosition, amount = amount, duration = duration, channel = 0
            }, true);
        }

        public static void MoveLocal(Transform t, Vector3 to, float duration, Func<float, float> ease = null, Action onComplete = null, float delay = 0f)
        {
            Add(new LocalMoveTween
            {
                target = t, from = t.localPosition, to = to, duration = duration, ease = ease ?? Ease.OutQuad,
                onComplete = onComplete, delay = delay, channel = 0
            }, true);
        }

        /// <summary>Generic value tween. Pass an owner transform so it dies with it (or null to run always).</summary>
        public static void Value(Transform owner, float duration, Action<float> update, Action onComplete = null, float delay = 0f)
        {
            Add(new ActionTween
            {
                target = owner, requiresTarget = owner != null, duration = duration, update = update,
                onComplete = onComplete, delay = delay, channel = 3
            }, false);
        }

        public static void Delay(float seconds, Action action)
        {
            Add(new ActionTween { target = null, requiresTarget = false, duration = seconds, update = _ => { }, onComplete = action, channel = 3 }, false);
        }
    }
}
