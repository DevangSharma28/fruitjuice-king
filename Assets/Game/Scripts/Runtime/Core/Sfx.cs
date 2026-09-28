using UnityEngine;

namespace JuiceKing
{
    public enum SfxId { Pop, Chop, Splat, Coin, Cash, Unlock, Drop, Click, Pour, Error, Count }

    /// <summary>
    /// Procedurally synthesised sound effects, so the game needs no audio assets.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 22050;

        static Sfx _i;
        static AudioClip[] _clips;
        static AudioClip _chainsaw, _juicerLoop;
        AudioSource[] _sources;
        int _next;
        readonly float[] _lastPlay = new float[(int)SfxId.Count];

        public static bool Muted
        {
            get => PlayerPrefs.GetInt("jk_mute", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("jk_mute", value ? 1 : 0);
                AudioListener.volume = value ? 0f : 1f;
            }
        }

        public static AudioClip ChainsawClip
        {
            get
            {
                Ensure();
                return _chainsaw;
            }
        }

        public static AudioClip JuicerClip
        {
            get
            {
                Ensure();
                return _juicerLoop;
            }
        }

        static void Ensure()
        {
            if (_i != null) return;
            var go = new GameObject("[Sfx]");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Sfx>();
            _i._sources = new AudioSource[16];
            for (int i = 0; i < _i._sources.Length; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _i._sources[i] = s;
            }
            BuildClips();
            AudioListener.volume = Muted ? 0f : 1f;
        }

        public static void Play(SfxId id, float volume = 1f, float pitch = 1f)
        {
            Ensure();
            float now = Time.unscaledTime;
            if (now - _i._lastPlay[(int)id] < 0.035f) return;
            _i._lastPlay[(int)id] = now;
            var src = _i._sources[_i._next];
            _i._next = (_i._next + 1) % _i._sources.Length;
            src.pitch = pitch * Random.Range(0.96f, 1.04f);
            src.PlayOneShot(_clips[(int)id], volume);
        }

        // ---------------------------------------------------------------- synthesis

        static void BuildClips()
        {
            _clips = new AudioClip[(int)SfxId.Count];
            _clips[(int)SfxId.Pop] = Make("pop", 0.08f, (t, d) =>
            {
                float f = Mathf.Lerp(520f, 1150f, t / d);
                return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.004f, 0.06f) * 0.8f;
            });
            _clips[(int)SfxId.Chop] = Make("chop", 0.09f, (t, d) =>
            {
                float n = Noise() * Env(t, 0.001f, 0.04f);
                float thump = Mathf.Sin(2f * Mathf.PI * 140f * t) * Env(t, 0.002f, 0.06f);
                return (n * 0.5f + thump * 0.6f);
            }, lowpass: 0.35f);
            _clips[(int)SfxId.Splat] = Make("splat", 0.32f, (t, d) =>
            {
                float n = Noise() * Env(t, 0.003f, 0.18f);
                float f = Mathf.Lerp(220f, 55f, t / d);
                float body = Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.004f, 0.2f);
                return n * 0.55f + body * 0.7f;
            }, lowpass: 0.25f);
            _clips[(int)SfxId.Coin] = Make("coin", 0.28f, (t, d) =>
            {
                float f = t < 0.05f ? 1318.5f : 1975.5f;
                float env = t < 0.05f ? Env(t, 0.002f, 0.05f) : Env(t - 0.05f, 0.002f, 0.2f);
                return (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t)) * env * 0.45f;
            });
            _clips[(int)SfxId.Cash] = Make("cash", 0.55f, (t, d) =>
            {
                float click = Noise() * Env(t, 0.001f, 0.02f) * 0.6f;
                float b1 = Mathf.Sin(2f * Mathf.PI * 2093f * t) * Env(t - 0.03f, 0.002f, 0.35f);
                float b2 = Mathf.Sin(2f * Mathf.PI * 2637f * t) * Env(t - 0.09f, 0.002f, 0.35f);
                return click + (b1 + b2) * 0.35f;
            });
            _clips[(int)SfxId.Unlock] = Make("unlock", 0.7f, (t, d) =>
            {
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                float s = 0f;
                for (int i = 0; i < 4; i++)
                {
                    float st = i * 0.09f;
                    if (t < st) continue;
                    float lt = t - st;
                    float f = notes[i];
                    s += (Mathf.Sin(2f * Mathf.PI * f * lt) + 0.25f * Mathf.Sin(6f * Mathf.PI * f * lt)) * Env(lt, 0.004f, 0.3f);
                }
                return s * 0.3f;
            });
            _clips[(int)SfxId.Drop] = Make("drop", 0.1f, (t, d) =>
            {
                float f = Mathf.Lerp(360f, 160f, t / d);
                return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.003f, 0.07f) * 0.7f;
            });
            _clips[(int)SfxId.Click] = Make("click", 0.05f, (t, d) =>
                Mathf.Sin(2f * Mathf.PI * 1100f * t) * Env(t, 0.001f, 0.03f) * 0.6f);
            _clips[(int)SfxId.Pour] = Make("pour", 0.35f, (t, d) =>
            {
                float bubble = Mathf.Sin(2f * Mathf.PI * (380f + 160f * Mathf.Sin(2f * Mathf.PI * 18f * t)) * t);
                return (bubble * 0.4f + Noise() * 0.2f) * Env(t, 0.02f, 0.25f);
            }, lowpass: 0.4f);
            _clips[(int)SfxId.Error] = Make("error", 0.18f, (t, d) =>
                (Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 180f * t)) * 0.3f) * Env(t, 0.005f, 0.14f), lowpass: 0.3f);

            // Chainsaw: 0.4 s loop, all periodic components complete whole cycles so it loops seamlessly.
            _chainsaw = Make("chainsaw", 0.4f, (t, d) =>
            {
                float saw1 = Saw(55f * t) * 0.5f;
                float saw2 = Saw(110f * t + 0.3f) * 0.35f;
                float saw3 = Saw(165f * t + 0.6f) * 0.2f;
                float am = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 30f * t);
                return (saw1 + saw2 + saw3 + Noise() * 0.12f) * am * 0.5f;
            }, lowpass: 0.55f);

            _juicerLoop = Make("juicer", 0.5f, (t, d) =>
            {
                float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.3f + Saw(240f * t) * 0.12f;
                float am = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 8f * t);
                return (hum + Noise() * 0.05f) * am * 0.5f;
            }, lowpass: 0.4f);
        }

        static float _lp;

        static AudioClip Make(string name, float dur, System.Func<float, float, float> fn, float lowpass = 1f)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            _lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = fn(t, dur);
                _lp += (v - _lp) * lowpass;
                data[i] = Mathf.Clamp(_lp, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return Mathf.Exp(-(t - attack) / Mathf.Max(0.0001f, decay) * 3f);
        }

        static float Saw(float phase) => 2f * (phase - Mathf.Floor(phase + 0.5f));

        static uint _seed = 1234567;

        static float Noise()
        {
            _seed ^= _seed << 13;
            _seed ^= _seed >> 17;
            _seed ^= _seed << 5;
            return (_seed / (float)uint.MaxValue) * 2f - 1f;
        }
    }
}
