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
            /// <summary>Scene that queued the tween: target-less delays die with their scene.</summary>
            public int scene;
            // Returns true when finished.
            public abstract bool Step(float t);

            /// <summary>Clear references so pooled tweens do not keep objects or closures alive.</summary>
            public virtual void Reset()
            {
                target = null;
                onComplete = null;
                duration = time = delay = 0f;
                channel = 0;
                requiresTarget = true;
                dead = false;
            }

            public abstract void Release();
        }

        /// <summary>Per-type free list: tweens are recycled instead of allocated for every arc / punch / delay.</summary>
        static class Pool<T> where T : TweenBase, new()
        {
            static readonly Stack<T> Free = new Stack<T>(64);

            public static T Get()
            {
                var t = Free.Count > 0 ? Free.Pop() : new T();
                t.Reset();
                return t;
            }

            public static void Put(T t)
            {
                t.Reset();
                if (Free.Count < 512) Free.Push(t);
            }
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

            public override void Reset()
            {
                base.Reset();
                end = null;
                endRot = null;
                scale = false;
                height = 0f;
            }

            public override void Release() => Pool<ArcTween>.Put(this);

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
            public override void Release() => Pool<ScaleTween>.Put(this);
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
            public override void Release() => Pool<PunchTween>.Put(this);
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
            public override void Release() => Pool<ShakeTween>.Put(this);
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
            public override void Release() => Pool<LocalMoveTween>.Put(this);
            public override bool Step(float t)
            {
                target.localPosition = Vector3.LerpUnclamped(from, to, ease(t));
                return t >= 1f;
            }
        }

        class ActionTween : TweenBase
        {
            public Action<float> update;
            public override void Release() => Pool<ActionTween>.Put(this);

            public override void Reset()
            {
                base.Reset();
                update = null;
            }
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
            int activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if (_adding.Count > 0)
            {
                _tweens.AddRange(_adding);
                _adding.Clear();
            }

            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                var tw = _tweens[i];
                if (tw.dead || (tw.requiresTarget && tw.target == null) || (!tw.requiresTarget && tw.scene != activeScene))
                {
                    _tweens.RemoveAt(i);
                    tw.Release();
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
                    var cb = tw.onComplete;
                    tw.Release();
                    if (cb != null)
                    {
                        try { cb(); }
                        catch (Exception e) { Debug.LogException(e); }
                    }
                }
            }
        }

        static void Add(TweenBase tw, bool killSameChannel)
        {
            var inst = Instance;
            tw.scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
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
                if (list[i].target == target && (channel < 0 || list[i].channel == channel))
                {
                    var tw = list[i];
                    list.RemoveAt(i);
                    tw.Release();
                }
        }

        /// <summary>Parabolic jump from the current position to a (possibly moving) world target.</summary>
        public static void Arc(Transform t, Func<Vector3> end, float height, float duration, Action onComplete = null,
            Func<Quaternion> endRot = null, Vector3? endScale = null, float delay = 0f)
        {
            var tw = Pool<ArcTween>.Get();
            tw.target = t;
            tw.start = t.position;
            tw.end = end;
            tw.height = height;
            tw.duration = duration;
            tw.onComplete = onComplete;
            tw.startRot = t.rotation;
            tw.endRot = endRot;
            tw.delay = delay;
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
            var tw = Pool<ArcTween>.Get();
            tw.target = t;
            tw.start = t.position;
            tw.fixedEnd = end;
            tw.height = height;
            tw.duration = duration;
            tw.onComplete = onComplete;
            tw.delay = delay;
            Add(tw, true);
        }

        public static void Scale(Transform t, Vector3 from, Vector3 to, float duration, Func<float, float> ease = null,
            Action onComplete = null, float delay = 0f)
        {
            t.localScale = from;
            var tw = Pool<ScaleTween>.Get();
            tw.target = t;
            tw.from = from;
            tw.to = to;
            tw.duration = duration;
            tw.ease = ease ?? Ease.OutBack;
            tw.onComplete = onComplete;
            tw.delay = delay;
            tw.channel = 1;
            Add(tw, true);
        }

        public static void Punch(Transform t, float amount = 0.2f, float duration = 0.3f, Vector3? baseScale = null)
        {
            var tw = Pool<PunchTween>.Get();
            tw.target = t;
            tw.baseScale = baseScale ?? t.localScale;
            tw.amount = amount;
            tw.duration = duration;
            tw.channel = 1;
            Add(tw, true);
        }

        public static void Shake(Transform t, float amount = 0.08f, float duration = 0.2f, Vector3? basePos = null)
        {
            var tw = Pool<ShakeTween>.Get();
            tw.target = t;
            tw.basePos = basePos ?? t.localPosition;
            tw.amount = amount;
            tw.duration = duration;
            Add(tw, true);
        }

        public static void MoveLocal(Transform t, Vector3 to, float duration, Func<float, float> ease = null, Action onComplete = null, float delay = 0f)
        {
            var tw = Pool<LocalMoveTween>.Get();
            tw.target = t;
            tw.from = t.localPosition;
            tw.to = to;
            tw.duration = duration;
            tw.ease = ease ?? Ease.OutQuad;
            tw.onComplete = onComplete;
            tw.delay = delay;
            Add(tw, true);
        }

        /// <summary>Generic value tween. Pass an owner transform so it dies with it (or null to run always).</summary>
        public static void Value(Transform owner, float duration, Action<float> update, Action onComplete = null, float delay = 0f)
        {
            var tw = Pool<ActionTween>.Get();
            tw.target = owner;
            tw.requiresTarget = owner != null;
            tw.duration = duration;
            tw.update = update;
            tw.onComplete = onComplete;
            tw.delay = delay;
            tw.channel = 3;
            Add(tw, false);
        }

        static readonly Action<float> Noop = _ => { };

        public static void Delay(float seconds, Action action)
        {
            var tw = Pool<ActionTween>.Get();
            tw.requiresTarget = false;
            tw.duration = seconds;
            tw.update = Noop;
            tw.onComplete = action;
            tw.channel = 3;
            Add(tw, false);
        }
    }
}
