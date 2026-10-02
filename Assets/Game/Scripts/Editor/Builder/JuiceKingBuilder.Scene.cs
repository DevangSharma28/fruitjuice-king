using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace JuiceKing.EditorTools
{
    public static partial class JuiceKingBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/JuiceKing.unity";

        static readonly float[] FruitScale = { 8.8f, 4.1f, 4.9f };
        static readonly float[] FruitRadius = { 0.75f, 0.95f, 0.62f };
        static readonly float[] HpBarY = { 1.95f, 2.35f, 2.75f };

        static Material _mPadIn, _mPadOut, _mPadCounter, _mPadCash, _mPadUpgrade, _mPadUnlock, _mLeaf, _mSoilDark, _mYellow, _mCurb, _mStraw;
        static System.Random _rnd;

        static Transform _world, _stations, _unlocks, _decor, _actors;
        static TrashBin _trash;
        static readonly List<UnlockZone> _zones = new List<UnlockZone>();

        // ================================================================== entry points

        [MenuItem("Juice King/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            ArtGen.GenerateAll();
            UIKit.BuildAll();
            AssetDatabase.Refresh();
            KenneyImport.ConfigureModels();
            _controller = KenneyImport.BuildController();
            KenneyImport.TintNature();
            BuildFont();
            BuildMeshes();
            BuildMaterials();
            BuildItemPrefabs();
            BuildCustomerPrefabs();
            BuildScene();
            BuildTropicalScene();
            BuildBerryScene();
            BuildBootScene();
            ConfigureProject();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BootScenePath);
            Debug.Log("[JuiceKing] Build complete: " + ScenePath);
        }

        [MenuItem("Juice King/Rebuild Scene (skip art + import)", priority = 1)]
        public static void RebuildSceneOnly()
        {
            _controller = KenneyImport.BuildController();
            KenneyImport.TintNature();
            BuildFont();
            BuildMeshes();
            BuildMaterials();
            BuildItemPrefabs();
            BuildCustomerPrefabs();
            BuildScene();
            BuildTropicalScene();
            BuildBerryScene();
            BuildBootScene();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BootScenePath);
            Debug.Log("[JuiceKing] Scene rebuilt: " + ScenePath);
        }

        [MenuItem("Juice King/Rebuild Tropical Scene", priority = 2)]
        public static void RebuildTropicalOnly()
        {
            _controller = KenneyImport.BuildController();
            KenneyImport.TintNature();
            BuildFont();
            BuildMeshes();
            BuildMaterials();
            BuildItemPrefabs();
            BuildCustomerPrefabs();
            BuildTropicalScene();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[JuiceKing] Tropical rebuilt: " + TropicalScenePath);
        }

        public static string RunTropicalOnly()
        {
            RebuildTropicalOnly();
            return "ok";
        }

        [MenuItem("Juice King/Rebuild Berry Scene", priority = 3)]
        public static void RebuildBerryOnly()
        {
            _controller = KenneyImport.BuildController();
            KenneyImport.TintNature();
            BuildFont();
            BuildMeshes();
            BuildMaterials();
            BuildItemPrefabs();
            BuildCustomerPrefabs();
            BuildBerryScene();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[JuiceKing] Berry rebuilt: " + BerryScenePath);
        }

        public static string RunBerryOnly()
        {
            RebuildBerryOnly();
            return "ok";
        }

        /// <summary>Golden Apple, fox and cake art every world's GameRefs carries (premium currency is global).</summary>
        static void AssignPremiumRefs(GameRefs refs)
        {
            refs.appleIcon = _sApple;
            refs.foxIcon = _sFox;
            refs.cakeIcons = (Sprite[])_sCakeIcons.Clone();
            refs.batterPrefabs = _batterPrefabs;
            refs.cakePrefabs = _cakePrefabs;
        }

        /// <summary>Build order: the Boot loading screen first, then the worlds (loaded by name).</summary>
        static void SetBuildScenes()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (System.IO.File.Exists(BootScenePath)) list.Add(new EditorBuildSettingsScene(BootScenePath, true));
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            list.Add(new EditorBuildSettingsScene(TropicalScenePath, true));
            if (System.IO.File.Exists(BerryScenePath)) list.Add(new EditorBuildSettingsScene(BerryScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        [MenuItem("Juice King/Reset Save Data", priority = 20)]
        public static void ResetSave()
        {
            // The PlayerPrefs save, the debug world slots and the backup file (persistentDataPath).
            GameManager.DeleteSaveFiles();
            Debug.Log("[JuiceKing] Save data cleared");
        }

        /// <summary>Entry for `unity command run_script`.</summary>
        public static string Run()
        {
            BuildEverything();
            return "ok";
        }

        public static string RunSceneOnly()
        {
            RebuildSceneOnly();
            return "ok";
        }

        // ================================================================== scene

        static void BuildScene()
        {
            _rnd = new System.Random(1234);
            _zones.Clear();

            BuildStationMaterials();
            _mPadIn = PadMat("In", new Color(1f, 0.78f, 0.2f, 1f));
            _mPadOut = PadMat("Out", new Color(0.4f, 0.95f, 0.45f, 1f));
            _mPadCounter = PadMat("Counter", new Color(0.4f, 0.75f, 1f, 1f));
            _mPadCash = PadMat("Cash", new Color(0.5f, 1f, 0.5f, 1f));
            _mPadUpgrade = PadMat("Upgrade", new Color(0.8f, 0.55f, 1f, 1f));
            _mPadUnlock = MatLib.Sprite("Pad_Unlock", MatLib.Tex("pad_dashed.png"), new Color(1f, 1f, 1f, 1f));
            _mLeaf = MatLib.Lit("Leaf", new Color(0.3f, 0.66f, 0.26f), 0.2f);
            _mSoilDark = MatLib.Lit("SoilDark", new Color(0.45f, 0.3f, 0.18f), 0.05f);
            _mYellow = MatLib.Lit("GuideYellow", new Color(1f, 0.85f, 0.1f), 0.4f);
            _mCurb = MatLib.Lit("Curb", new Color(0.78f, 0.78f, 0.76f), 0.1f);
            _mStraw = MatLib.Lit("StrawHat", new Color(0.95f, 0.8f, 0.4f), 0.1f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = B.Node("Systems", null, Vector3.zero);
            _world = B.Node("World", null, Vector3.zero).transform;
            _stations = B.Node("Stations", _world, Vector3.zero).transform;
            _unlocks = B.Node("Unlocks", null, Vector3.zero).transform;
            _decor = B.Node("Decor", _world, Vector3.zero).transform;
            _actors = B.Node("Actors", null, Vector3.zero).transform;

            var gm = systems.AddComponent<GameManager>();
            gm.sceneExpansion = 0;
            var refs = systems.AddComponent<GameRefs>();
            systems.AddComponent<LooseItems>();
            systems.AddComponent<Boosts>();
            _ambient = systems.AddComponent<Ambient>();
            _nature = B.Node("Nature", _world, Vector3.zero).transform;
            BuildEnvMeshes();

            BuildLighting();
            BuildGround();

            // ---------------- fields
            var orangeField = BuildField(0, new Vector3(-6.5f, 0f, 7.1f), new Vector2(5.8f, 5.6f),
                new[] { new Vector2(-7.8f, 5.8f), new Vector2(-5.2f, 5.8f), new Vector2(-7.8f, 8.4f), new Vector2(-5.2f, 8.4f) }, "OrangeField");
            var orangeMore = BuildFieldExpansion(0, orangeField, new Vector3(-6.5f, 0f, 11.3f), new Vector2(5.8f, 2.8f),
                new[] { new Vector2(-7.8f, 11.0f), new Vector2(-5.2f, 11.0f) });

            var melonField = BuildField(1, new Vector3(6.5f, 0f, 7.2f), new Vector2(5.8f, 5.8f),
                new[] { new Vector2(5.1f, 5.8f), new Vector2(7.9f, 5.8f), new Vector2(5.1f, 8.6f), new Vector2(7.9f, 8.6f) }, "WatermelonField");
            var melonMore = BuildFieldExpansion(1, melonField, new Vector3(6.5f, 0f, 11.6f), new Vector2(5.8f, 3f),
                new[] { new Vector2(5.1f, 11.4f), new Vector2(7.9f, 11.4f) });

            var pineField = BuildField(2, new Vector3(0f, 0f, 15.2f), new Vector2(8.4f, 5.6f),
                new[] { new Vector2(-2.6f, 14.0f), new Vector2(0f, 14.0f), new Vector2(2.6f, 14.0f), new Vector2(-2.6f, 16.4f), new Vector2(0f, 16.4f), new Vector2(2.6f, 16.4f) }, "PineappleField");

            // Fences around the fields (colliders keep players and helpers on the paths).
            FenceLine(new Vector3(-9.8f, 0, 4.2f), new Vector3(-9.8f, 0, 13.1f), orangeField.transform.parent);
            FenceLine(new Vector3(-9.8f, 0, 13.1f), new Vector3(-3.2f, 0, 13.1f), orangeField.transform.parent);
            FenceLine(new Vector3(9.8f, 0, 4.2f), new Vector3(9.8f, 0, 13.4f), melonField.transform.parent);
            FenceLine(new Vector3(3.2f, 0, 13.4f), new Vector3(9.8f, 0, 13.4f), melonField.transform.parent);
            FenceLine(new Vector3(-4.6f, 0, 18.4f), new Vector3(4.6f, 0, 18.4f), pineField.transform.parent);

            // ---------------- juicers
            var orangeJuicer = BuildJuicer(0, new Vector3(-4.6f, 0f, -0.8f));
            var melonJuicer = BuildJuicer(1, new Vector3(4.6f, 0f, -0.8f));
            var pineJuicer = BuildJuicer(2, new Vector3(0f, 0f, 3.4f));

            // ---------------- counter, cash, upgrades
            var counter = BuildCounter(new Vector3(0f, 0f, -7f));
            var cash = BuildCashPile(new Vector3(4.3f, 0f, -6.3f));
            var upgradeStation = BuildUpgradeStation(new Vector3(-8.4f, 0f, -5f));
            var patio = BuildPatio(new Vector3(-7.6f, 0f, -10.2f));

            // ---------------- customers
            var cm = BuildCustomers(systems.transform, counter, cash, Vector3.zero);
            _trash = BuildTrashBin(new Vector3(-9.8f, 0f, -1.2f), new Vector3(-8.0f, 0f, -1.2f));

            // ---------------- player & helpers
            var player = BuildPlayerObject();
            player.transform.SetParent(_actors, false);
            player.transform.position = new Vector3(0f, 0f, -3f);
            SetLayerRecursive(player, 2);

            var waiter = BuildWaiter(new Vector3(7.6f, 0f, -4.2f), new[] { orangeJuicer, melonJuicer, pineJuicer }, counter, cm);
            var farmerOrange = BuildFarmer(0, orangeField, orangeJuicer, new Vector3(-10.6f, 0f, 2.2f), "character-male-d");
            var farmerMelon = BuildFarmer(1, melonField, melonJuicer, new Vector3(10.6f, 0f, 2.2f), "character-male-f");
            var farmerPine = BuildFarmer(2, pineField, pineJuicer, new Vector3(5.4f, 0f, 16.6f), "character-female-d");

            // ---------------- unlock chain
            var zFarmerPine = Unlock("farmer_pine", "Pineapple Farmer", 700, new Vector3(5.4f, 0f, 16.6f), _uFarmer, new[] { farmerPine });
            var zFarmerMelon = Unlock("farmer_melon", "Melon Farmer", 550, new Vector3(10.6f, 0f, 2.2f), _uFarmer, new[] { farmerMelon }, zFarmerPine);
            var zFarmerOrange = Unlock("farmer_orange", "Orange Farmer", 400, new Vector3(-10.6f, 0f, 2.2f), _uFarmer, new[] { farmerOrange }, zFarmerMelon);
            // Each juicer unlocks before its field, so harvested fruit always has somewhere to go.
            var zPineField = Unlock("pine_field", "Pineapple Field", 350, new Vector3(0f, 0f, 15.2f), _sFruit[2], new[] { pineField.transform.parent.gameObject }, zFarmerOrange);
            var zPineJuicer = Unlock("pine_juicer", "Pineapple Juicer", 250, pineJuicer.transform.position, _uMachine, new[] { pineJuicer.gameObject }, zPineField);
            var zPatio = Unlock("patio", "Cozy Patio", 150, patio.transform.position, _uStar, new[] { patio });
            zPatio.subtitle = "Charm: customers pay +10%";
            var zWaiter = Unlock("hire_waiter", "Hire Waiter", 180, new Vector3(7.6f, 0f, -4.2f), _uWaiter, new[] { waiter }, zPineJuicer, zPatio);
            var zMelonMore = Unlock("melon_more", "More Melons", 120, new Vector3(6.5f, 0f, 11.6f), _sFruit[1], new[] { melonMore });
            var zMelonField = Unlock("melon_field", "Watermelon Field", 100, new Vector3(6.5f, 0f, 7.2f), _sFruit[1], new[] { melonField.transform.parent.gameObject }, zWaiter, zMelonMore);
            var zMelonJuicer = Unlock("melon_juicer", "Melon Juicer", 60, melonJuicer.transform.position, _uMachine, new[] { melonJuicer.gameObject }, zMelonField);
            var zUpgrades = Unlock("upgrades", "Upgrade Shop", 40, new Vector3(-8.4f, 0f, -5f), _uTools, new[] { upgradeStation });
            var zOrangeMore = Unlock("orange_more", "More Oranges", 20, new Vector3(-6.5f, 0f, 11.3f), _sFruit[0], new[] { orangeMore }, zUpgrades, zMelonJuicer);
            zOrangeMore.startVisible = true;

            var um = systems.AddComponent<UnlockManager>();
            um.zones = _zones.ToArray();

            // ---------------- navmesh
            var surface = _world.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(1 << 2);
            var nb = systems.AddComponent<NavBaker>();
            nb.surface = surface;

            // ---------------- camera, ui, guide
            var cam = BuildCamera(player.transform);
            BuildUI(systems.transform, out var hud, 0);
            var em = systems.AddComponent<ExpansionManager>();
            em.nextExpansion = 1;
            em.completionPopup = _uiCompletion;
            em.nextWorldButton = _uiNextWorld;

            var tut = systems.AddComponent<Tutorial>();
            tut.firstField = orangeField;
            tut.firstJuicer = orangeJuicer;
            tut.counter = counter;
            tut.cash = cash;
            tut.upgradeZone = upgradeStation.GetComponentInChildren<UpgradeZone>(true);
            var arrowMat = Emissive("GuideGlow", new Color(1f, 0.82f, 0.1f), 1.5f);
            var outlineMat = MatLib.Lit("GuideOutline", new Color(0.3f, 0.16f, 0.04f), 0f);
            outlineMat.SetFloat("_Cull", 1f); // front-face culled shell = cartoon outline
            UnityEditor.EditorUtility.SetDirty(outlineMat);
            var arrow = B.MeshObj("GuideArrow", _actors, _arrow, arrowMat, new Vector3(0, 3, 0), Vector3.one * 1.35f, null, false);
            B.MeshObj("Outline", arrow.transform, _arrow, outlineMat, new Vector3(0f, -0.07f, 0f), Vector3.one * 1.13f, null, false);
            tut.arrow = arrow.transform;
            var pointer = B.Node("GuidePointer", _actors, Vector3.zero);
            B.Decal("Chevron", pointer.transform, _mPointer, new Vector3(0f, 0f, 0.35f), new Vector2(1.5f, 1.5f)).GetComponent<MeshRenderer>().sortingOrder = 6;
            tut.pointer = pointer.transform;
            tut.edgeArrow = _edgeArrow;
            tut.edgeArea = _edgeArea;

            // ---------------- refs
            refs.slicePrefabs = _slicePrefabs;
            refs.juicePrefabs = _juicePrefabs;
            refs.moneyPrefab = _moneyPrefab;
            refs.fruitIcons = _sFruit;
            refs.juiceIcon = _sJuice;
            refs.juiceIcons = (Sprite[])_sJuiceIcons.Clone();
            UpgradeIconTable(out refs.upgradeIconKeys, out refs.upgradeIconSprites);
            refs.moneyIcon = _sMoney;
            AssignPremiumRefs(refs);
            refs.particleMaterial = _mParticle;
            refs.fxMaterials = _mFx;
            refs.font = B.Font;
            refs.floatingTextPrefab = _floatingText;
            refs.customerPrefabs = _customerPrefabs.ToArray();
            refs.mainCamera = cam;
            refs.player = player.GetComponent<Carrier>();
            refs.counter = counter;
            refs.cashPile = cash;

            BuildDecor();
            BuildPlazaDressing();
            BuildPond(new Vector3(-8.0f, 0f, 16.3f), 2.3f);
            BuildWindmill(new Vector3(-16.5f, 0f, 9f));
            BuildCoop(new Vector3(10.2f, 0f, 16.6f));
            BuildCritters();
            BuildButterflies();
            BuildClouds();
            BuildGroundCover();
            PolishClassic();
            OptimizeScene("JuiceKing");
            // Static-batch the scenery; animated decor (swayers, spinners, critters) is un-flagged below.
            MarkStatic(_decor.gameObject);
            foreach (var n in new[] { "Trees", "Plants", "GroundCover" })
            {
                var t = _nature.Find(n);
                if (t != null) MarkStatic(t.gameObject);
            }
            foreach (var w in _decor.GetComponentsInChildren<Wanderer>(true)) ClearStatic(w.gameObject);
            foreach (var n in new[] { "Butterflies", "CloudShadows", "Pond/Water" })
            {
                var t = _decor.Find(n);
                if (t != null) ClearStatic(t.gameObject);
            }
            foreach (var sw in _ambient.swayers) if (sw.t != null) ClearStatic(sw.t.gameObject);
            foreach (var sp in _ambient.spinners) if (sp.t != null) ClearStatic(sp.t.gameObject);
            var water = _decor.Find("Pond/Water");
            if (water != null) _ambient.water = new[] { water.GetComponent<Renderer>() };
            // Grass tufts no longer sway, so they can be static-batched with the rest of the ground cover.
            var cover = _nature.Find("GroundCover");
            if (cover != null) MarkStatic(cover.gameObject);
            // Decor layers get per-layer cull distances on the camera (see CameraFollow): big props far, small props near.
            SetLayerRecursive(_decor.gameObject, CameraFollow.BigDecorLayer);
            SetLayerRecursive(_nature.gameObject, CameraFollow.BigDecorLayer);
            foreach (var n in new[] { "GroundCover", "Plants" })
            {
                var t = _nature.Find(n);
                if (t != null) SetLayerRecursive(t.gameObject, CameraFollow.SmallDecorLayer);
            }
            var flies = _decor.Find("Butterflies");
            if (flies != null) SetLayerRecursive(flies.gameObject, CameraFollow.SmallDecorLayer);
            foreach (var n in new[] { "Grass", "Plaza", "Path", "Sidewalk", "Curb", "Road", "CurbFar" })
            {
                var g = _world.Find(n);
                if (g != null) MarkStatic(g.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
        }

        static void MarkStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void ClearStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        // ================================================================== lighting, ground, camera

        static void BuildLighting(bool tropical = false, bool berry = false)
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = berry ? new Color(1f, 0.9f, 0.8f) : tropical ? new Color(1f, 0.91f, 0.76f) : new Color(1f, 0.93f, 0.8f);
            l.intensity = berry ? 1.35f : tropical ? 1.4f : 1.3f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.68f;
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.8f, 0.87f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.76f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.45f, 0.38f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = tropical ? new Color(0.62f, 0.86f, 0.96f) : new Color(0.7f, 0.87f, 0.98f);
            RenderSettings.fogStartDistance = tropical ? 48f : 40f;
            RenderSettings.fogEndDistance = tropical ? 105f : 78f;
            if (tropical)
            {
                RenderSettings.ambientSkyColor = new Color(0.78f, 0.9f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.78f, 0.8f, 0.68f);
                RenderSettings.ambientGroundColor = new Color(0.62f, 0.55f, 0.42f);
            }
            if (berry)
            {
                // Soft afternoon light with a hint of pink in the shade and a lavender haze in the distance.
                sun.transform.rotation = Quaternion.Euler(50f, -38f, 0f);
                // Bright and fresh: greener bounce light, and the haze only far away so the meadow keeps its colour.
                RenderSettings.ambientSkyColor = new Color(0.86f, 0.9f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.84f, 0.88f, 0.76f);
                RenderSettings.ambientGroundColor = new Color(0.62f, 0.58f, 0.46f);
                RenderSettings.fogColor = new Color(0.86f, 0.88f, 0.96f);
                RenderSettings.fogStartDistance = 58f;
                RenderSettings.fogEndDistance = 120f;
            }
            RenderSettings.sun = l;

            // Post processing.
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(berry ? 0.22f : 0.02f);
            ca.contrast.Override(berry ? 8f : 14f);
            ca.saturation.Override(berry ? 36f : tropical ? 30f : 26f);
            // Cool, slightly lifted shade and warm highlights: the soft storybook look.
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.96f, 0.97f, 1.08f, 0.02f));
            smh.highlights.Override(new Vector4(1.04f, 1.01f, 0.95f, 0f));
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(berry ? 5f : tropical ? 9f : 6f);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.6f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(berry ? 0.14f : 0.2f);
            vig.smoothness.Override(0.5f);
            string profilePath = berry ? "Assets/Game/Generated/PostFX_Berry.asset" : tropical ? "Assets/Game/Generated/PostFX_Tropical.asset" : "Assets/Game/Generated/PostFX.asset";
            AssetDatabase.DeleteAsset(profilePath);
            AssetDatabase.CreateAsset(profile, profilePath);
            foreach (var comp in profile.components) AssetDatabase.AddObjectToAsset(comp, profile);
            AssetDatabase.SaveAssets();

            var vol = new GameObject("PostFX").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        static void BuildGround()
        {
            var ground = B.Box("Grass", _world, _mGrass, new Vector3(0f, -0.05f, 3f), new Vector3(110f, 0.1f, 120f), null, false);
            ground.AddComponent<BoxCollider>();
            ground.isStatic = true;

            B.Box("Plaza", _world, _mTiles, new Vector3(0f, 0.01f, -4.6f), new Vector3(24f, 0.02f, 16.4f), null, false);
            // path to the pineapple field
            var path = B.Box("Path", _world, _mTiles, new Vector3(0f, 0.01f, 7.25f), new Vector3(4.4f, 0.02f, 7.3f), null, false);
            path.GetComponent<MeshRenderer>().sharedMaterial = MatLib.Lit("Ground_Tiles_Path", Color.white, 0.12f, MatLib.Tex("ground_tiles.png"), new Vector2(1f, 1.7f));

            B.Box("Sidewalk", _world, _mSidewalk, new Vector3(0f, 0.03f, -14.1f), new Vector3(110f, 0.06f, 2.6f), null, false);
            B.Box("Curb", _world, _mCurb, new Vector3(0f, 0.07f, -15.45f), new Vector3(110f, 0.14f, 0.25f), null, false);
            B.Box("Road", _world, _mRoad, new Vector3(0f, 0.01f, -18.2f), new Vector3(110f, 0.02f, 5.4f), null, false);
            var dashMat = MatLib.Lit("RoadDash", new Color(0.98f, 0.95f, 0.85f), 0.1f);
            for (float x = -52f; x <= 52f; x += 4f)
                B.Box("Dash", _world, dashMat, new Vector3(x, 0.025f, -18.2f), new Vector3(2f, 0.01f, 0.22f), null, false);
            B.Box("CurbFar", _world, _mCurb, new Vector3(0f, 0.07f, -20.95f), new Vector3(110f, 0.14f, 0.25f), null, false);

            // Invisible bounds for the player.
            var bounds = B.Node("Bounds", _world, Vector3.zero).transform;
            Wall(bounds, new Vector3(-12.8f, 1f, 3f), new Vector3(0.5f, 2f, 34f));
            Wall(bounds, new Vector3(12.8f, 1f, 3f), new Vector3(0.5f, 2f, 34f));
            Wall(bounds, new Vector3(0f, 1f, 19.6f), new Vector3(26f, 2f, 0.5f));
            Wall(bounds, new Vector3(0f, 1f, -12.9f), new Vector3(26f, 2f, 0.5f));
        }

        static void Wall(Transform parent, Vector3 pos, Vector3 size)
        {
            var w = B.Node("Wall", parent, pos);
            var bc = w.AddComponent<BoxCollider>();
            bc.size = size;
        }

        static Camera BuildCamera(Transform target, Color? background = null, float far = 120f)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background ?? new Color(0.7f, 0.87f, 0.98f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = far;
            go.AddComponent<AudioListener>();
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var follow = go.AddComponent<CameraFollow>();
            follow.target = target;
            follow.offset = new Vector3(0f, 15.6f, -13.8f);
            follow.portraitFov = 52f;
            follow.landscapeFov = 40f;
            follow.lookAhead = 1.4f;
            go.transform.position = target.position + follow.offset;
            go.transform.rotation = Quaternion.LookRotation(-follow.offset.normalized);
            return cam;
        }

        // ================================================================== fields

        /// <summary>Low wooden frame around a soil bed (visual only).</summary>
        static void BedFrame(Transform parent, Vector3 center, Vector2 size, bool south = true)
        {
            const float h = 0.14f, w = 0.14f;
            B.Box("FrameN", parent, _mWood, center + new Vector3(0f, h * 0.5f, size.y * 0.5f), new Vector3(size.x + w, h, w), null, false);
            if (south) B.Box("FrameS", parent, _mWood, center + new Vector3(0f, h * 0.5f, -size.y * 0.5f), new Vector3(size.x + w, h, w), null, false);
            B.Box("FrameE", parent, _mWood, center + new Vector3(size.x * 0.5f, h * 0.5f, 0f), new Vector3(w, h, size.y), null, false);
            B.Box("FrameW", parent, _mWood, center + new Vector3(-size.x * 0.5f, h * 0.5f, 0f), new Vector3(w, h, size.y), null, false);
        }

        static FruitField BuildField(int f, Vector3 center, Vector2 size, Vector2[] spots, string name)
        {
            var root = B.Node(name, _stations, Vector3.zero);
            var soil = B.Box("Soil", root.transform, _mSoil, center + new Vector3(0f, 0.025f, 0f), new Vector3(size.x, 0.05f, size.y), null, false);
            soil.GetComponent<MeshRenderer>().sharedMaterial = MatLib.Lit("Ground_Soil_" + f, Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(size.x / 3f, size.y / 3f));
            BedFrame(root.transform, center, size);

            var fieldGo = B.Node("Field", root.transform, center);
            var field = fieldGo.AddComponent<FruitField>();
            field.kind = (FruitKind)f;
            field.idlePoint = B.Node("Idle", fieldGo.transform, new Vector3(0f, 0f, -size.y * 0.5f - 0.6f)).transform;
            foreach (var s in spots) BuildFruit(f, field, root.transform, new Vector3(s.x, 0f, s.y));
            return field;
        }

        static GameObject BuildFieldExpansion(int f, FruitField field, Vector3 center, Vector2 size, Vector2[] spots)
        {
            var root = B.Node(FruitKey[f] + "Expansion", field.transform.parent, Vector3.zero);
            var soil = B.Box("Soil", root.transform, _mSoil, center + new Vector3(0f, 0.025f, 0f), new Vector3(size.x, 0.05f, size.y), null, false);
            soil.GetComponent<MeshRenderer>().sharedMaterial = MatLib.Lit("Ground_Soil_X" + f, Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(size.x / 3f, size.y / 3f));
            BedFrame(root.transform, center, size, false);
            foreach (var s in spots) BuildFruit(f, field, root.transform, new Vector3(s.x, 0f, s.y));
            return root;
        }

        static FruitNode BuildFruit(int f, FruitField field, Transform parent, Vector3 pos)
        {
            var go = B.Node(FruitKey[f] + "Plant", parent, pos);
            float r = FruitRadius[f];
            B.MeshObj("Mound", go.transform, _disc, new[] { _mSoilDark, _mSoilDark }, new Vector3(0f, 0f, 0f), new Vector3(r * 2.3f, 0.1f, r * 2.3f), null, false);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f + (float)_rnd.NextDouble() * 40f;
                var leaf = B.MeshObj("Leaf", go.transform, B.Sphere, _mLeaf, Vector3.zero, new Vector3(0.45f, 0.08f, r * 1.5f), new Vector3(0f, a, 0f));
                leaf.transform.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.08f, r * 0.75f);
            }

            var vis = B.Node("Visual", go.transform, Vector3.zero);
            B.Model(FruitModel[f], vis.transform, Vector3.zero, FruitScale[f], (float)_rnd.NextDouble() * 360f, "Fruit");

            var node = go.AddComponent<FruitNode>();
            node.kind = (FruitKind)f;
            node.visual = vis.transform;
            node.radius = r;
            node.field = field;

            var hp = B.Node("HP", go.transform, new Vector3(0f, HpBarY[f], 0f), null, new Vector3(1.2f, 0.16f, 1f));
            hp.AddComponent<Billboard>();
            B.Sprite("Bg", hp.transform, _sWhite, Vector3.zero, 1f, false, 20, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            var fill = B.Sprite("Fill", hp.transform, _sWhite, new Vector3(0f, 0f, -0.001f), 1f, false, 21, new Color(0.45f, 1f, 0.35f));
            node.hpBar = hp.transform;
            node.hpFill = fill.transform;

            var col = go.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, r * 0.8f, 0f);
            col.radius = r * 0.85f;
            node.blocker = col;
            return node;
        }

        static void FenceLine(Vector3 a, Vector3 b, Transform parent)
        {
            const float seg = 1.1f;
            Vector3 d = b - a;
            float len = d.magnitude;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / seg));
            var line = B.Node("Fence", parent, Vector3.zero).transform;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg - 90f;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = a + d * ((i + 0.5f) / n);
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var m = B.Prop("Survival/fence", line, p, 2.2f, yaw);
                m.transform.localScale = new Vector3((len / n) / seg, 1f, 1f);
            }
            var col = B.Node("Collider", line, (a + b) * 0.5f + Vector3.up * 0.6f, new Vector3(0f, yaw + 90f, 0f));
            col.AddComponent<BoxCollider>().size = new Vector3(0.3f, 1.2f, len);
        }

        // ================================================================== stations

        static CashPile BuildCashPile(Vector3 pos)
        {
            var root = B.Node("CashPile", _stations, pos);
            B.Box("Pallet", root.transform, _mWood, new Vector3(0f, 0.06f, 0f), new Vector3(1.9f, 0.12f, 1.1f));
            var pile = B.Node("Pile", root.transform, new Vector3(0f, 0.12f, 0f)).AddComponent<ItemPile>();
            pile.columns = 3;
            pile.rows = 3;
            pile.layers = 25;
            pile.spacing = new Vector2(0.56f, 0.3f);
            pile.layerHeight = 0.065f;
            var cp = root.AddComponent<CashPile>();
            cp.pile = pile;
            var zone = MakeZone<CashZone>("CashZone", root.transform, pos, new Vector2(2.8f, 2.2f), _mPadCash, null);
            zone.cash = cp;
            cp.zone = zone;
            return cp;
        }

        static GameObject BuildUpgradeStation(Vector3 zonePos)
        {
            var root = B.Node("UpgradeStation", _stations, zonePos);
            B.Prop("Survival/workbench", root.transform, new Vector3(0f, 0f, -1.8f), 3.4f, 180f);
            B.Prop("Survival/tool-axe", root.transform, new Vector3(-1.1f, 0f, -1.9f), 2.2f, 30f);
            B.Prop("Survival/barrel", root.transform, new Vector3(1.3f, 0f, -1.9f), 2.4f, 0f);
            var label = B.Node("Label", root.transform, new Vector3(0f, 1.9f, -2.3f));
            label.AddComponent<Billboard>();
            B.Text("Text", label.transform, "UPGRADES", 5f, new Color(0.95f, 0.8f, 1f), Vector3.zero);
            var col = B.Node("Collider", root.transform, new Vector3(0f, 0.6f, -1.85f));
            col.AddComponent<BoxCollider>().size = new Vector3(3.6f, 1.2f, 1.2f);
            MakeZone<UpgradeZone>("UpgradeZone", root.transform, zonePos, new Vector2(2.3f, 1.8f), _mPadUpgrade, _sSaw);
            return root;
        }

        static GameObject BuildPatio(Vector3 pos)
        {
            var root = B.Node("Patio", _decor, pos);
            var t = root.transform;
            B.MeshObj("Rug", t, _disc, new[] { MatLib.Lit("PatioRug", new Color(0.95f, 0.55f, 0.35f), 0.05f), MatLib.Lit("PatioRug", new Color(0.95f, 0.55f, 0.35f), 0.05f) },
                new Vector3(0f, 0.02f, 0f), new Vector3(4.6f, 0.02f, 4.6f), null, false);
            Vector3[] tables = { new Vector3(-1.1f, 0f, 0.8f), new Vector3(1.2f, 0f, -0.6f) };
            foreach (var tp in tables)
            {
                B.Prop("Furniture/tableRound", t, tp, 0.2f);
                B.Prop("Furniture/chair", t, tp + new Vector3(-0.95f, 0f, 0f), 0.2f, 90f);
                B.Prop("Furniture/chair", t, tp + new Vector3(0.95f, 0f, 0f), 0.2f, -90f);
                // Umbrella
                B.Cyl("UmbrellaPole", t, _mWhite, tp + new Vector3(0f, 1.3f, 0f), 0.07f, 2.6f);
                StripedUmbrella(t, MatLib.Lit("Canopy_Patio", new Color(0.96f, 0.34f, 0.32f), 0.2f), tp + new Vector3(0f, 2.3f, 0f), new Vector3(1.9f, 0.6f, 1.9f));
            }
            B.Prop("Furniture/pottedPlant", t, new Vector3(2.1f, 0f, 1.7f), 0.35f);
            B.Prop("Furniture/pottedPlant", t, new Vector3(-2.2f, 0f, -1.6f), 0.35f);
            return root;
        }

        static Mesh _umbrella;

        static Mesh MakeUmbrella()
        {
            if (_umbrella != null) return _umbrella;
            var g = new MeshGen();
            g.Cone(0.5f, 0f, 0.5f, 16, 0, false);
            g.Cap(0.5f, 0f, 16, 0, false);
            _umbrella = SaveMesh(g.ToMesh("Umbrella"), "Umbrella");
            return _umbrella;
        }

        // ================================================================== customers & helpers

        static CustomerManager BuildCustomers(Transform systems, Counter counter, CashPile cash, Vector3 offset)
        {
            var root = B.Node("Customers", systems, Vector3.zero);
            var cm = root.AddComponent<CustomerManager>();
            cm.counter = counter;
            cm.cash = cash;
            Vector3[] entry = { new Vector3(-15f, 0f, -14.2f), new Vector3(-3.6f, 0f, -14.2f) };
            Vector3[] queue =
            {
                new Vector3(0f, 0f, -8.35f), new Vector3(0f, 0f, -9.45f), new Vector3(0f, 0f, -10.55f), new Vector3(0f, 0f, -11.65f),
                new Vector3(0f, 0f, -12.75f), new Vector3(-1.1f, 0f, -13.9f), new Vector3(-2.2f, 0f, -13.9f)
            };
            Vector3[] exit = { new Vector3(1.6f, 0f, -9.0f), new Vector3(2.4f, 0f, -14.4f), new Vector3(16f, 0f, -14.4f) };
            for (int i = 0; i < entry.Length; i++) entry[i] += offset;
            for (int i = 0; i < queue.Length; i++) queue[i] += offset;
            for (int i = 0; i < exit.Length; i++) exit[i] += offset;
            cm.entryPath = Points(root.transform, "Entry", entry);
            cm.queueSlots = Points(root.transform, "Queue", queue);
            cm.exitPath = Points(root.transform, "Exit", exit);
            return cm;
        }

        static Transform[] Points(Transform parent, string name, Vector3[] pts)
        {
            var arr = new Transform[pts.Length];
            var g = B.Node(name, parent, Vector3.zero).transform;
            for (int i = 0; i < pts.Length; i++) arr[i] = B.Node(name + i, g, pts[i]).transform;
            return arr;
        }

        static NavMeshAgent AddAgent(GameObject go, float speed)
        {
            var a = go.AddComponent<NavMeshAgent>();
            a.radius = 0.35f;
            a.height = 1.5f;
            a.speed = speed;
            a.acceleration = 30f;
            a.angularSpeed = 900f;
            a.stoppingDistance = 0.15f;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            return a;
        }

        static GameObject BuildWaiter(Vector3 pos, Juicer[] juicers, Counter counter, CustomerManager customers)
        {
            var root = CharacterBase("Waiter", "Market/character-employee", out var anim, out _);
            root.transform.SetParent(_actors, false);
            root.transform.position = pos;
            var hands = B.Node("Hands", root.transform, new Vector3(0f, 0.62f, 0.42f));
            var carrier = root.AddComponent<Carrier>();
            carrier.stackRoot = hands.transform;
            carrier.capacity = 6;
            carrier.columns = 1;
            carrier.canCollectLoose = false;
            carrier.swayAmount = 0.04f;
            var ca = root.AddComponent<CharacterAnim>();
            ca.animator = anim;
            ca.hands = carrier;
            ca.animSpeedRef = 3.6f;
            var ai = root.AddComponent<WorkerAI>();
            ai.role = WorkerAI.Role.Waiter;
            ai.agent = AddAgent(root, 3.6f);
            ai.carrier = carrier;
            ai.juicers = juicers;
            ai.counter = counter;
            ai.customers = customers;
            ai.idlePoint = B.Node("WaiterIdle", _actors, pos).transform;

            // "Need X juice!" bubble over the head.
            var warn = B.Node("Warn", root.transform, new Vector3(0f, 2.45f, 0f));
            warn.AddComponent<Billboard>();
            var bubble = _uBubbleWhite != null ? _uBubbleWhite : _sRound;
            B.Sprite("Bg", warn.transform, bubble, new Vector3(0f, -0.05f, 0.01f), SpriteScale(bubble, 1.35f), false, 20);
            var warnIcon = _uWarning != null ? _uWarning : _sStar;
            B.Sprite("Warn", warn.transform, warnIcon, new Vector3(-0.3f, 0.04f, 0f), SpriteScale(warnIcon, 0.42f), false, 21);
            var fruit = B.Sprite("Fruit", warn.transform, _sFruit[0], new Vector3(0.28f, 0.04f, 0f), SpriteScale(_sFruit[0], 0.46f), false, 21);
            ai.warnBubble = warn;
            ai.warnFruit = fruit;
            SetLayerRecursive(root, 2);
            root.SetActive(false);
            return root;
        }

        static GameObject BuildFarmer(int f, FruitField field, Juicer juicer, Vector3 pos, string model)
        {
            var root = CharacterBase("Farmer_" + FruitKey[f], "Characters/" + model, out var anim, out var mdl);
            root.transform.SetParent(_actors, false);
            root.transform.position = pos;

            var stack = B.Node("StackRoot", root.transform, new Vector3(0f, 0.72f, -0.52f));
            B.Model("Market/shopping-basket", root.transform, new Vector3(0f, 0.5f, -0.5f), 1.5f, 90f, "Basket");
            var carrier = root.AddComponent<Carrier>();
            carrier.stackRoot = stack.transform;
            carrier.capacity = 8;
            carrier.columns = 2;
            carrier.columnSpacing = 0.44f;

            // Straw hat so helpers read differently from the player.
            var head = B.Find(mdl, "head");
            var hat = B.Node("Hat", head != null ? head : mdl, Vector3.zero);
            hat.transform.position = root.transform.position + new Vector3(0f, 1.38f, 0f);
            B.MeshObj("Brim", hat.transform, _disc, new[] { _mStraw, _mStraw }, Vector3.zero, new Vector3(0.5f, 0.03f, 0.5f));
            B.MeshObj("Top", hat.transform, _disc, new[] { _mStraw, _mStraw }, Vector3.zero, new Vector3(0.28f, 0.14f, 0.28f));
            B.MeshObj("Band", hat.transform, _disc, new[] { _mRed, _mRed }, new Vector3(0f, 0.02f, 0f), new Vector3(0.29f, 0.05f, 0.29f));

            var torso = B.Find(mdl, "torso");
            var saw = BuildChainsaw(root.transform, new Vector3(0.3f, 0.62f, 0.42f), 0.25f);
            if (torso != null) saw.transform.SetParent(torso, true);
            saw.carrier = carrier;
            saw.owner = root.transform;
            saw.range = 1.35f;
            saw.dps = 5f;
            saw.restrictTo = field;

            var ca = root.AddComponent<CharacterAnim>();
            ca.animator = anim;
            ca.alwaysHoldRight = true;
            ca.animSpeedRef = 3.4f;
            var ai = root.AddComponent<WorkerAI>();
            ai.role = WorkerAI.Role.Farmer;
            ai.agent = AddAgent(root, 3.3f);
            ai.carrier = carrier;
            ai.saw = saw;
            ai.field = field;
            ai.juicer = juicer;
            ai.idlePoint = field.idlePoint;
            SetLayerRecursive(root, 2);
            root.SetActive(false);
            return root;
        }

        // ================================================================== unlock pads

        // ================================================================== decor

        static void BuildDecor()
        {
            string[] trees =
            {
                "Survival/tree", "Survival/tree-tall", "Survival/tree-autumn", "Survival/tree-autumn-tall",
                "Nature/tree_oak", "Nature/tree_default", "Nature/tree_fat"
            };
            string[] bushes = { "Nature/plant_bush", "Nature/plant_bushLarge", "Nature/plant_bushSmall" };
            string[] flowers = { "Nature/flower_redA", "Nature/flower_yellowA", "Nature/flower_purpleA", "Nature/flower_redB", "Nature/flower_yellowB" };
            string[] rocks = { "Survival/rock-a", "Survival/rock-b", "Survival/rock-c", "Nature/rock_smallA", "Nature/rock_smallB" };

            var treesRoot = B.Node("Trees", _nature, Vector3.zero).transform;
            void Tree(Vector3 p)
            {
                if (InReserved(p)) return;
                string m = trees[_rnd.Next(trees.Length)];
                float s = 2.8f + (float)_rnd.NextDouble() * 0.9f;
                B.Prop(m, treesRoot, p, s, (float)_rnd.NextDouble() * 360f);
            }

            // Side forests
            for (float z = -11f; z <= 26f; z += 2.6f)
            {
                Tree(new Vector3(-14.5f - (float)_rnd.NextDouble() * 1.5f, 0f, z + (float)_rnd.NextDouble()));
                Tree(new Vector3(-18f - (float)_rnd.NextDouble() * 2f, 0f, z + 1.3f));
                Tree(new Vector3(14.5f + (float)_rnd.NextDouble() * 1.5f, 0f, z + (float)_rnd.NextDouble()));
                Tree(new Vector3(18f + (float)_rnd.NextDouble() * 2f, 0f, z + 1.3f));
            }
            // North forest
            for (float x = -13f; x <= 13f; x += 2.6f)
            {
                Tree(new Vector3(x + (float)_rnd.NextDouble(), 0f, 21.5f + (float)_rnd.NextDouble() * 1.5f));
                Tree(new Vector3(x + 1.3f, 0f, 25f + (float)_rnd.NextDouble() * 2f));
            }
            // Across the road
            for (float x = -30f; x <= 30f; x += 3.2f)
                Tree(new Vector3(x + (float)_rnd.NextDouble(), 0f, -23f - (float)_rnd.NextDouble() * 2f));

            var small = B.Node("Plants", _nature, Vector3.zero).transform;
            Vector3[] bushSpots =
            {
                new Vector3(-11.6f, 0, -11.5f), new Vector3(11.6f, 0, -11.5f), new Vector3(-11.8f, 0, -2f), new Vector3(11.8f, 0, -2.5f),
                new Vector3(-11.6f, 0, 14f), new Vector3(11.6f, 0, 15f), new Vector3(-6f, 0, 17.8f), new Vector3(6.5f, 0, 18f),
                new Vector3(-3.2f, 0, 12.2f), new Vector3(3.2f, 0, 12.2f), new Vector3(-11f, 0, 7f), new Vector3(11f, 0, 8f)
            };
            foreach (var p in bushSpots)
            {
                if (InReserved(p)) continue;
                B.Prop(bushes[_rnd.Next(bushes.Length)], small, p, 2.6f + (float)_rnd.NextDouble(), (float)_rnd.NextDouble() * 360f);
            }

            for (int i = 0; i < 110; i++)
            {
                Vector3 p;
                int guard = 0;
                do
                {
                    p = new Vector3(((float)_rnd.NextDouble() - 0.5f) * 25f, 0f, -12f + (float)_rnd.NextDouble() * 32f);
                } while (IsBusy(p) && ++guard < 30);
                if (guard >= 30 || InReserved(p)) continue;
                var fl = B.Prop(flowers[_rnd.Next(flowers.Length)], small, p, 1.6f + (float)_rnd.NextDouble() * 0.8f, (float)_rnd.NextDouble() * 360f);
                B.NoShadows(fl);
                Sway(fl.transform, 7f, 2f + (float)_rnd.NextDouble());
            }
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = new Vector3((_rnd.Next(2) == 0 ? -1 : 1) * (10.8f + (float)_rnd.NextDouble() * 1.5f), 0f, -10f + (float)_rnd.NextDouble() * 28f);
                if (InReserved(p)) continue;
                B.Prop(rocks[_rnd.Next(rocks.Length)], small, p, 1.2f + (float)_rnd.NextDouble() * 0.8f, (float)_rnd.NextDouble() * 360f);
            }

            // Street lamps along the sidewalk.
            for (float x = -10f; x <= 10f; x += 10f)
                B.Prop("Furniture/lampRoundFloor", _decor, new Vector3(x + 5f, 0.06f, -15.1f), 0.28f);

            // Crates / barrels near the juicers for a workshop vibe.
            B.Prop("Survival/box", _decor, new Vector3(-11.3f, 0f, 0.6f), 2.6f, 10f);
            B.Prop("Survival/signpost", _decor, new Vector3(2.8f, 0f, 9.8f), 2.6f, 200f);
        }

        /// <summary>Keeps random decor off the plaza, fields and paths.</summary>
        static bool IsBusy(Vector3 p)
        {
            if (p.z < 3f && Mathf.Abs(p.x) < 12.5f) return true; // plaza
            if (p.x > -10f && p.x < -3f && p.z > 3.5f && p.z < 13.5f) return true;
            if (p.x > 3f && p.x < 10f && p.z > 3.5f && p.z < 13.8f) return true;
            if (Mathf.Abs(p.x) < 5f && p.z > 3f && p.z < 19f) return true;
            return false;
        }

        // ================================================================== project settings

        static void ConfigureProject()
        {
            PlayerSettings.productName = "Juice King Tycoon";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            // Strictly portrait (the HUD is laid out for it).
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.gcIncremental = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            // Store builds: Play needs an App Bundle; Android 7.0+ (Unity IAP 5). iOS keeps the engine's minimum (15.0 in Unity 6.4).
            EditorUserBuildSettings.buildAppBundle = true;
            // Never lower the engine's own minimum (API 25 in Unity 6.4); only raise older projects.
            if ((int)PlayerSettings.Android.minSdkVersion < (int)AndroidSdkVersions.AndroidApiLevel24)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            // Store identity, version, target API and the AdMob app id (JuiceKingBuilder.Release.cs).
            ConfigureRelease();
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            BuildSpriteAtlas();

            // PC_RPAsset = editor / desktop (Standalone). Mobile_RPAsset = Android, iOS and WebGL.
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null) continue;
                bool mobile = path.Contains("Mobile");
                var so = new SerializedObject(asset);
                SetProp(so, "m_ShadowDistance", mobile ? 32f : 45f);
                SetProp(so, "m_SoftShadowsSupported", true);
                SetProp(so, "m_MainLightShadowmapResolution", mobile ? 1024 : 2048);
                SetProp(so, "m_ShadowCascadeCount", 1);
                SetProp(so, "m_MSAA", mobile ? 2 : 4);
                SetProp(so, "m_SupportsHDR", !mobile);
                SetProp(so, "m_RenderScale", mobile ? 0.9f : 1f);
                SetProp(so, "m_AdditionalLightsRenderingMode", mobile ? 0 : 1);
                SetProp(so, "m_RequireDepthTexture", false);
                SetProp(so, "m_RequireOpaqueTexture", false);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Pack every UI / icon sprite into one atlas so the canvas batches into a few draw calls.</summary>
        static void BuildSpriteAtlas()
        {
            const string path = "Assets/Game/Generated/UIAtlas.spriteatlasv2";
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    var atlas = new UnityEditor.U2D.SpriteAtlasAsset();
                    atlas.Add(new Object[]
                    {
                        AssetDatabase.LoadAssetAtPath<Object>(UIAtlasCutter.OutDir.TrimEnd('/')),
                        AssetDatabase.LoadAssetAtPath<Object>(ArtGen.Dir.TrimEnd('/')),
                    });
                    UnityEditor.U2D.SpriteAtlasAsset.Save(atlas, path);
                    AssetDatabase.ImportAsset(path);
                }
                var imp = (UnityEditor.U2D.SpriteAtlasImporter)AssetImporter.GetAtPath(path);
                if (imp != null)
                {
                    // Mipmaps keep 256 px icons smooth when shown at 60-120 px (no jagged edges); the padding stops mips bleeding.
                    imp.packingSettings = new UnityEditor.U2D.SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 8 };
                    imp.textureSettings = new UnityEditor.U2D.SpriteAtlasTextureSettings { filterMode = FilterMode.Trilinear, generateMipMaps = true, sRGB = true };
                    imp.includeInBuild = true;
                    imp.SaveAndReimport();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[JuiceKing] Sprite atlas not created: " + e.Message);
            }
        }

        static void SetProp(SerializedObject so, string name, object value)
        {
            var p = so.FindProperty(name);
            if (p == null) return;
            switch (value)
            {
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
            }
        }
    }
}
