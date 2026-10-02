using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>One game sound (<see cref="SfxId"/>) and the recorded clips that replace its synthesised version.</summary>
    [Serializable]
    public class SoundPick
    {
        /// <summary><see cref="SfxId"/> name ("Coin").</summary>
        public string id;
        /// <summary>Source clips (project paths). Several = random variants at runtime.</summary>
        public List<string> sources = new List<string>();
        /// <summary>Longest part kept after the leading silence is trimmed (s). 0 = whole clip.</summary>
        public float maxLength;
        /// <summary>Loudness relative to the synthesised sound it replaces (1 = the same).</summary>
        public float gain = 1f;
    }

    [Serializable]
    public class SoundPickList
    {
        public List<SoundPick> picks = new List<SoundPick>();
    }

    /// <summary>
    /// Recorded sound effects. The picks (which clips play for which <see cref="SfxId"/>) live in
    /// <c>Assets/Game/Audio/sound_picks.json</c>, edited by hand or with <see cref="SoundBoardWindow"/>.
    /// <see cref="Import"/> turns each source into a game-ready clip in <c>Generated/Audio/Resources/Sfx/</c>: mono,
    /// leading silence trimmed, cut to <c>maxLength</c> with a short fade, peak-normalised to -1 dBFS. <see cref="Sfx"/>
    /// loads them at boot, matches each one's loudness to the synthesised sound it replaces (times <c>gain</c>) and picks a
    /// random variant per play. Sounds without a pick keep their synthesised version (chainsaw, juicer, engine, ambience...).
    /// Sources: Feel / Nice Vibrations samples and the two Asset Store packs in <c>Assets/ThirdParty/Audio</c>.
    /// </summary>
    public static class SoundLibrary
    {
        public const string PicksPath = "Assets/Game/Audio/sound_picks.json";
        public const string OutDir = "Assets/Game/Generated/Audio/Resources/Sfx";

        const string NV = "Assets/Feel/NiceVibrations/HapticSamples/";
        const string CGS = "Assets/ThirdParty/Audio/Casual Game Sounds/CasualGameSounds/";
        const string UI = "Assets/ThirdParty/Audio/UI SFX Free Pack/Assets/Audio/";

        /// <summary>Folders the Sound Board offers as candidates.</summary>
        public static readonly string[] CandidateFolders =
        {
            "Assets/ThirdParty/Audio",
            "Assets/Feel/NiceVibrations/HapticSamples",
            "Assets/Feel/NiceVibrations/Demo/DemoAssets/HapticClipsDemo/Sounds",
            "Assets/Feel/FeelDemos",
        };

        /// <summary>Starting picks (written to the JSON when it does not exist yet).</summary>
        static SoundPickList Defaults() => new SoundPickList
        {
            picks = new List<SoundPick>
            {
                // Collecting slices / cups (very frequent, pitched up with the stack): a round tonal bloop.
                Pick("Pop", 0.18f, 1f, NV + "ApplicationUX/Pop1.wav"),
                // UI buttons: a crisp, short tap.
                Pick("Click", 0.12f, 1f, NV + "ApplicationUX/Button1.wav"),
                // Every bill collected: real coin clinks, four variants so a fast collection never repeats.
                Pick("Coin", 0.35f, 1f, NV + "Objects/Coins2.wav", NV + "Objects/Coins3.wav", NV + "Objects/Coins5.wav", NV + "Objects/Coins7.wav"),
                // A customer pays.
                Pick("Cash", 0.7f, 1f, NV + "Objects/Cash5.wav"),
                // Truck payout.
                Pick("BigCash", 1.8f, 1f, NV + "Objects/Cash8.wav"),
                // Unlock pad completed: sparkly rising arpeggio.
                Pick("Unlock", 0f, 1f, NV + "ApplicationUX/Award1.wav"),
                // Ad / shop reward: a chord that builds up.
                Pick("Reward", 1.4f, 0.9f, "Assets/Feel/FeelDemos/Strike/Sounds/FeelStrikeSuccess.wav"),
                // Not allowed / can't afford: three falling notes.
                Pick("Error", 0f, 0.8f, CGS + "DM-CGS-27.wav"),
                // Panels and popups.
                Pick("Whoosh", 0f, 1f, "Assets/Feel/FeelDemos/Duck/Sounds/FeelDuckWoosh.wav"),
                // Juice cup set down: glass / ice clink.
                Pick("Tink", 0.28f, 1f, NV + "SoundFX/IceCubes1.wav"),
                // Golden Apples: bells.
                Pick("Chime", 1.1f, 0.9f, "Assets/Feel/NiceVibrations/Demo/DemoAssets/HapticClipsDemo/Sounds/NVCarillon.wav"),
                // Delivery box taped shut.
                Pick("Tape", 0.6f, 1f, NV + "Objects/Velcro1.wav"),
                // Crates into the truck / boxes landing.
                Pick("Load", 0.35f, 1f, NV + "Impacts/Wood1.wav"),
                Pick("Thud", 0.35f, 1f, NV + "Impacts/Wood3.wav"),
                // Footsteps on the cobbles.
                Pick("Step", 0.2f, 0.8f, NV + "Footsteps/FootstepGravel1.wav", NV + "Footsteps/FootstepGravel2.wav", NV + "Footsteps/FootstepGravel3.wav"),
            }
        };

        static SoundPick Pick(string id, float maxLength, float gain, params string[] sources) =>
            new SoundPick { id = id, maxLength = maxLength, gain = gain, sources = new List<string>(sources) };

        public static SoundPickList Load()
        {
            if (!File.Exists(PicksPath))
            {
                var d = Defaults();
                Save(d);
                return d;
            }
            return JsonUtility.FromJson<SoundPickList>(File.ReadAllText(PicksPath)) ?? new SoundPickList();
        }

        public static void Save(SoundPickList list)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PicksPath));
            File.WriteAllText(PicksPath, JsonUtility.ToJson(list, true));
            AssetDatabase.ImportAsset(PicksPath);
        }

        [MenuItem("Juice King/Audio/Import Game Sounds")]
        public static void Import()
        {
            var list = Load();
            Directory.CreateDirectory(OutDir);
            foreach (var f in Directory.GetFiles(OutDir, "*.wav")) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            int written = 0;
            var problems = new List<string>();
            foreach (var p in list.picks)
            {
                if (!Enum.TryParse<SfxId>(p.id, out _))
                {
                    problems.Add(p.id + ": not an SfxId");
                    continue;
                }
                for (int i = 0; i < p.sources.Count; i++)
                {
                    string src = p.sources[i];
                    if (!File.Exists(src))
                    {
                        problems.Add(p.id + ": missing " + src);
                        continue;
                    }
                    try
                    {
                        var wav = Wav.Read(src);
                        var mono = Process(wav.Mono(), wav.rate, p.maxLength);
                        string name = p.id.ToLowerInvariant() + (i == 0 ? "" : "_" + (i + 1));
                        Wav.Write(Path.Combine(OutDir, name + ".wav"), mono, wav.rate);
                        written++;
                    }
                    catch (Exception e)
                    {
                        problems.Add(p.id + ": " + Path.GetFileName(src) + " " + e.Message);
                    }
                }
            }
            // The gain table travels with the clips (Sfx reads it at runtime).
            File.WriteAllText(Path.Combine(OutDir, "gains.json"), JsonUtility.ToJson(GainTable(list)));
            AssetDatabase.Refresh();
            Debug.Log("[JuiceKing] Sounds imported: " + written + " clips" + (problems.Count > 0 ? "\n- " + string.Join("\n- ", problems) : ""));
        }

        static SfxGains GainTable(SoundPickList list)
        {
            var t = new SfxGains();
            foreach (var p in list.picks)
            {
                t.ids.Add(p.id.ToLowerInvariant());
                t.gains.Add(p.gain <= 0f ? 1f : p.gain);
            }
            return t;
        }

        /// <summary>Trim leading silence, cut, fade in/out, peak-normalise to -1 dBFS.</summary>
        public static float[] Process(float[] x, int rate, float maxLength)
        {
            float peak = 0f;
            for (int i = 0; i < x.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(x[i]));
            if (peak < 1e-5f) return x;
            // Leading silence: first sample above -36 dB of the peak, minus 2 ms so the attack keeps its edge.
            float gate = peak * 0.016f;
            int start = 0;
            while (start < x.Length && Mathf.Abs(x[start]) < gate) start++;
            start = Mathf.Max(0, start - rate / 500);
            // Trailing silence.
            int end = x.Length;
            while (end > start + 1 && Mathf.Abs(x[end - 1]) < peak * 0.003f) end--;
            int len = end - start;
            bool cut = maxLength > 0f && len > (int)(maxLength * rate);
            if (cut) len = (int)(maxLength * rate);
            var y = new float[len];
            Array.Copy(x, start, y, 0, len);
            int fadeIn = Mathf.Min(len / 4, rate / 1000);
            int fadeOut = Mathf.Min(len / 3, cut ? rate * 6 / 100 : rate / 100);
            for (int i = 0; i < fadeIn; i++) y[i] *= (float)i / fadeIn;
            for (int i = 0; i < fadeOut; i++) y[len - 1 - i] *= (float)i / fadeOut;
            float g = 0.89f / peak;
            for (int i = 0; i < len; i++) y[i] *= g;
            return y;
        }

        /// <summary>Minimal RIFF/WAVE reader and writer (PCM 8/16/24/32-bit, IEEE float 32-bit).</summary>
        public class Wav
        {
            public int rate, channels;
            public float[] data;

            public float[] Mono()
            {
                if (channels == 1) return data;
                var m = new float[data.Length / channels];
                for (int i = 0; i < m.Length; i++)
                {
                    float s = 0f;
                    for (int c = 0; c < channels; c++) s += data[i * channels + c];
                    m[i] = s / channels;
                }
                return m;
            }

            public static Wav Read(string path)
            {
                var b = File.ReadAllBytes(path);
                if (b.Length < 12 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F') throw new Exception("not a RIFF file");
                int pos = 12, fmt = 0, bits = 0;
                var w = new Wav();
                while (pos + 8 <= b.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(b, pos, 4);
                    int size = BitConverter.ToInt32(b, pos + 4);
                    int body = pos + 8;
                    if (id == "fmt ")
                    {
                        fmt = BitConverter.ToUInt16(b, body);
                        w.channels = BitConverter.ToUInt16(b, body + 2);
                        w.rate = BitConverter.ToInt32(b, body + 4);
                        bits = BitConverter.ToUInt16(b, body + 14);
                        if (fmt == 0xFFFE && size >= 26) fmt = BitConverter.ToUInt16(b, body + 24);
                    }
                    else if (id == "data")
                    {
                        size = Mathf.Min(size, b.Length - body);
                        int bps = bits / 8, n = size / bps;
                        w.data = new float[n];
                        for (int i = 0; i < n; i++)
                        {
                            int o = body + i * bps;
                            w.data[i] = fmt == 3 && bits == 32 ? BitConverter.ToSingle(b, o)
                                : bits == 8 ? (b[o] - 128) / 128f
                                : bits == 16 ? BitConverter.ToInt16(b, o) / 32768f
                                : bits == 24 ? ((b[o] << 8 | b[o + 1] << 16 | b[o + 2] << 24) >> 8) / 8388608f
                                : bits == 32 ? BitConverter.ToInt32(b, o) / 2147483648f
                                : 0f;
                        }
                    }
                    pos = body + size + (size & 1);
                }
                if (w.data == null || w.channels < 1 || w.rate < 1) throw new Exception("unsupported WAV");
                return w;
            }

            public static void Write(string path, float[] x, int rate)
            {
                using (var s = new BinaryWriter(File.Create(path)))
                {
                    int bytes = x.Length * 2;
                    s.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    s.Write(36 + bytes);
                    s.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    s.Write(16);
                    s.Write((short)1);
                    s.Write((short)1);
                    s.Write(rate);
                    s.Write(rate * 2);
                    s.Write((short)2);
                    s.Write((short)16);
                    s.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    s.Write(bytes);
                    for (int i = 0; i < x.Length; i++) s.Write((short)Mathf.Clamp(Mathf.RoundToInt(x[i] * 32767f), -32768, 32767));
                }
            }
        }
    }
}
