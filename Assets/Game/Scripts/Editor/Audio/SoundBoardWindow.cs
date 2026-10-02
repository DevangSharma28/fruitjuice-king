using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// <b>Juice King ▸ Audio ▸ Sound Board</b>: listen to every game sound and choose its recorded clips.
    /// Left: each <see cref="SfxId"/> with its current pick (▶ plays the game-ready clip, "synth" the procedural one).
    /// Right: every candidate clip in <see cref="SoundLibrary.CandidateFolders"/>, searchable; "use" adds it as a variant
    /// of the selected sound. "Save + Import" writes <c>sound_picks.json</c> and rebuilds the clips (no scene rebuild needed).
    /// </summary>
    public class SoundBoardWindow : EditorWindow
    {
        SoundPickList _list;
        string _selected = "Pop";
        string _search = "";
        Vector2 _leftScroll, _rightScroll;
        List<string> _candidates;
        bool _dirty;

        [MenuItem("Juice King/Audio/Sound Board")]
        static void Open() => GetWindow<SoundBoardWindow>("Sound Board").minSize = new Vector2(820f, 420f);

        void OnEnable()
        {
            _list = SoundLibrary.Load();
            _candidates = null;
        }

        void OnDisable() => StopAll();

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_dirty ? "Unsaved changes" : "Picks: " + SoundLibrary.PicksPath, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Stop", EditorStyles.toolbarButton)) StopAll();
                if (GUILayout.Button("Revert", EditorStyles.toolbarButton)) { _list = SoundLibrary.Load(); _dirty = false; }
                if (GUILayout.Button("Save + Import", EditorStyles.toolbarButton))
                {
                    SoundLibrary.Save(_list);
                    SoundLibrary.Import();
                    _dirty = false;
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var s = new EditorGUILayout.ScrollViewScope(_leftScroll, GUILayout.Width(position.width * 0.56f)))
                {
                    _leftScroll = s.scrollPosition;
                    foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
                        if (id != SfxId.Count) DrawSound(id);
                }
                using (var s = new EditorGUILayout.ScrollViewScope(_rightScroll))
                {
                    _rightScroll = s.scrollPosition;
                    DrawCandidates();
                }
            }
        }

        void DrawSound(SfxId id)
        {
            string name = id.ToString();
            var pick = _list.picks.Find(p => p.id == name);
            bool sel = _selected == name;
            using (new EditorGUILayout.VerticalScope(sel ? "SelectionRect" : EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(sel, name, "Button", GUILayout.Width(90f)) && !sel) _selected = name;
                    if (GUILayout.Button("synth", GUILayout.Width(48f))) Play(Sfx.SynthClip(id));
                    if (pick != null && pick.sources.Count > 0)
                    {
                        if (GUILayout.Button("▶ game", GUILayout.Width(60f))) PlayGame(name, pick.sources.Count);
                        GUILayout.Label("len", GUILayout.Width(24f));
                        float ml = EditorGUILayout.FloatField(pick.maxLength, GUILayout.Width(40f));
                        GUILayout.Label("gain", GUILayout.Width(30f));
                        float g = EditorGUILayout.FloatField(pick.gain, GUILayout.Width(40f));
                        if (!Mathf.Approximately(ml, pick.maxLength) || !Mathf.Approximately(g, pick.gain))
                        {
                            pick.maxLength = Mathf.Max(0f, ml);
                            pick.gain = Mathf.Clamp(g, 0.1f, 3f);
                            _dirty = true;
                        }
                    }
                    else GUILayout.Label("(synthesised)", EditorStyles.miniLabel);
                }
                if (pick == null) return;
                for (int i = 0; i < pick.sources.Count; i++)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20f);
                        if (GUILayout.Button("▶", GUILayout.Width(24f))) Play(AssetDatabase.LoadAssetAtPath<AudioClip>(pick.sources[i]));
                        GUILayout.Label(System.IO.Path.GetFileNameWithoutExtension(pick.sources[i]), EditorStyles.miniLabel);
                        if (GUILayout.Button("✕", GUILayout.Width(22f)))
                        {
                            pick.sources.RemoveAt(i);
                            if (pick.sources.Count == 0) _list.picks.Remove(pick);
                            _dirty = true;
                            GUIUtility.ExitGUI();
                        }
                    }
            }
        }

        void DrawCandidates()
        {
            if (_candidates == null)
            {
                _candidates = new List<string>();
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", SoundLibrary.CandidateFolders))
                    _candidates.Add(AssetDatabase.GUIDToAssetPath(guid));
                _candidates.Sort((a, b) => string.Compare(System.IO.Path.GetFileName(a), System.IO.Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            }
            GUILayout.Label("Candidates → add to " + _selected, EditorStyles.boldLabel);
            _search = EditorGUILayout.TextField("Search", _search);
            foreach (var path in _candidates)
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (_search.Length > 0 && path.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("▶", GUILayout.Width(24f))) Play(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                    if (GUILayout.Button("use", GUILayout.Width(36f))) Use(path);
                    GUILayout.Label(new GUIContent(file, path), EditorStyles.miniLabel);
                }
            }
        }

        void Use(string path)
        {
            var pick = _list.picks.Find(p => p.id == _selected);
            if (pick == null)
            {
                pick = new SoundPick { id = _selected, gain = 1f };
                _list.picks.Add(pick);
            }
            if (!pick.sources.Contains(path)) pick.sources.Add(path);
            _dirty = true;
        }

        void PlayGame(string id, int variants)
        {
            int v = UnityEngine.Random.Range(0, variants);
            string file = SoundLibrary.OutDir + "/" + id.ToLowerInvariant() + (v == 0 ? "" : "_" + (v + 1)) + ".wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
            if (clip == null) Debug.Log("[Sound Board] " + id + " is not imported yet: press Save + Import");
            else Play(clip);
        }

        // ------------------------------------------------------------ preview through the Editor's internal AudioUtil

        static Type AudioUtil => typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        static void Play(AudioClip clip)
        {
            if (clip == null) return;
            StopAll();
            var m = AudioUtil?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            m?.Invoke(null, new object[] { clip, 0, false });
        }

        static void StopAll() =>
            AudioUtil?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }
}
