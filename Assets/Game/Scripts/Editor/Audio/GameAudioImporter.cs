using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Mobile import settings for the game's sound effects (<see cref="SoundLibrary.OutDir"/>): mono, decompressed on
    /// load (no decode cost when a sound fires, and <c>GetData</c> works for the loudness match), Vorbis on disk,
    /// 44.1 kHz kept for crisp transients. Short clips this small cost a few hundred KB of RAM in total.
    /// </summary>
    public class GameAudioImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(SoundLibrary.OutDir)) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            s.preloadAudioData = true;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = s;
        }
    }
}
