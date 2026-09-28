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
        static readonly List<UnlockZone> _zones = new List<UnlockZone>();

        // ================================================================== entry points

        [MenuItem("Juice King/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            ArtGen.GenerateAll();
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
            ConfigureProject();
            AssetDatabase.SaveAssets();
            Debug.Log("[JuiceKing] Build complete: " + ScenePath);
        }

        [MenuItem("Juice King/Rebuild Scene (skip art + import)", priority = 1)]
        public static void RebuildSceneOnly()
        {
            _controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KenneyImport.ControllerPath);
            if (_controller == null) _controller = KenneyImport.BuildController();
            KenneyImport.TintNature();
            BuildFont();
            BuildMeshes();
            BuildMaterials();
            BuildItemPrefabs();
            BuildCustomerPrefabs();
            BuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[JuiceKing] Scene rebuilt: " + ScenePath);
        }

        [MenuItem("Juice King/Reset Save Data", priority = 20)]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteKey("juiceking_save_v1");
            PlayerPrefs.Save();
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

            _mPadIn = MatLib.Sprite("Pad_In", MatLib.Tex("pad_solid.png"), new Color(1f, 0.78f, 0.25f, 1f));
            _mPadOut = MatLib.Sprite("Pad_Out", MatLib.Tex("pad_solid.png"), new Color(0.45f, 1f, 0.5f, 1f));
            _mPadCounter = MatLib.Sprite("Pad_Counter", MatLib.Tex("pad_solid.png"), new Color(0.45f, 0.8f, 1f, 1f));
            _mPadCash = MatLib.Sprite("Pad_Cash", MatLib.Tex("pad_solid.png"), new Color(0.55f, 1f, 0.55f, 1f));
            _mPadUpgrade = MatLib.Sprite("Pad_Upgrade", MatLib.Tex("pad_solid.png"), new Color(0.85f, 0.6f, 1f, 1f));
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
            var refs = systems.AddComponent<GameRefs>();
            systems.AddComponent<LooseItems>();

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
            var cm = BuildCustomers(systems.transform, counter, cash);

            // ---------------- player & helpers
            var player = BuildPlayerObject();
            player.transform.SetParent(_actors, false);
            player.transform.position = new Vector3(0f, 0f, -3f);
            SetLayerRecursive(player, 2);

            var waiter = BuildWaiter(new Vector3(7.6f, 0f, -4.2f), new[] { orangeJuicer, melonJuicer, pineJuicer }, counter);
            var farmerOrange = BuildFarmer(0, orangeField, orangeJuicer, new Vector3(-10.6f, 0f, 2.2f), "character-male-d");
            var farmerMelon = BuildFarmer(1, melonField, melonJuicer, new Vector3(10.6f, 0f, 2.2f), "character-male-f");
            var farmerPine = BuildFarmer(2, pineField, pineJuicer, new Vector3(5.4f, 0f, 16.6f), "character-female-d");

            // ---------------- unlock chain
            var zFarmerPine = Unlock("farmer_pine", "Pineapple Farmer", 700, new Vector3(5.4f, 0f, 16.6f), _sWorker, new[] { farmerPine });
            var zFarmerMelon = Unlock("farmer_melon", "Melon Farmer", 550, new Vector3(10.6f, 0f, 2.2f), _sWorker, new[] { farmerMelon }, zFarmerPine);
            var zFarmerOrange = Unlock("farmer_orange", "Orange Farmer", 400, new Vector3(-10.6f, 0f, 2.2f), _sWorker, new[] { farmerOrange }, zFarmerMelon);
            var zPineJuicer = Unlock("pine_juicer", "Pineapple Juicer", 350, pineJuicer.transform.position, _sJuice, new[] { pineJuicer.gameObject }, zFarmerOrange);
            var zPineField = Unlock("pine_field", "Pineapple Field", 250, new Vector3(0f, 0f, 15.2f), _sFruit[2], new[] { pineField.transform.parent.gameObject }, zPineJuicer);
            var zPatio = Unlock("patio", "Cozy Patio", 150, patio.transform.position, _sStar, new[] { patio });
            var zWaiter = Unlock("hire_waiter", "Hire Waiter", 180, new Vector3(7.6f, 0f, -4.2f), _sWorker, new[] { waiter }, zPineField, zPatio);
            var zMelonMore = Unlock("melon_more", "More Melons", 120, new Vector3(6.5f, 0f, 11.6f), _sFruit[1], new[] { melonMore });
            var zMelonJuicer = Unlock("melon_juicer", "Melon Juicer", 100, melonJuicer.transform.position, _sJuice, new[] { melonJuicer.gameObject }, zWaiter, zMelonMore);
            var zMelonField = Unlock("melon_field", "Watermelon Field", 60, new Vector3(6.5f, 0f, 7.2f), _sFruit[1], new[] { melonField.transform.parent.gameObject }, zMelonJuicer);
            var zUpgrades = Unlock("upgrades", "Upgrade Shop", 40, new Vector3(-8.4f, 0f, -5f), _sSaw, new[] { upgradeStation });
            var zOrangeMore = Unlock("orange_more", "More Oranges", 20, new Vector3(-6.5f, 0f, 11.3f), _sFruit[0], new[] { orangeMore }, zUpgrades, zMelonField);
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
            BuildUI(systems.transform, out var hud);

            var tut = systems.AddComponent<Tutorial>();
            tut.firstField = orangeField;
            tut.firstJuicer = orangeJuicer;
            tut.counter = counter;
            tut.cash = cash;
            var arrow = B.MeshObj("GuideArrow", _actors, _arrow, _mYellow, new Vector3(0, 3, 0), Vector3.one * 0.9f, null, false);
            tut.arrow = arrow.transform;
            var pointer = B.Node("GuidePointer", _actors, Vector3.zero);
            B.Decal("Chevron", pointer.transform, _mPointer, new Vector3(0f, 0f, 0.2f), new Vector2(0.9f, 0.9f));
            tut.pointer = pointer.transform;

            // ---------------- refs
            refs.slicePrefabs = _slicePrefabs;
            refs.juicePrefabs = _juicePrefabs;
            refs.moneyPrefab = _moneyPrefab;
            refs.fruitIcons = _sFruit;
            refs.juiceIcon = _sJuice;
            refs.moneyIcon = _sMoney;
            refs.particleMaterial = _mParticle;
            refs.font = B.Font;
            refs.floatingTextPrefab = _floatingText;
            refs.customerPrefabs = _customerPrefabs.ToArray();
            refs.mainCamera = cam;
            refs.player = player.GetComponent<Carrier>();
            refs.counter = counter;
            refs.cashPile = cash;

            BuildDecor();
            MarkStatic(_decor.gameObject);
            foreach (var n in new[] { "Grass", "Plaza", "Path", "Sidewalk", "Curb", "Road", "CurbFar" })
            {
                var g = _world.Find(n);
                if (g != null) MarkStatic(g.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void MarkStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        // ================================================================== lighting, ground, camera

        static void BuildLighting()
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.95f, 0.86f);
            l.intensity = 1.35f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.72f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.42f, 0.38f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.84f, 0.97f);
            RenderSettings.fogStartDistance = 42f;
            RenderSettings.fogEndDistance = 80f;
            RenderSettings.sun = l;

            // Post processing.
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.15f);
            ca.contrast.Override(10f);
            ca.saturation.Override(18f);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.2f);
            vig.smoothness.Override(0.5f);
            const string profilePath = "Assets/Game/Generated/PostFX.asset";
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
            var path = B.Box("Path", _world, _mTiles, new Vector3(0f, 0.01f, 7.2f), new Vector3(4.4f, 0.02f, 7.4f), null, false);
            path.GetComponent<MeshRenderer>().sharedMaterial = MatLib.Lit("Ground_Tiles_Path", Color.white, 0.12f, MatLib.Tex("ground_tiles.png"), new Vector2(2f, 3.5f));

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

        static Camera BuildCamera(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.84f, 0.97f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            go.AddComponent<AudioListener>();
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var follow = go.AddComponent<CameraFollow>();
            follow.target = target;
            follow.offset = new Vector3(0f, 12.8f, -11.2f);
            go.transform.position = target.position + follow.offset;
            go.transform.rotation = Quaternion.LookRotation(-follow.offset.normalized);
            return cam;
        }

        // ================================================================== fields

        static FruitField BuildField(int f, Vector3 center, Vector2 size, Vector2[] spots, string name)
        {
            var root = B.Node(name, _stations, Vector3.zero);
            var soil = B.Box("Soil", root.transform, _mSoil, center + new Vector3(0f, 0.025f, 0f), new Vector3(size.x, 0.05f, size.y), null, false);
            soil.GetComponent<MeshRenderer>().sharedMaterial = MatLib.Lit("Ground_Soil_" + f, Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(size.x / 3f, size.y / 3f));

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

        static T MakeZone<T>(string name, Transform parent, Vector3 worldPos, Vector2 size, Material mat, Sprite icon) where T : Zone
        {
            var go = B.Node(name, parent, Vector3.zero);
            go.transform.position = worldPos;
            var pad = B.Node("Pad", go.transform, new Vector3(0f, 0.035f, 0f), null, new Vector3(size.x, 1f, size.y));
            B.Decal("Frame", pad.transform, mat, Vector3.zero, Vector2.one);
            if (icon != null) B.Sprite("Icon", go.transform, icon, new Vector3(0f, 0.045f, 0f), Mathf.Min(size.x, size.y) * 0.16f, true, 1, new Color(1f, 1f, 1f, 0.85f));
            var z = go.AddComponent<T>();
            z.size = size;
            z.padVisual = pad.transform;
            return z;
        }

        static Juicer BuildJuicer(int f, Vector3 pos)
        {
            var root = B.Node("Juicer_" + FruitKey[f], _stations, pos);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero);
            var bt = body.transform;

            B.Box("Base", bt, _mDark, new Vector3(0f, 0.12f, 0f), new Vector3(1.9f, 0.24f, 1.7f));
            B.Box("Cabinet", bt, _mFruitBody[f], new Vector3(0f, 0.8f, 0f), new Vector3(1.55f, 1.12f, 1.3f));
            B.Box("Band", bt, _mWhite, new Vector3(0f, 1.3f, 0f), new Vector3(1.6f, 0.12f, 1.35f));
            B.Sprite("IconFront", bt, _sFruit[f], new Vector3(0f, 0.9f, -0.66f), 0.24f, false, 0).transform.localRotation = Quaternion.identity;
            var back = B.Sprite("IconBack", bt, _sFruit[f], new Vector3(0f, 0.9f, 0.66f), 0.24f, false, 0);
            back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Glass jar with juice and spinning blades.
            B.MeshObj("JarBase", bt, _disc, new[] { _mSteel, _mSteel }, new Vector3(0f, 1.36f, 0f), new Vector3(1.0f, 0.1f, 1.0f));
            var liquid = B.MeshObj("Liquid", bt, _disc, new[] { _mJuice[f], _mJuice[f] }, new Vector3(0f, 1.46f, 0f), new Vector3(0.72f, 0.95f, 0.72f), null, false);
            var blades = B.Node("Blades", bt, new Vector3(0f, 1.52f, 0f));
            B.Box("B1", blades.transform, _mSteel, Vector3.zero, new Vector3(0.7f, 0.03f, 0.1f));
            B.Box("B2", blades.transform, _mSteel, Vector3.zero, new Vector3(0.1f, 0.03f, 0.7f));
            B.MeshObj("Jar", bt, _cup, _mGlass, new Vector3(0f, 1.44f, 0f), new Vector3(0.95f, 1.0f, 0.95f), null, false);
            B.MeshObj("Funnel", bt, _funnel, _mFruitBody[f], new Vector3(0f, 2.44f, 0f), new Vector3(0.62f, 0.5f, 0.62f));
            var intake = B.Node("Intake", bt, new Vector3(0f, 2.9f, 0f));

            // Spout + output tray (south, toward the counter).
            B.MeshObj("Spout", bt, B.Cylinder, _mSteel, new Vector3(0f, 0.95f, -0.8f), new Vector3(0.16f, 0.16f, 0.16f), new Vector3(90f, 0f, 0f));
            var spout = B.Node("SpoutPoint", bt, new Vector3(0f, 0.85f, -0.95f));
            var tray = B.Node("Tray", t, new Vector3(0f, 0f, -1.62f));
            B.Box("Top", tray.transform, _mWood, new Vector3(0f, 0.5f, 0f), new Vector3(1.5f, 0.08f, 0.95f));
            foreach (var lx in new[] { -0.65f, 0.65f })
            foreach (var lz in new[] { -0.38f, 0.38f })
                B.Box("Leg", tray.transform, _mDark, new Vector3(lx, 0.25f, lz), new Vector3(0.08f, 0.5f, 0.08f));
            var outPile = B.Node("OutputPile", tray.transform, new Vector3(0f, 0.54f, 0f)).AddComponent<ItemPile>();
            outPile.columns = 4;
            outPile.rows = 2;
            outPile.layers = 3;
            outPile.spacing = new Vector2(0.34f, 0.4f);
            outPile.layerHeight = 0.37f;

            // Input crate (north, toward the fields).
            var crate = B.Node("Crate", t, new Vector3(0f, 0f, 1.45f));
            B.Box("Base", crate.transform, _mWood, new Vector3(0f, 0.17f, 0f), new Vector3(1.15f, 0.34f, 1.05f));
            B.Box("RimL", crate.transform, _mWood, new Vector3(-0.55f, 0.42f, 0f), new Vector3(0.08f, 0.18f, 1.05f));
            B.Box("RimR", crate.transform, _mWood, new Vector3(0.55f, 0.42f, 0f), new Vector3(0.08f, 0.18f, 1.05f));
            B.Box("RimB", crate.transform, _mWood, new Vector3(0f, 0.42f, 0.5f), new Vector3(1.15f, 0.18f, 0.08f));
            var inPile = B.Node("InputPile", t, new Vector3(0f, 0.36f, 1.45f)).AddComponent<ItemPile>();
            inPile.columns = 2;
            inPile.rows = 2;
            inPile.layers = 6;
            inPile.spacing = new Vector2(0.42f, 0.42f);
            inPile.layerHeight = 0.13f;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.2f, -0.1f);
            col.size = new Vector3(1.9f, 2.4f, 4.1f);

            var hum = root.AddComponent<AudioSource>();
            hum.playOnAwake = true;
            hum.loop = true;

            var j = root.AddComponent<Juicer>();
            j.kind = (FruitKind)f;
            j.inputPile = inPile;
            j.outputPile = outPile;
            j.intakePoint = intake.transform;
            j.spoutPoint = spout.transform;
            j.body = bt;
            j.blades = blades.transform;
            j.liquid = liquid.transform;
            j.hum = hum;

            var dz = MakeZone<DropZone>("InputZone", t, pos + new Vector3(0f, 0f, 3.0f), new Vector2(2.3f, 1.5f), _mPadIn, _sFruit[f]);
            dz.receiverBehaviour = j;
            var pz = MakeZone<PickupZone>("OutputZone", t, pos + new Vector3(0f, 0f, -3.0f), new Vector2(2.3f, 1.4f), _mPadOut, _sJuice);
            pz.sourceBehaviour = j;
            j.inputZone = dz;
            j.outputZone = pz;
            return j;
        }

        static Counter BuildCounter(Vector3 pos)
        {
            var root = B.Node("JuiceStand", _stations, pos);
            var t = root.transform;
            B.Box("Counter", t, _mWood, new Vector3(0f, 0.5f, 0f), new Vector3(4.4f, 1.0f, 1.1f));
            B.Box("Top", t, _mWhite, new Vector3(0f, 1.04f, 0f), new Vector3(4.6f, 0.08f, 1.3f));
            B.Box("Kick", t, _mDark, new Vector3(0f, 0.06f, -0.02f), new Vector3(4.42f, 0.12f, 1.12f));
            for (int i = 0; i < 3; i++)
                B.Sprite("Icon" + i, t, _sFruit[i], new Vector3(-1.3f + i * 1.3f, 0.55f, -0.56f), 0.2f, false, 0);

            // Striped skirt on the customer side + end caps.
            B.Box("Skirt", t, _mAwning, new Vector3(0f, 0.62f, -0.56f), new Vector3(4.42f, 0.7f, 0.04f));
            B.Box("CapL", t, _mRed, new Vector3(-2.22f, 0.55f, 0f), new Vector3(0.12f, 1.02f, 1.14f));
            B.Box("CapR", t, _mRed, new Vector3(2.22f, 0.55f, 0f), new Vector3(0.12f, 1.02f, 1.14f));

            // Sandwich-board sign beside the stand, facing the street.
            var sign = B.Node("Sign", t, new Vector3(-3.35f, 0f, -0.9f), new Vector3(0f, 15f, 0f));
            B.Box("Board", sign.transform, _mWhite, new Vector3(0f, 1.05f, 0f), new Vector3(1.6f, 1.1f, 0.1f), new Vector3(-10f, 0f, 0f));
            B.Box("Frame", sign.transform, _mRed, new Vector3(0f, 1.05f, 0.02f), new Vector3(1.72f, 1.22f, 0.08f), new Vector3(-10f, 0f, 0f));
            B.Box("LegL", sign.transform, _mDark, new Vector3(-0.7f, 0.5f, 0.12f), new Vector3(0.08f, 1f, 0.08f));
            B.Box("LegR", sign.transform, _mDark, new Vector3(0.7f, 0.5f, 0.12f), new Vector3(0.08f, 1f, 0.08f));
            var txt = B.Text("Title", sign.transform, "FRESH\nJUICE", 3.6f, new Color(1f, 0.55f, 0.1f), new Vector3(0f, 1.08f, -0.08f));
            txt.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            txt.textWrappingMode = TMPro.TextWrappingModes.Normal;
            txt.lineSpacing = -20f;
            txt.rectTransform.sizeDelta = new Vector2(1.5f, 1f);

            B.Prop("Market/cash-register", t, new Vector3(1.6f, 1.08f, 0.05f), 0.9f, 180f);

            var display = B.Node("Display", t, new Vector3(-0.45f, 1.08f, 0f)).AddComponent<ItemPile>();
            display.columns = 6;
            display.rows = 2;
            display.layers = 2;
            display.spacing = new Vector2(0.36f, 0.42f);
            display.layerHeight = 0.37f;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.size = new Vector3(4.6f, 1.2f, 1.3f);

            var c = root.AddComponent<Counter>();
            c.display = display;
            c.servePoint = B.Node("ServePoint", t, new Vector3(0f, 0f, -1.35f)).transform;
            var dz = MakeZone<DropZone>("StockZone", t, pos + new Vector3(0f, 0f, 1.65f), new Vector2(3.4f, 1.5f), _mPadCounter, _sJuice);
            dz.receiverBehaviour = c;
            c.dropZone = dz;
            return c;
        }

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
                B.MeshObj("Umbrella", t, MakeUmbrella(), _mAwning, tp + new Vector3(0f, 2.3f, 0f), new Vector3(1.9f, 0.6f, 1.9f));
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

        static CustomerManager BuildCustomers(Transform systems, Counter counter, CashPile cash)
        {
            var root = B.Node("Customers", systems, Vector3.zero);
            var cm = root.AddComponent<CustomerManager>();
            cm.counter = counter;
            cm.cash = cash;
            Vector3[] entry = { new Vector3(-26f, 0f, -14.2f), new Vector3(-3.6f, 0f, -14.2f) };
            Vector3[] queue =
            {
                new Vector3(0f, 0f, -8.35f), new Vector3(0f, 0f, -9.45f), new Vector3(0f, 0f, -10.55f), new Vector3(0f, 0f, -11.65f),
                new Vector3(0f, 0f, -12.75f), new Vector3(-1.1f, 0f, -13.9f), new Vector3(-2.2f, 0f, -13.9f)
            };
            Vector3[] exit = { new Vector3(1.6f, 0f, -9.0f), new Vector3(2.4f, 0f, -14.4f), new Vector3(26f, 0f, -14.4f) };
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

        static GameObject BuildWaiter(Vector3 pos, Juicer[] juicers, Counter counter)
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
            ai.idlePoint = B.Node("WaiterIdle", _actors, pos).transform;
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

        static UnlockZone Unlock(string id, string title, int price, Vector3 pos, Sprite icon, GameObject[] reveal, params UnlockZone[] next)
        {
            var go = B.Node("Unlock_" + id, _unlocks, pos);
            var size = new Vector2(2.4f, 2.4f);
            var pad = B.Node("Pad", go.transform, new Vector3(0f, 0.04f, 0f), null, new Vector3(size.x, 1f, size.y));
            B.Decal("Frame", pad.transform, _mPadUnlock, Vector3.zero, Vector2.one);
            var fill = B.MeshObj("Fill", pad.transform, _quadXZ, _mPadFill, new Vector3(0f, 0.004f, -0.5f), new Vector3(0.9f, 1f, 0.0001f), null, false);

            var label = B.Node("Label", go.transform, new Vector3(0f, 1.1f, 0f));
            label.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", label.transform, _sRound, new Vector3(0f, 0.05f, 0.02f), 1f, false, 4, new Color(0f, 0f, 0f, 0.45f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(2.1f, 0.8f);
            var ic = B.Sprite("Icon", label.transform, icon, new Vector3(-0.62f, 0.05f, 0f), 0.22f, false, 5);
            var priceT = B.Text("Price", label.transform, "$" + price, 6.5f, Color.white, new Vector3(0.28f, 0.07f, 0f));
            var titleT = B.Text("Title", label.transform, title, 3.6f, new Color(1f, 0.95f, 0.7f), new Vector3(0f, -0.6f, 0f));

            var z = go.AddComponent<UnlockZone>();
            z.id = id;
            z.title = title;
            z.price = price;
            z.size = size;
            z.padVisual = pad.transform;
            z.reveal = reveal;
            z.next = next;
            z.priceText = priceT;
            z.titleText = titleT;
            z.fill = fill.transform;
            z.icon = ic;
            _zones.Add(z);
            return z;
        }

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

            var treesRoot = B.Node("Trees", _decor, Vector3.zero).transform;
            void Tree(Vector3 p)
            {
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

            var small = B.Node("Plants", _decor, Vector3.zero).transform;
            Vector3[] bushSpots =
            {
                new Vector3(-11.6f, 0, -11.5f), new Vector3(11.6f, 0, -11.5f), new Vector3(-11.8f, 0, -2f), new Vector3(11.8f, 0, -2.5f),
                new Vector3(-11.6f, 0, 14f), new Vector3(11.6f, 0, 15f), new Vector3(-6f, 0, 17.8f), new Vector3(6.5f, 0, 18f),
                new Vector3(-3.2f, 0, 12.2f), new Vector3(3.2f, 0, 12.2f), new Vector3(-11f, 0, 7f), new Vector3(11f, 0, 8f)
            };
            foreach (var p in bushSpots)
                B.Prop(bushes[_rnd.Next(bushes.Length)], small, p, 2.6f + (float)_rnd.NextDouble(), (float)_rnd.NextDouble() * 360f);

            for (int i = 0; i < 70; i++)
            {
                Vector3 p;
                int guard = 0;
                do
                {
                    p = new Vector3(((float)_rnd.NextDouble() - 0.5f) * 25f, 0f, -12f + (float)_rnd.NextDouble() * 32f);
                } while (IsBusy(p) && ++guard < 30);
                if (guard >= 30) continue;
                B.Prop(flowers[_rnd.Next(flowers.Length)], small, p, 1.5f + (float)_rnd.NextDouble() * 0.7f, (float)_rnd.NextDouble() * 360f);
            }
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = new Vector3((_rnd.Next(2) == 0 ? -1 : 1) * (10.8f + (float)_rnd.NextDouble() * 1.5f), 0f, -10f + (float)_rnd.NextDouble() * 28f);
                B.Prop(rocks[_rnd.Next(rocks.Length)], small, p, 1.2f + (float)_rnd.NextDouble() * 0.8f, (float)_rnd.NextDouble() * 360f);
            }

            // Street lamps along the sidewalk.
            for (float x = -10f; x <= 10f; x += 10f)
                B.Prop("Furniture/lampRoundFloor", _decor, new Vector3(x + 5f, 0.06f, -15.1f), 0.28f);

            // Crates / barrels near the juicers for a workshop vibe.
            B.Prop("Survival/barrel", _decor, new Vector3(-7.6f, 0f, -1.5f), 2.4f, 20f);
            B.Prop("Survival/box", _decor, new Vector3(-7.9f, 0f, 0.2f), 2.6f, 10f);
            B.Prop("Survival/box-large", _decor, new Vector3(7.8f, 0f, 0.2f), 2.4f, -15f);
            B.Prop("Survival/barrel", _decor, new Vector3(7.5f, 0f, -1.6f), 2.4f, 0f);
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
            PlayerSettings.runInBackground = true;

            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null) continue;
                var so = new SerializedObject(asset);
                SetProp(so, "m_ShadowDistance", 45f);
                SetProp(so, "m_SoftShadowsSupported", true);
                SetProp(so, "m_MainLightShadowmapResolution", 2048);
                SetProp(so, "m_ShadowCascadeCount", 1);
                SetProp(so, "m_MSAA", 4);
                SetProp(so, "m_SupportsHDR", true);
                so.ApplyModifiedPropertiesWithoutUndo();
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
