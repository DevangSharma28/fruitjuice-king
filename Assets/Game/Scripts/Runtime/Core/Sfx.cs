using System;
using UnityEngine;

namespace JuiceKing
{
    public enum SfxId
    {
        Pop, Chop, Splat, Coin, Cash, Unlock, Drop, Click, Pour, Error,
        Tink, Trash, Lid, Whoosh, Step, Sparkle, Reward, Bubble,
        Crack, Slice, Leafy, Squish, Horn, Depart, Fanfare, BigCash, Load,
        Ding, Yip, Rustle, Chime, Thud, Tape, Count
    }

    /// <summary>
    /// Procedurally synthesised sound effects, so the game needs no audio assets.
    /// Tuned to be soft and tactile (short transients, rounded tails, no harsh square waves).
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;

        static Sfx _i;
        static AudioClip[] _clips;
        static AudioClip _chainsaw, _juicerLoop, _ambient, _engine;
        AudioSource[] _sources;
        AudioSource _ambientSource;
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

        public static bool AmbienceOn
        {
            get => PlayerPrefs.GetInt("jk_ambience", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("jk_ambience", value ? 1 : 0);
                if (_i != null && _i._ambientSource != null) _i._ambientSource.mute = !value;
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

        public static AudioClip EngineClip
        {
            get
            {
                Ensure();
                return _engine;
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

        /// <summary>Synthesise every clip now (the loading screen calls this so the first seconds of play don't hitch).</summary>
        public static void Warmup() => Ensure();

        static void Ensure()
        {
            if (_i != null) return;
            var go = new GameObject("[Sfx]");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Sfx>();
            _i._sources = new AudioSource[20];
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

        /// <summary>Soft outdoor bed: breeze and distant birds.</summary>
        public static void StartAmbient(float volume = 0.32f)
        {
            Ensure();
            if (_i._ambientSource == null)
            {
                var s = _i.gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = true;
                s.spatialBlend = 0f;
                s.clip = _ambient;
                _i._ambientSource = s;
            }
            _i._ambientSource.volume = volume;
            _i._ambientSource.mute = !AmbienceOn;
            if (!_i._ambientSource.isPlaying) _i._ambientSource.Play();
        }

        public static void Play(SfxId id, float volume = 1f, float pitch = 1f)
        {
            Ensure();
            float now = Time.unscaledTime;
            if (now - _i._lastPlay[(int)id] < 0.03f) return;
            _i._lastPlay[(int)id] = now;
            var src = _i._sources[_i._next];
            _i._next = (_i._next + 1) % _i._sources.Length;
            src.pitch = pitch * UnityEngine.Random.Range(0.96f, 1.04f);
            src.PlayOneShot(_clips[(int)id], volume);
        }

        // ---------------------------------------------------------------- synthesis

        static void BuildClips()
        {
            _clips = new AudioClip[(int)SfxId.Count];

            // Pickup: round "bloop" with a soft attack.
            _clips[(int)SfxId.Pop] = Make("pop", 0.09f, (t, d) =>
            {
                float f = Mathf.Lerp(420f, 980f, Mathf.Sqrt(t / d));
                return Sin(f * t) * Env(t, 0.003f, 0.05f) * 0.7f;
            }, 0.5f);

            // Chainsaw bite: crisp crunch + dull thud.
            _clips[(int)SfxId.Chop] = Make("chop", 0.11f, (t, d) =>
            {
                float crackle = Crackle(0.35f) * Env(t, 0.001f, 0.05f);
                float hiss = Noise() * Env(t, 0.001f, 0.025f);
                float thud = Sin(Mathf.Lerp(160f, 90f, t / d) * t) * Env(t, 0.002f, 0.05f);
                return crackle * 0.45f + hiss * 0.35f + thud * 0.55f;
            }, 0.45f, 0.02f);

            // Fruit burst: wet squelch with little bubbly blips.
            var blips = new float[6];
            for (int i = 0; i < blips.Length; i++) blips[i] = 0.01f + i * 0.028f + (float)Rnd() * 0.012f;
            _clips[(int)SfxId.Splat] = Make("splat", 0.42f, (t, d) =>
            {
                float n = Noise() * Env(t, 0.004f, 0.16f);
                float body = Sin(Mathf.Lerp(150f, 55f, t / d) * t) * Env(t, 0.004f, 0.14f);
                float wet = 0f;
                for (int i = 0; i < blips.Length; i++)
                {
                    float lt = t - blips[i];
                    if (lt < 0f || lt > 0.03f) continue;
                    float f = 700f + i * 130f + lt * 26000f;
                    wet += Sin(f * lt) * Env(lt, 0.001f, 0.012f);
                }
                return n * 0.45f + body * 0.6f + wet * 0.3f;
            }, 0.18f);

            // Coin: two metallic clinks (inharmonic partials).
            _clips[(int)SfxId.Coin] = Make("coin", 0.32f, (t, d) =>
            {
                float s = Clink(t, 2350f) + Clink(t - 0.045f, 2950f) * 0.6f;
                return s * 0.38f;
            });

            // Register: drawer click then a bright "ching".
            _clips[(int)SfxId.Cash] = Make("cash", 0.7f, (t, d) =>
            {
                float click = Noise() * Env(t, 0.001f, 0.015f) * 0.5f;
                float lt = t - 0.04f;
                float vib = 1f + 0.003f * Mathf.Sin(2f * Mathf.PI * 7f * t);
                float bell = lt < 0f ? 0f : (Sin(2093f * vib * lt) + 0.7f * Sin(2637f * vib * lt) + 0.35f * Sin(3136f * vib * lt)) * Env(lt, 0.002f, 0.35f);
                return click + bell * 0.28f;
            });

            // Unlock: marimba arpeggio + shimmer.
            _clips[(int)SfxId.Unlock] = Make("unlock", 0.95f, (t, d) =>
            {
                float s = Marimba(t, 523.25f) + Marimba(t - 0.08f, 659.25f) + Marimba(t - 0.16f, 783.99f) + Marimba(t - 0.24f, 1046.5f) * 1.1f;
                return s * 0.26f + Glitter(t - 0.24f, 0.4f) * 0.12f;
            });

            // Slice into a crate: soft wooden "tok".
            _clips[(int)SfxId.Drop] = Make("drop", 0.1f, (t, d) =>
            {
                float f = Mathf.Lerp(560f, 330f, t / d);
                return Sin(f * t) * Env(t, 0.001f, 0.035f) * 0.6f + Noise() * Env(t, 0.0005f, 0.006f) * 0.25f;
            }, 0.6f);

            // UI tap: woodblock tick.
            _clips[(int)SfxId.Click] = Make("click", 0.06f, (t, d) =>
                Sin(1250f * t) * Env(t, 0.0008f, 0.018f) * 0.55f + Noise() * Env(t, 0.0005f, 0.004f) * 0.2f, 0.7f);

            // Juice pouring: bubbling gurgle over a soft hiss.
            var gurgles = new float[9];
            for (int i = 0; i < gurgles.Length; i++) gurgles[i] = 0.02f + i * 0.038f + (float)Rnd() * 0.02f;
            _clips[(int)SfxId.Pour] = Make("pour", 0.42f, (t, d) =>
            {
                float g = 0f;
                for (int i = 0; i < gurgles.Length; i++)
                {
                    float lt = t - gurgles[i];
                    if (lt < 0f || lt > 0.035f) continue;
                    g += Sin((260f + (i % 3) * 90f + lt * 16000f) * lt) * Env(lt, 0.002f, 0.014f);
                }
                return g * 0.35f + Noise() * Env(t, 0.03f, 0.25f) * 0.08f;
            }, 0.35f);

            // Gentle "bonk".
            _clips[(int)SfxId.Error] = Make("error", 0.2f, (t, d) =>
            {
                float f = Mathf.Lerp(250f, 170f, t / d);
                return (Sin(f * t) + 0.3f * Sin(2f * f * t)) * Env(t, 0.004f, 0.1f) * 0.45f;
            }, 0.5f);

            // Cup set down: glass tink.
            _clips[(int)SfxId.Tink] = Make("tink", 0.2f, (t, d) =>
                (Sin(1830f * t) + 0.5f * Sin(4120f * t) * Env(t, 0.001f, 0.05f) + 0.25f * Sin(6350f * t) * Env(t, 0.001f, 0.03f)) *
                Env(t, 0.0008f, 0.09f) * 0.3f);

            // Trash: thunk + papery rustle.
            _clips[(int)SfxId.Trash] = Make("trash", 0.24f, (t, d) =>
            {
                float thump = Sin(Mathf.Lerp(120f, 70f, t / d) * t) * Env(t, 0.002f, 0.06f);
                float rustle = Crackle(0.2f) * Env(t - 0.02f, 0.01f, 0.08f);
                return thump * 0.65f + rustle * 0.35f;
            }, 0.35f);

            // Bin lid: small metal clank.
            _clips[(int)SfxId.Lid] = Make("lid", 0.22f, (t, d) =>
            {
                float s = Sin(910f * t) + 0.6f * Sin(1540f * t) + 0.35f * Sin(2770f * t);
                return s * Env(t, 0.001f, 0.06f) * 0.22f + Noise() * Env(t, 0.0005f, 0.006f) * 0.2f;
            }, 0.8f);

            // Soft whoosh for reveals and panels.
            _clips[(int)SfxId.Whoosh] = Make("whoosh", 0.4f, (t, d) =>
            {
                float k = t / d;
                float env = Mathf.Sin(k * Mathf.PI);
                return Noise() * env * env * 0.5f;
            }, 0.08f);

            // Footstep tap.
            _clips[(int)SfxId.Step] = Make("step", 0.06f, (t, d) =>
                Noise() * Env(t, 0.001f, 0.015f) * 0.5f + Sin(95f * t) * Env(t, 0.001f, 0.02f) * 0.35f, 0.15f);

            // Glitter.
            _clips[(int)SfxId.Sparkle] = Make("sparkle", 0.45f, (t, d) => Glitter(t, 0.3f) * 0.3f);

            // Reward fanfare: rising major chime + glitter.
            _clips[(int)SfxId.Reward] = Make("reward", 1.1f, (t, d) =>
            {
                float s = Marimba(t, 659.25f) + Marimba(t - 0.1f, 783.99f) + Marimba(t - 0.2f, 987.77f) + Marimba(t - 0.3f, 1318.5f) * 1.2f;
                s += (Sin(1318.5f * t) + Sin(1975.5f * t) * 0.5f) * Env(t - 0.3f, 0.01f, 0.45f) * 0.35f;
                return s * 0.24f + Glitter(t - 0.3f, 0.5f) * 0.14f;
            });

            // Tiny bubble pop.
            _clips[(int)SfxId.Bubble] = Make("bubble", 0.04f, (t, d) => Sin((600f + t * 30000f) * t) * Env(t, 0.001f, 0.01f) * 0.5f);

            // Chainsaw: 0.4 s loop, every periodic component completes whole cycles so it loops seamlessly.
            _chainsaw = Make("chainsaw", 0.4f, (t, d) =>
            {
                float s1 = Saw(50f * t) * 0.5f;
                float s2 = Saw(100f * t + 0.3f) * 0.3f;
                float s3 = Saw(150f * t + 0.6f) * 0.16f;
                float am = 0.78f + 0.22f * Mathf.Sin(2f * Mathf.PI * 25f * t);
                return (s1 + s2 + s3 + Noise() * 0.08f) * am * 0.45f;
            }, 0.18f);

            // Blender whirr with a juicy slosh.
            _juicerLoop = Make("juicer", 0.5f, (t, d) =>
            {
                float hum = Sin(120f * t) * 0.28f + Saw(240f * t) * 0.1f + Sin(480f * t) * 0.05f;
                float am = 0.82f + 0.18f * Mathf.Sin(2f * Mathf.PI * 8f * t);
                return (hum + Noise() * 0.06f) * am * 0.5f;
            }, 0.25f);

            // Coconut: hard woody crack, then a sloshy splash of milk.
            _clips[(int)SfxId.Crack] = Make("crack", 0.36f, (t, d) =>
            {
                float knock = Sin(Mathf.Lerp(420f, 260f, t / 0.05f) * t) * Env(t, 0.0008f, 0.03f);
                float snap = Crackle(0.5f) * Env(t, 0.0005f, 0.02f);
                float slosh = Noise() * Env(t - 0.05f, 0.01f, 0.14f) * 0.4f;
                float blip = t > 0.07f && t < 0.1f ? Sin((500f + (t - 0.07f) * 12000f) * (t - 0.07f)) * Env(t - 0.07f, 0.002f, 0.01f) : 0f;
                return knock * 0.7f + snap * 0.5f + slosh + blip * 0.3f;
            }, 0.4f);

            // Mango: clean soft slice + juicy squelch.
            _clips[(int)SfxId.Slice] = Make("slice", 0.3f, (t, d) =>
            {
                float swish = Noise() * Env(t, 0.004f, 0.04f) * 0.4f;
                float squelch = Noise() * Env(t - 0.03f, 0.005f, 0.12f) * 0.45f;
                float body = Sin(Mathf.Lerp(180f, 80f, t / d) * t) * Env(t - 0.03f, 0.003f, 0.1f);
                return swish + squelch + body * 0.5f;
            }, 0.22f);

            // Banana bunch: leafy thump.
            _clips[(int)SfxId.Leafy] = Make("leafy", 0.34f, (t, d) =>
            {
                float thump = Sin(Mathf.Lerp(130f, 60f, t / d) * t) * Env(t, 0.002f, 0.07f);
                float rustle = Crackle(0.3f) * Env(t, 0.01f, 0.16f);
                return thump * 0.7f + rustle * 0.35f;
            }, 0.3f);

            // Papaya: big soft squish with wet blips.
            _clips[(int)SfxId.Squish] = Make("squish", 0.45f, (t, d) =>
            {
                float n = Noise() * Env(t, 0.006f, 0.2f);
                float body = Sin(Mathf.Lerp(120f, 45f, t / d) * t) * Env(t, 0.004f, 0.18f);
                float wet = 0f;
                for (int i = 0; i < 4; i++)
                {
                    float lt = t - (0.03f + i * 0.05f);
                    if (lt > 0f && lt < 0.03f) wet += Sin((600f + i * 150f + lt * 20000f) * lt) * Env(lt, 0.001f, 0.012f);
                }
                return n * 0.4f + body * 0.6f + wet * 0.3f;
            }, 0.16f);

            // Friendly two-tone truck horn.
            _clips[(int)SfxId.Horn] = Make("horn", 0.55f, (t, d) =>
            {
                float env = t < 0.24f ? Env(t, 0.01f, 1f) * Mathf.Clamp01((0.24f - t) * 40f)
                    : Env(t - 0.28f, 0.01f, 1f) * Mathf.Clamp01((0.54f - t) * 40f);
                float f = t < 0.26f ? 392f : 330f;
                return (Saw(f * t) * 0.35f + Saw(f * 1.5f * t) * 0.2f) * env * 0.5f;
            }, 0.25f);

            // Engine rev and pull-away.
            _clips[(int)SfxId.Depart] = Make("depart", 1.1f, (t, d) =>
            {
                float k = t / d;
                float f = Mathf.Lerp(45f, 90f, Mathf.Sqrt(k));
                float env = Mathf.Sin(k * Mathf.PI) * 0.9f + 0.1f;
                return (Saw(f * t) * 0.4f + Saw(f * 2f * t) * 0.2f + Noise() * 0.1f) * env * 0.4f;
            }, 0.15f);

            // Delivery complete: brass-ish fanfare + glitter.
            float[] fanNotes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            _clips[(int)SfxId.Fanfare] = Make("fanfare", 1.3f, (t, d) =>
            {
                float s = 0f;
                for (int i = 0; i < 4; i++)
                {
                    float lt = t - i * 0.12f;
                    if (lt < 0f) continue;
                    float len = i == 3 ? 0.9f : 0.2f;
                    float env = Env(lt, 0.01f, len);
                    s += (Saw(fanNotes[i] * lt) * 0.3f + Sin(fanNotes[i] * lt)) * env;
                }
                return s * 0.18f + Glitter(t - 0.36f, 0.6f) * 0.15f;
            }, 0.35f);

            // Big cash: cascade of coin clinks.
            _clips[(int)SfxId.BigCash] = Make("bigcash", 0.9f, (t, d) =>
            {
                float s = 0f;
                for (int i = 0; i < 10; i++)
                {
                    float st = i * 0.07f + (i % 3) * 0.013f;
                    s += Clink(t - st, 2100f + (i * 137 % 7) * 180f) * (1f - i * 0.05f);
                }
                return s * 0.22f;
            });

            // Crate thunk while loading the truck.
            _clips[(int)SfxId.Load] = Make("load", 0.14f, (t, d) =>
                Sin(Mathf.Lerp(240f, 150f, t / d) * t) * Env(t, 0.001f, 0.045f) * 0.6f + Noise() * Env(t, 0.0005f, 0.008f) * 0.25f, 0.5f);

            // Idle diesel loop: 0.5 s, whole cycles of 40/80/120 Hz and a 10 Hz chug.
            _engine = Make("engine", 0.5f, (t, d) =>
            {
                float chug = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 10f * t);
                return (Saw(40f * t) * 0.45f + Saw(80f * t + 0.2f) * 0.25f + Sin(120f * t) * 0.15f + Noise() * 0.05f) * chug * 0.45f;
            }, 0.12f);

            // Oven timer: bright bell "ding" with a shimmering tail.
            _clips[(int)SfxId.Ding] = Make("ding", 1f, (t, d) =>
                (Sin(1568f * t) * Env(t, 0.001f, 0.6f) + 0.5f * Sin(3136f * t) * Env(t, 0.001f, 0.25f) + 0.2f * Sin(4704f * t) * Env(t, 0.001f, 0.1f)) * 0.35f);

            // Fox: a short two-part yip, pitch jumping up.
            _clips[(int)SfxId.Yip] = Make("yip", 0.34f, (t, d) =>
            {
                float s = 0f;
                for (int i = 0; i < 2; i++)
                {
                    float lt = t - i * 0.15f;
                    if (lt < 0f || lt > 0.13f) continue;
                    float f = Mathf.Lerp(700f + i * 150f, 1250f + i * 200f, Mathf.Sqrt(lt / 0.13f));
                    s += (Sin(f * lt) * 0.7f + Saw(f * lt) * 0.2f) * Env(lt, 0.006f, 0.05f);
                }
                return s * 0.5f + Noise() * Env(t, 0.002f, 0.02f) * 0.05f;
            }, 0.45f);

            // Bush rustle: soft crackly leaves.
            _clips[(int)SfxId.Rustle] = Make("rustle", 0.4f, (t, d) =>
                (Crackle(0.35f) * 0.6f + Noise() * 0.12f) * Env(t, 0.03f, 0.18f) * 0.6f, 0.35f, 0.3f);

            // Golden Apple: magical rising chime.
            float[] chime = { 1046.5f, 1318.5f, 1568f, 2093f };
            _clips[(int)SfxId.Chime] = Make("chime", 0.8f, (t, d) =>
            {
                float s = 0f;
                for (int i = 0; i < 4; i++)
                {
                    float lt = t - i * 0.06f;
                    if (lt < 0f) continue;
                    s += Sin(chime[i] * lt) * Env(lt, 0.002f, 0.35f) + 0.3f * Sin(chime[i] * 2.01f * lt) * Env(lt, 0.001f, 0.12f);
                }
                return s * 0.16f + Glitter(t - 0.1f, 0.4f) * 0.12f;
            });

            // Cardboard box landing / closing.
            _clips[(int)SfxId.Thud] = Make("thud", 0.22f, (t, d) =>
                Sin(Mathf.Lerp(150f, 70f, t / d) * t) * Env(t, 0.002f, 0.07f) * 0.8f + Noise() * Env(t, 0.001f, 0.02f) * 0.3f, 0.3f);

            // Packing tape: a zippy rip.
            _clips[(int)SfxId.Tape] = Make("tape", 0.35f, (t, d) =>
            {
                float buzz = Saw((180f + t * 400f) * t) * 0.3f + Noise() * 0.5f;
                return buzz * Env(t, 0.01f, 0.25f) * Mathf.Clamp01((d - t) * 12f) * 0.45f;
            }, 0.5f, 0.2f);

            _ambient = MakeAmbient(8f);
        }

        static float _lp, _hp, _hpIn;

        /// <summary>Render a clip. lowpass/highpass are one-pole coefficients (1 = off / 0 = off).</summary>
        static AudioClip Make(string name, float dur, Func<float, float, float> fn, float lowpass = 1f, float highpass = 0f)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            _lp = 0f;
            _hp = 0f;
            _hpIn = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = fn(t, dur);
                _lp += (v - _lp) * lowpass;
                float o = _lp;
                if (highpass > 0f)
                {
                    _hp = (1f - highpass) * (_hp + o - _hpIn);
                    _hpIn = o;
                    o = _hp;
                }
                // Short fade-out at the end avoids clicks.
                float tail = Mathf.Clamp01((n - i) / (Rate * 0.004f));
                data[i] = Mathf.Clamp(o * tail, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Seamless ambience: low breeze plus a few bird chirps, crossfaded at the loop point.</summary>
        static AudioClip MakeAmbient(float dur)
        {
            const float fade = 0.6f;
            int n = Mathf.CeilToInt(dur * Rate);
            int nf = Mathf.CeilToInt(fade * Rate);
            var raw = new float[n + nf];

            // Breeze.
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)Rate;
                lp1 += (Noise() - lp1) * 0.02f;
                lp2 += (lp1 - lp2) * 0.05f;
                float gust = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t / 4f + 1.3f) * Mathf.Sin(2f * Mathf.PI * t / 2.7f);
                raw[i] = lp2 * 1.6f * gust;
            }

            // Birds: little trilled chirps in groups.
            var rnd = new System.Random(42);
            float bt = 0.4f;
            while (bt < dur)
            {
                int count = 2 + rnd.Next(3);
                float baseF = 2600f + (float)rnd.NextDouble() * 1400f;
                float vol = 0.05f + (float)rnd.NextDouble() * 0.05f;
                for (int c = 0; c < count; c++)
                {
                    float st = bt + c * (0.09f + (float)rnd.NextDouble() * 0.05f);
                    float len = 0.05f + (float)rnd.NextDouble() * 0.06f;
                    float up = (float)rnd.NextDouble() < 0.5f ? 1f : -1f;
                    int i0 = Mathf.FloorToInt(st * Rate), i1 = Mathf.Min(raw.Length, Mathf.CeilToInt((st + len) * Rate));
                    for (int i = i0; i < i1; i++)
                    {
                        float lt = (i - i0) / (float)Rate;
                        float k = lt / len;
                        float f = baseF * (1f + up * 0.25f * k) + 180f * Mathf.Sin(2f * Mathf.PI * 32f * lt);
                        raw[i] += Mathf.Sin(2f * Mathf.PI * f * lt) * Mathf.Sin(k * Mathf.PI) * vol;
                    }
                }
                bt += 1.1f + (float)rnd.NextDouble() * 1.8f;
            }

            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = raw[i];
                if (i < nf)
                {
                    float k = i / (float)nf;
                    v = raw[i] * k + raw[n + i] * (1f - k);
                }
                data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            var clip = AudioClip.Create("ambient", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------------------------------------------------------------- building blocks

        static float Sin(float phaseHz) => Mathf.Sin(2f * Mathf.PI * phaseHz);

        static float Env(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return Mathf.Exp(-(t - attack) / Mathf.Max(0.0001f, decay) * 3f);
        }

        static float Clink(float t, float f)
        {
            if (t < 0f) return 0f;
            return Sin(f * t) * Env(t, 0.0008f, 0.14f) + 0.5f * Sin(f * 2.76f * t) * Env(t, 0.0008f, 0.06f) +
                   0.25f * Sin(f * 5.4f * t) * Env(t, 0.0008f, 0.03f);
        }

        static float Marimba(float t, float f)
        {
            if (t < 0f) return 0f;
            return Sin(f * t) * Env(t, 0.002f, 0.28f) + 0.35f * Sin(f * 4f * t) * Env(t, 0.001f, 0.05f);
        }

        static float Glitter(float t, float len)
        {
            if (t < 0f || t > len) return 0f;
            float s = 0f;
            for (int i = 0; i < 6; i++)
            {
                float st = i * len / 7f;
                float lt = t - st;
                if (lt < 0f || lt > 0.08f) continue;
                float f = 3200f + ((i * 7919) % 11) * 260f;
                s += Sin(f * lt) * Env(lt, 0.001f, 0.03f);
            }
            return s;
        }

        /// <summary>Sparse random impulses (crunch, rustle).</summary>
        static float Crackle(float density) => Rnd() < density ? Noise() : 0f;

        static float Saw(float phase) => 2f * (phase - Mathf.Floor(phase + 0.5f));

        static uint _seed = 1234567;

        static float Noise() => (float)Rnd() * 2f - 1f;

        static double Rnd()
        {
            _seed ^= _seed << 13;
            _seed ^= _seed >> 17;
            _seed ^= _seed << 5;
            return _seed / (double)uint.MaxValue;
        }
    }
}
