using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Boot scene (build index 0): the loading screen built from the art in <c>Assets/Game/UI/Loading screen/</c>
    /// (key art background + game logo), with a progress bar and tips at the bottom. See <see cref="LoadingScreen"/>.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        public const string BootScenePath = "Assets/Game/Scenes/Boot.unity";
        const string LoadingArt = "Assets/Game/UI/Loading screen/";

        static readonly string[] LoadingTips =
        {
            "Hire helpers - they keep earning while you're away!",
            "Every juicer unlocks before its field, so fruit always has somewhere to go.",
            "Watch an ad for 2x CASH on every sale.",
            "Delivery trucks pay about twice the shop price. Fill them fast!",
            "Upgrade the counter to stock more cups for the queue.",
            "Max every upgrade to open the next world.",
            "Carrying fruit you can't use? Toss it in the trash bin.",
            "Customers only order flavours you can make.",
        };

        /// <summary>The user's loading art as single, un-mipped sprites (they arrive as "Multiple" sprite sheets).</summary>
        static Sprite LoadingSprite(string file, bool alpha)
        {
            string path = LoadingArt + file;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning("[JuiceKing] Missing loading art " + path);
                return null;
            }
            bool changed = imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.mipmapEnabled
                           || imp.alphaIsTransparency != alpha || imp.maxTextureSize != 2048 || imp.textureCompression != TextureImporterCompression.CompressedHQ;
            if (changed)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = alpha;
                imp.maxTextureSize = 2048;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("Juice King/Rebuild Boot (Loading) Scene", priority = 3)]
        public static void RebuildBootOnly()
        {
            BuildFont();
            BuildMaterials();
            BuildBootScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[JuiceKing] Boot rebuilt: " + BootScenePath);
        }

        public static string RunBootOnly()
        {
            RebuildBootOnly();
            return "ok";
        }

        static void BuildBootScene()
        {
            var bgSprite = LoadingSprite("starterBG.png", false);
            var logoSprite = LoadingSprite("GameLogo.png", true);
            var shadeSprite = MatLib.Spr("grad_vertical.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // A camera that only clears (the loading screen is an overlay canvas).
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black; // the screen fades in from black (after Unity's splash / a world change)
            cam.cullingMask = 0;
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            var canvasGo = new GameObject("LoadingScreen", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above the game's HUD while it fades out
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var group = canvasGo.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var root = canvasGo.transform;

            // Key art: fills the portrait screen (crops the sides, keeps the juice machines in the middle).
            var bg = UIRect("Background", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1673f, 940f));
            var bgImg = Img(bg, bgSprite, Color.white);
            bgImg.preserveAspect = false;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1673f / 940f;

            // Soft dark shade at the bottom so the bar and tip read over the art, and a lighter one behind the logo.
            var shade = UIRect("BottomShade", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3000f, 820f));
            Img(shade, shadeSprite, new Color(0.08f, 0.04f, 0.02f, 0.78f)).preserveAspect = false;
            var topShade = UIRect("TopShade", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(3000f, 700f));
            var topImg = Img(topShade, shadeSprite, new Color(0.08f, 0.04f, 0.02f, 0.35f));
            topImg.preserveAspect = false;
            topShade.localRotation = Quaternion.Euler(0f, 0f, 180f);

            var safe = Stretch("Safe", root);
            safe.gameObject.AddComponent<SafeArea>();

            // Logo, with a shine that sweeps across it (the logo's own alpha masks it).
            var logo = UIRect("Logo", safe, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(960f, 480f));
            var logoImg = Img(logo, logoSprite, Color.white);
            var logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
            logo.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var shine = UIRect("Shine", logo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 900f));
            Img(shine, _sWhite, new Color(1f, 1f, 1f, 0.38f)).preserveAspect = false;
            shine.localRotation = Quaternion.Euler(0f, 0f, -22f);
            _ = logoImg;

            // Bottom: tip, "LOADING...", bar with percentage.
            var tip = Label(UIRect("Tip", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(940f, 110f)),
                LoadingTips[0], 38f, Color.white, TextAlignmentOptions.Center, true);
            tip.textWrappingMode = TextWrappingModes.Normal;
            tip.enableAutoSizing = true;
            tip.fontSizeMin = 26f;
            tip.fontSizeMax = 38f;
            var status = Label(UIRect("Status", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 262f), new Vector2(500f, 70f)),
                "LOADING", 48f, new Color(1f, 0.9f, 0.45f), TextAlignmentOptions.Center, true);

            var bar = UIRect("Bar", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 178f), new Vector2(800f, 74f));
            Sliced(bar, _uBtnGrey ? _uBtnGrey : _sPill, 0.9f);
            var inner = Stretch("Inner", bar, 10f, 10f, 10f, 12f);
            inner.gameObject.AddComponent<RectMask2D>();
            var fillRt = Stretch("Fill", inner);
            var fill = Img(fillRt, _sWhite, new Color(1f, 0.62f, 0.12f));
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            // Lighter top half of the juice for a glossy look (follows the fill because it is filled the same way).
            var glossRt = Stretch("Gloss", fillRt, 0f, 0f, 0f, 26f);
            var gloss = Img(glossRt, _sWhite, new Color(1f, 0.86f, 0.4f, 0.8f));
            gloss.preserveAspect = false;
            gloss.type = Image.Type.Filled;
            gloss.fillMethod = Image.FillMethod.Horizontal;
            gloss.fillAmount = 0f;
            var head = UIRect("Head", fillRt, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            Img(head, _sGlow, new Color(1f, 0.97f, 0.8f, 0.9f));
            var pct = Label(Stretch("Percent", bar, 0f, 0f, 0f, 4f), "0%", 40f, Color.white, TextAlignmentOptions.Center, true);

            var ls = canvasGo.AddComponent<LoadingScreen>();
            ls.group = group;
            ls.background = bg;
            ls.logo = logo;
            ls.logoGroup = logoGroup;
            ls.logoShine = shine;
            ls.barFill = fill;
            ls.barFillGloss = gloss;
            ls.barHead = head;
            ls.barRoot = bar;
            ls.percentText = pct;
            ls.statusText = status;
            ls.tipText = tip;
            ls.tips = LoadingTips;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootScenePath);
            SetBuildScenes();
        }
    }
}
