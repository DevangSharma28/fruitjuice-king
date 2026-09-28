using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Configures the Kenney CC0 models: shared matte materials, animation import for the character rig,
    /// and the character Animator controller (locomotion + arm-carry layer).
    /// </summary>
    public static class KenneyImport
    {
        public const string Root = "Assets/Game/Art/Kenney";
        public const string AnimSource = Root + "/Characters/character-male-a.fbx";
        public const string ControllerPath = "Assets/Game/Generated/Animation/Character.controller";
        public const string MaskPath = "Assets/Game/Generated/Animation/ArmsMask.mask";

        static readonly HashSet<string> LoopClips = new HashSet<string>
        {
            "idle", "walk", "sprint", "holding-both", "holding-right", "holding-left", "static", "sit", "crouch"
        };

        public static void ConfigureModels()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { Root });
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.EndsWith(".fbx")) continue;
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                string pack = Path.GetFileName(Path.GetDirectoryName(path));
                string file = Path.GetFileNameWithoutExtension(path);
                bool isChar = file.StartsWith("character-");

                imp.importCameras = false;
                imp.importLights = false;
                imp.addCollider = false;
                imp.importBlendShapes = false;

                if (isChar)
                {
                    imp.animationType = ModelImporterAnimationType.Generic;
                    bool source = path == AnimSource;
                    imp.importAnimation = source;
                    if (source)
                    {
                        var clips = imp.defaultClipAnimations;
                        foreach (var c in clips) c.loopTime = LoopClips.Contains(c.name);
                        imp.clipAnimations = clips;
                    }
                }
                else
                {
                    imp.animationType = ModelImporterAnimationType.None;
                    imp.importAnimation = false;
                }

                // Remap embedded materials to shared matte materials (one per pack + material name).
                foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    string key = pack + "_" + m.name;
                    var shared = AssetDatabase.LoadAssetAtPath<Material>(MatLib.Dir + "Kenney/" + key + ".mat");
                    if (shared == null)
                    {
                        Color col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                        Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                        shared = MatLib.Lit("Kenney/" + key, col, 0.12f, tex);
                    }
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), shared);
                }

                imp.SaveAndReimport();
            }

            // Palette textures: no mipmaps (prevents colour bleeding between palette cells at distance).
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var tp = AssetDatabase.GUIDToAssetPath(g);
                var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
                if (ti.mipmapEnabled)
                {
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                }
            }
        }

        /// <summary>Re-tint the Nature kit's pastel/teal palette so it matches the other (saturated) kits.</summary>
        public static void TintNature()
        {
            var colors = new Dictionary<string, Color>
            {
                { "leafsGreen", new Color(0.32f, 0.68f, 0.28f) }, { "grass", new Color(0.4f, 0.74f, 0.3f) },
                { "stone", new Color(0.63f, 0.65f, 0.7f) }, { "stoneDark", new Color(0.5f, 0.52f, 0.57f) },
                { "dirt", new Color(0.6f, 0.42f, 0.27f) }, { "dirtDark", new Color(0.5f, 0.34f, 0.22f) },
                { "wood", new Color(0.66f, 0.45f, 0.28f) }, { "woodBark", new Color(0.55f, 0.37f, 0.24f) },
                { "woodDark", new Color(0.45f, 0.3f, 0.2f) }, { "woodInner", new Color(0.92f, 0.8f, 0.6f) },
                { "colorRed", new Color(0.97f, 0.32f, 0.38f) }, { "colorYellow", new Color(1f, 0.83f, 0.2f) },
                { "colorPurple", new Color(0.72f, 0.45f, 0.97f) }
            };
            foreach (var kv in colors)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(MatLib.Dir + "Kenney/Nature_" + kv.Key + ".mat");
                if (m == null) continue;
                m.SetColor("_BaseColor", kv.Value);
                EditorUtility.SetDirty(m);
            }
        }

        public static Dictionary<string, AnimationClip> Clips()
        {
            return AssetDatabase.LoadAllAssetsAtPath(AnimSource).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview"))
                .ToDictionary(c => c.name, c => c);
        }

        public static AnimatorController BuildController()
        {
            var clips = Clips();
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            AssetDatabase.DeleteAsset(ControllerPath);
            AssetDatabase.DeleteAsset(MaskPath);

            // Arms-only mask built from the actual rig hierarchy.
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AnimSource);
            var mask = new AvatarMask();
            var transforms = model.GetComponentsInChildren<Transform>(true);
            mask.transformCount = transforms.Length;
            for (int i = 0; i < transforms.Length; i++)
            {
                string p = AnimationUtility.CalculateTransformPath(transforms[i], model.transform);
                mask.SetTransformPath(i, p);
                mask.SetTransformActive(i, p.Contains("arm-left") || p.Contains("arm-right"));
            }
            AssetDatabase.CreateAsset(mask, MaskPath);

            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Hold", AnimatorControllerParameterType.Int);

            var loco = ac.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips["idle"], 0f);
            tree.AddChild(clips["walk"], 1.8f);
            tree.AddChild(clips["sprint"], 4.4f);
            ac.layers[0].stateMachine.defaultState = loco;

            var sm = new AnimatorStateMachine { name = "Arms", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(sm, ac);
            var both = sm.AddState("HoldBoth");
            both.motion = clips["holding-both"];
            var right = sm.AddState("HoldRight");
            right.motion = clips["holding-right"];
            sm.defaultState = both;

            var t1 = sm.AddAnyStateTransition(right);
            t1.AddCondition(AnimatorConditionMode.Equals, 1, "Hold");
            t1.hasExitTime = false;
            t1.duration = 0.1f;
            t1.canTransitionToSelf = false;

            var t2 = sm.AddAnyStateTransition(both);
            t2.AddCondition(AnimatorConditionMode.Equals, 2, "Hold");
            t2.hasExitTime = false;
            t2.duration = 0.1f;
            t2.canTransitionToSelf = false;

            ac.AddLayer(new AnimatorControllerLayer
            {
                name = "Arms",
                defaultWeight = 0f,
                avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = sm
            });

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            return ac;
        }
    }
}
