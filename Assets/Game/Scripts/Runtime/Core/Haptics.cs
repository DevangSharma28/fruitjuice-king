using UnityEngine;

namespace JuiceKing
{
    public enum HapticKind { Selection, Light, Medium, Heavy, Success, Warning, Failure }

    /// <summary>
    /// Short vibrations through Nice Vibrations (Assets/Feel). Rate-limited so streams of small events (coins, items)
    /// never buzz continuously, switchable in Settings, and a no-op in the Editor (its native plugin is not available
    /// there). Only meaningful moments call this: purchases, unlocks, rewards, warnings, UI presses.
    /// </summary>
    public static class Haptics
    {
        const string Key = "juiceking_haptics";
        const float MinGap = 0.07f;
        static float _last = -1f;
        static int _enabled = -1;

        public static bool Enabled
        {
            get
            {
                if (_enabled < 0) _enabled = PlayerPrefs.GetInt(Key, 1);
                return _enabled == 1;
            }
            set
            {
                _enabled = value ? 1 : 0;
                PlayerPrefs.SetInt(Key, _enabled);
                PlayerPrefs.Save();
            }
        }

        public static void Play(HapticKind kind)
        {
            if (!Enabled) return;
            float now = Time.unscaledTime;
            // Bigger moments may cut in; small ticks respect the gap.
            if (now - _last < MinGap && kind <= HapticKind.Light) return;
            _last = now;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            try
            {
                Lofelt.NiceVibrations.HapticPatterns.PlayPreset(Map(kind));
            }
            catch (System.Exception)
            {
                // Devices without haptics support: stay silent.
            }
#endif
        }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        static Lofelt.NiceVibrations.HapticPatterns.PresetType Map(HapticKind k)
        {
            switch (k)
            {
                case HapticKind.Selection: return Lofelt.NiceVibrations.HapticPatterns.PresetType.Selection;
                case HapticKind.Light: return Lofelt.NiceVibrations.HapticPatterns.PresetType.LightImpact;
                case HapticKind.Medium: return Lofelt.NiceVibrations.HapticPatterns.PresetType.MediumImpact;
                case HapticKind.Heavy: return Lofelt.NiceVibrations.HapticPatterns.PresetType.HeavyImpact;
                case HapticKind.Success: return Lofelt.NiceVibrations.HapticPatterns.PresetType.Success;
                case HapticKind.Warning: return Lofelt.NiceVibrations.HapticPatterns.PresetType.Warning;
                default: return Lofelt.NiceVibrations.HapticPatterns.PresetType.Failure;
            }
        }
#endif
    }
}
