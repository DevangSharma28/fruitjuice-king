using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Expansion 1 — the Tropical Farm. A rounded island: juice hut on the beach, four orchards (coconut, mango, banana,
    /// papaya) with their mixers, and a delivery bay where trucks roll in over a causeway for big orders.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        public const string TropicalScenePath = "Assets/Game/Scenes/Tropical.unity";

        static readonly Vector3 IslandCenter = new Vector3(0f, 0f, 4f);

        static float IslandRadius(float a) =>
            35f * (1f + 0.05f * Mathf.Sin(3f * a + 0.4f) + 0.035f * Mathf.Sin(5f * a + 1.7f) + 0.02f * Mathf.Sin(8f * a + 0.3f));

        static bool OnIsland(Vector3 p, float margin = 0f)
        {
            var d = p - IslandCenter;
            d.y = 0f;
            return d.magnitude < IslandRadius(Mathf.Atan2(d.z, d.x)) - margin;
        }

        // Areas random decor keeps clear of (x, z, radius).
        static readonly List<Vector3> _tBusy = new List<Vector3>();

        static bool TBusy(Vector3 p)
        {
            foreach (var r in _tBusy)
                if ((new Vector2(p.x, p.z) - new Vector2(r.x, r.y)).sqrMagnitude < r.z * r.z) return true;
            return false;
        }

        // ================================================================== scene

        static void BuildTropicalScene()
        {
            _rnd = new System.Random(4321);
            _zones.Clear();
            _tBusy.Clear();

            BuildStationMaterials();
            BuildTropicalMaterials();
            BuildTropicalMeshes();
            _mPadIn = PadMat("In", new Color(1f, 0.78f, 0.2f, 1f));
            _mPadOut = PadMat("Out", new Color(0.4f, 0.95f, 0.45f, 1f));
            _mPadCounter = PadMat("Counter", new Color(0.4f, 0.75f, 1f, 1f));
            _mPadCash = PadMat("Cash", new Color(0.5f, 1f, 0.5f, 1f));
            _mPadUpgrade = PadMat("Upgrade", new Color(0.8f, 0.55f, 1f, 1f));
            _mLeaf = MatLib.Lit("Leaf", new Color(0.3f, 0.66f, 0.26f), 0.2f);
            _mSoilDark = MatLib.Lit("SoilDark", new Color(0.45f, 0.3f, 0.18f), 0.05f);
            _mYellow = MatLib.Lit("GuideYellow", new Color(1f, 0.85f, 0.1f), 0.4f);
            _mCurb = MatLib.Lit("Curb", new Color(0.78f, 0.78f, 0.76f), 0.1f);
            _mStraw = MatLib.Lit("StrawHat", new Color(0.95f, 0.8f, 0.4f), 0.1f);
            var padDelivery = PadMat("Delivery", new Color(1f, 0.55f, 0.2f, 1f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var systems = B.Node("Systems", null, Vector3.zero);
            _world = B.Node("World", null, Vector3.zero).transform;
            _stations = B.Node("Stations", _world, Vector3.zero).transform;
            _unlocks = B.Node("Unlocks", null, Vector3.zero).transform;
            _decor = B.Node("Decor", _world, Vector3.zero).transform;
            _actors = B.Node("Actors", null, Vector3.zero).transform;

            var gm = systems.AddComponent<GameManager>();
            gm.sceneExpansion = 1;
            gm.startMoney = Economy.TropicalStartMoney;
            var refs = systems.AddComponent<GameRefs>();
            systems.AddComponent<LooseItems>();
            systems.AddComponent<Boosts>();
            _ambient = systems.AddComponent<Ambient>();
            _nature = B.Node("Nature", _world, Vector3.zero).transform;
            BuildEnvMeshes();

            BuildLighting(true);
            var water = BuildIsland();

            // ---------------- the loop's anchors: hut, cash, workshop, bin
            var counter = BuildCounter(new Vector3(0f, 0f, -9f), "TROPIC JUICE");
            TropicalHut(counter.transform);
            var cash = BuildCashPile(new Vector3(4.3f, 0f, -8.3f));
            var upgradeStation = BuildUpgradeStation(new Vector3(-8.4f, 0f, -7f));
            // Tiki dressing beside the workbench, never over it: a roof there sat between the camera and the pad.
            TikiTorch(upgradeStation.transform, new Vector3(-2.3f, 0f, -1.9f));
            TikiTorch(upgradeStation.transform, new Vector3(2.3f, 0f, -1.9f));
            Fern(upgradeStation.transform, new Vector3(-2f, 0f, 0.9f), 0.8f);
            _trash = BuildTrashBin(new Vector3(-10.4f, 0f, -2.6f), new Vector3(-8.6f, 0f, -2.6f));
            Busy(0f, -10f, 7f);
            Busy(-8.6f, -7.5f, 3f);
            Busy(-9.6f, -2.6f, 2.2f);
            Busy(4.3f, -8.3f, 2f);

            // ---------------- orchards
            var coconutField = BuildTropicalField(3, new Vector3(-9f, 0f, 7.2f), new Vector2(7.6f, 6.2f),
                new[] { new Vector2(-11f, 5.4f), new Vector2(-7f, 5.4f), new Vector2(-11f, 8.9f), new Vector2(-7f, 8.9f) }, "CoconutGrove");
            var coconutMore = BuildTropicalFieldExpansion(3, coconutField, new Vector3(-9f, 0f, 12.2f), new Vector2(7.6f, 3.2f),
                new[] { new Vector2(-11f, 12.4f), new Vector2(-7f, 12.4f) });
            var mangoField = BuildTropicalField(4, new Vector3(9f, 0f, 7.2f), new Vector2(7.6f, 6.2f),
                new[] { new Vector2(7.1f, 5.7f), new Vector2(10.9f, 5.7f), new Vector2(7.1f, 8.7f), new Vector2(10.9f, 8.7f) }, "MangoOrchard");
            var mangoMore = BuildTropicalFieldExpansion(4, mangoField, new Vector3(9f, 0f, 12.2f), new Vector2(7.6f, 3.2f),
                new[] { new Vector2(7.1f, 12.1f), new Vector2(10.9f, 12.1f) });
            var bananaField = BuildTropicalField(5, new Vector3(0f, 0f, 18.6f), new Vector2(9.4f, 5.6f),
                new[] { new Vector2(-3f, 17.3f), new Vector2(0f, 17.3f), new Vector2(3f, 17.3f), new Vector2(-3f, 20f), new Vector2(0f, 20f), new Vector2(3f, 20f) }, "BananaPlantation");
            var papayaField = BuildTropicalField(6, new Vector3(-17.5f, 0f, 17.5f), new Vector2(7.2f, 6.4f),
                new[] { new Vector2(-19.4f, 16f), new Vector2(-15.6f, 16f), new Vector2(-19.4f, 19.2f), new Vector2(-15.6f, 19.2f) }, "PapayaGrove");

            FarmSign(_decor, new Vector3(-9f, 0f, 3.4f), "COCONUT GROVE", _sFruit[3]);
            FarmSign(_decor, new Vector3(9f, 0f, 3.4f), "MANGO ORCHARD", _sFruit[4]);
            FarmSign(_decor, new Vector3(-4.6f, 0f, 15.2f), "BANANAS", _sFruit[5], 18f);
            FarmSign(_decor, new Vector3(-17.5f, 0f, 13.6f), "PAPAYA GROVE", _sFruit[6]);

            // ---------------- mixers (each opens before its orchard)
            var coconutMixer = BuildJuicer(3, new Vector3(-4.6f, 0f, -2.6f));
            var mangoMixer = BuildJuicer(4, new Vector3(4.6f, 0f, -2.6f));
            var bananaMixer = BuildJuicer(5, new Vector3(0f, 0f, 9.6f));
            var papayaMixer = BuildJuicer(6, new Vector3(-15.6f, 0f, 6.8f));
            foreach (var j in new[] { coconutMixer, mangoMixer, bananaMixer, papayaMixer }) TropicalMixerDress(j);

            // ---------------- customers walk along the beach promenade
            var cm = BuildCustomers(systems.transform, counter, cash, new Vector3(0f, 0f, -2f));

            // ---------------- player & helpers
            var player = BuildPlayerObject();
            player.transform.SetParent(_actors, false);
            player.transform.position = new Vector3(0f, 0f, -5f);
            SetLayerRecursive(player, 2);

            var mixers = new[] { coconutMixer, mangoMixer, bananaMixer, papayaMixer };
            var runner = BuildWaiter(new Vector3(7.8f, 0f, -6.2f), mixers, counter, cm);
            runner.name = "Runner";

            // ---------------- delivery bay
            var bay = BuildDeliveryBay(systems.transform, padDelivery, out var bayRoot, out var loadZone);
            var loader = BuildWaiter(new Vector3(10.2f, 0f, -8.3f), mixers, counter, cm);
            loader.name = "Loader";
            var lai = loader.GetComponent<WorkerAI>();
            lai.role = WorkerAI.Role.Loader;
            lai.deliveryZone = loadZone;
            lai.customers = null;
            var lIdle = lai.idlePoint;
            lIdle.name = "LoaderIdle";

            var farmerCoconut = BuildFarmer(3, coconutField, coconutMixer, new Vector3(-12.8f, 0f, 1f), "character-male-d");
            var farmerMango = BuildFarmer(4, mangoField, mangoMixer, new Vector3(14.3f, 0f, 2.3f), "character-male-f");
            var farmerBanana = BuildFarmer(5, bananaField, bananaMixer, new Vector3(6.9f, 0f, 20.4f), "character-female-d");
            var farmerPapaya = BuildFarmer(6, papayaField, papayaMixer, new Vector3(-22.6f, 0f, 11.6f), "character-female-b");

            // ---------------- decor unlock
            var cabana = BuildCabana(new Vector3(-12.8f, 0f, -11.8f));

            // ---------------- unlock chain (cheapest first; each mixer before its orchard)
            var zPremium = TUnlock("t_premium", "Premium Contracts", 20000, new Vector3(11.8f, 0f, -2.8f), _uCrown ? _uCrown : _sCrown, new GameObject[0],
                "VIP trucks now visit with luxury orders!");
            var zFarmerPapaya = TUnlock("t_farmer_papaya", "Papaya Farmer", 14000, new Vector3(-22.6f, 0f, 11.6f), _uFarmer, new[] { farmerPapaya }, "An expert picker joins the grove");
            var zPapayaField = TUnlock("t_papaya_farm", "Papaya Grove", 12000, new Vector3(-17.5f, 0f, 17.5f), _sFruit[6], new[] { papayaField.transform.parent.gameObject },
                "Sweet papayas for the finest juice", zFarmerPapaya, zPremium);
            var zPapayaMixer = TUnlock("t_papaya_mixer", "Papaya Mixer", 9000, papayaMixer.transform.position, _uMachine, new[] { papayaMixer.gameObject }, "Papaya juice sells for top dollar", zPapayaField);
            var zFarmerBanana = TUnlock("t_farmer_banana", "Banana Farmer", 6000, new Vector3(6.9f, 0f, 20.4f), _uFarmer, new[] { farmerBanana }, "Bananas harvest themselves now");
            var zMangoMore = TUnlock("t_mango_more", "More Mangoes", 4000, new Vector3(9f, 0f, 12.2f), _sFruit[4], new[] { mangoMore }, "Two more mango trees");
            var zBananaField = TUnlock("t_banana_farm", "Banana Plantation", 4500, new Vector3(0f, 0f, 18.6f), _sFruit[5], new[] { bananaField.transform.parent.gameObject },
                "Creamy banana shakes unlocked", zFarmerBanana, zPapayaMixer, zMangoMore);
            var zBananaMixer = TUnlock("t_banana_mixer", "Banana Blender", 3000, bananaMixer.transform.position, _uMachine, new[] { bananaMixer.gameObject }, "Blend bananas into thick shakes", zBananaField);
            var zCabana = TUnlock("t_cabana", "Beach Cabana", 1500, cabana.transform.position, _uStar, new[] { cabana }, "Charm: customers pay +10%", zBananaMixer);
            var zLoader = TUnlock("t_loader", "Hire Loader", 3500, new Vector3(10.2f, 0f, -8.3f), _uWaiter, new[] { loader }, "The loader fills trucks for you");
            var zFarmerMango = TUnlock("t_farmer_mango", "Mango Farmer", 2500, new Vector3(14.3f, 0f, 2.3f), _uFarmer, new[] { farmerMango }, "Mangoes picked around the clock");
            var zFarmerCoconut = TUnlock("t_farmer_coconut", "Coconut Farmer", 1600, new Vector3(-12.8f, 0f, 1f), _uFarmer, new[] { farmerCoconut }, "Your first tropical farmhand", zFarmerMango);
            var zDelivery = TUnlock("t_delivery", "Delivery Bay", 1200, loadZone.transform.position, _sTruck, new[] { bayRoot },
                "Trucks bring BIG orders - fill them for huge rewards!", zLoader, zCabana);
            zDelivery.peekOnUnlock = true;
            var zRunner = TUnlock("t_runner", "Hire Runner", 1000, new Vector3(7.8f, 0f, -6.2f), _uWaiter, new[] { runner }, "Serves the queue so you can farm", zFarmerCoconut);
            var zMangoField = TUnlock("t_mango_farm", "Mango Orchard", 800, new Vector3(9f, 0f, 7.2f), _sFruit[4], new[] { mangoField.transform.parent.gameObject },
                "Juicy mangoes, juicier profits", zDelivery, zRunner);
            var zMangoMixer = TUnlock("t_mango_mixer", "Mango Mixer", 500, mangoMixer.transform.position, _uMachine, new[] { mangoMixer.gameObject }, "Mango juice is on the menu", zMangoField);
            var zUpgrades = TUnlock("t_upgrades", "Tiki Workshop", 150, new Vector3(-8.4f, 0f, -7f), _uTools, new[] { upgradeStation }, "Upgrade your farm, mixers, trucks and crew");
            var zCoconutMore = TUnlock("t_coconut_more", "More Palms", 250, new Vector3(-9f, 0f, 12.2f), _sFruit[3], new[] { coconutMore }, "Two more coconut palms", zMangoMixer);
            zUpgrades.startVisible = true;
            zCoconutMore.startVisible = true;
            foreach (var z in new[] { zMangoField, zBananaField, zPapayaField, zMangoMixer, zBananaMixer, zPapayaMixer, zCabana }) z.peekOnUnlock = true;

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
            var cam = BuildCamera(player.transform, new Color(0.55f, 0.82f, 0.96f), 140f);
            BuildUI(systems.transform, out var hud, 1);

            var tut = systems.AddComponent<Tutorial>();
            tut.firstField = coconutField;
            tut.firstJuicer = coconutMixer;
            tut.counter = counter;
            tut.cash = cash;
            tut.upgradeZone = upgradeStation.GetComponentInChildren<UpgradeZone>(true);
            var arrowMat = Emissive("GuideGlow", new Color(1f, 0.82f, 0.1f), 1.5f);
            var outlineMat = MatLib.Lit("GuideOutline", new Color(0.3f, 0.16f, 0.04f), 0f);
            outlineMat.SetFloat("_Cull", 1f);
            var arrow = B.MeshObj("GuideArrow", _actors, _arrow, arrowMat, new Vector3(0, 3, 0), Vector3.one * 1.35f, null, false);
            B.MeshObj("Outline", arrow.transform, _arrow, outlineMat, new Vector3(0f, -0.07f, 0f), Vector3.one * 1.13f, null, false);
            tut.arrow = arrow.transform;
            var pointer = B.Node("GuidePointer", _actors, Vector3.zero);
            B.Decal("Chevron", pointer.transform, _mPointer, new Vector3(0f, 0f, 0.35f), new Vector2(1.5f, 1.5f)).GetComponent<MeshRenderer>().sortingOrder = 6;
            tut.pointer = pointer.transform;
            tut.edgeArrow = _edgeArrow;
            tut.edgeArea = _edgeArea;

            // ---------------- delivery manager
            var dm = systems.AddComponent<DeliveryManager>();
            dm.bay = bay;

            // ---------------- world intro
            var em = systems.AddComponent<ExpansionManager>();
            em.nextExpansion = 2;
            em.completionPopup = _uiCompletion;
            em.nextWorldButton = _uiNextWorld;
            em.intro = _uiIntro;
            if (_uiIntro != null)
            {
                var shots = B.Node("IntroShots", systems.transform, Vector3.zero).transform;
                _uiIntro.openingPoint = B.Node("Opening", shots, new Vector3(0f, 0f, 5f)).transform;
                _uiIntro.openingDistance = 2.3f;
                _uiIntro.demoTruck = bay.trucks[(int)TruckKind.JuiceTruck];
                _uiIntro.demoPark = bay.park;
                _uiIntro.shots = new[]
                {
                    new ExpansionIntro.Shot { point = B.Node("Groves", shots, new Vector3(-8.5f, 0f, 7.5f)).transform, caption = "Grow coconuts, mangoes, bananas & papayas", distance = 1.2f, hold = 1.9f },
                    new ExpansionIntro.Shot { point = B.Node("Mixers", shots, new Vector3(0f, 0f, -2.5f)).transform, caption = "Blend them into premium tropical juice", distance = 1.1f, hold = 1.6f },
                    new ExpansionIntro.Shot { point = B.Node("Bay", shots, new Vector3(17f, 0f, -5f)).transform, caption = "Fill big delivery trucks for huge rewards", distance = 1.25f, hold = 1.9f, showTruck = true },
                    new ExpansionIntro.Shot { point = B.Node("Hut", shots, new Vector3(0f, 0f, -9.5f)).transform, caption = "Your juice hut opens now. Build the Juice Empire!", distance = 1f, hold = 1.7f },
                };
            }

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

            // ---------------- living world
            BuildTropicalDecor();
            BuildLagoon(new Vector3(-19f, 0f, -6.5f), 3.6f, water);
            BuildBeach();
            BuildBirds(_decor, new[] { new Vector3(-6f, 0f, -26f), new Vector3(8f, 0f, -28f), new Vector3(24f, 0f, -22f), new Vector3(-22f, 0f, -20f), new Vector3(2f, 0f, -34f) }, true);
            BuildBirds(_decor, new[] { new Vector3(-12f, 0f, 22f), new Vector3(10f, 0f, 24f), new Vector3(-24f, 0f, 8f) }, false);
            BuildButterflies();
            MoveButterflies(new[]
            {
                new Vector3(-12.5f, 0f, 3.2f), new Vector3(12.5f, 0f, 3f), new Vector3(-4.8f, 0f, 14f), new Vector3(5f, 0f, 14.5f),
                new Vector3(-14f, 0f, 12f), new Vector3(15f, 0f, 12f), new Vector3(-12f, 0f, -12f), new Vector3(10f, 0f, -12f)
            });
            BuildClouds();
            // The water shader animates itself; only sprite foam scrolls here, and the waterfall has its own flow.
            var scrolled = new List<Renderer>();
            var flows = new List<Renderer>();
            foreach (var r in water)
            {
                if (r == null) continue;
                if (r.sharedMaterial == _tWaterfall) flows.Add(r);
                else if (r.sharedMaterial.shader.name != "JuiceKing/Water") scrolled.Add(r);
            }
            _ambient.water = scrolled.ToArray();
            _ambient.waterScroll = new Vector2(0.012f, 0.008f);
            _ambient.flows = flows.ToArray();
            _ambient.flowScroll = new Vector2(0f, -0.55f);
            var sailA = MatLib.Lit("Sail_A", new Color(1f, 0.97f, 0.9f), 0.1f);
            var sailB = MatLib.Lit("Sail_B", new Color(1f, 0.55f, 0.35f), 0.1f);
            Sailboat(_decor, new[] { new Vector3(-30f, -0.35f, -42f), new Vector3(10f, -0.35f, -46f) }, sailA, 1.6f, 0f);
            Sailboat(_decor, new[] { new Vector3(40f, -0.35f, -30f), new Vector3(46f, -0.35f, 6f) }, sailB, 1.3f, 12f);

            PolishTropical();

            // ---------------- mobile optimisation
            // Small ground decor does not need to cast shadows (saves shadow-map draw calls and fill).
            foreach (var t in new[] { _decor, _nature, _stations })
                foreach (var tr in t.GetComponentsInChildren<Transform>(true))
                    if (tr.name == "Fern" || tr.name == "Hibiscus" || tr.name == "Rocks" || tr.name == "Starfish" || tr.name == "BeachBall"
                        || tr.name == "TikiTorch" || tr.name == "FruitCrate" || tr.name == "Surfboard" || tr.name == "PathBlob")
                        B.NoShadows(tr.gameObject);
            OptimizeScene("Tropical");
            foreach (var node in _stations.GetComponentsInChildren<TropicalFruitNode>(true))
            {
                CombineUnder(node.plant, node.visual);
                CombineUnder(node.visual);
            }
            MarkStatic(_decor.gameObject);
            MarkStatic(_nature.gameObject);
            foreach (var n in new[] { "Gulls", "Parrots", "Butterflies", "CloudShadows", "Sailboat" })
                foreach (Transform c in _decor)
                    if (c.name == n) ClearStatic(c.gameObject);
            foreach (var sw in _ambient.swayers) if (sw.t != null) ClearStatic(sw.t.gameObject);
            foreach (var sp in _ambient.spinners) if (sp.t != null) ClearStatic(sp.t.gameObject);
            foreach (var r in water) if (r != null) ClearStatic(r.gameObject);
            SetLayerRecursive(_decor.gameObject, CameraFollow.BigDecorLayer);
            SetLayerRecursive(_nature.gameObject, CameraFollow.BigDecorLayer);
            var small = _nature.Find("Small");
            if (small != null) SetLayerRecursive(small.gameObject, CameraFollow.SmallDecorLayer);
            var flies = _decor.Find("Butterflies");
            if (flies != null) SetLayerRecursive(flies.gameObject, CameraFollow.SmallDecorLayer);
            var ground = _world.Find("Ground");
            if (ground != null) MarkStatic(ground.gameObject);
            foreach (var r in water) if (r != null) ClearStatic(r.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, TropicalScenePath);
            SetBuildScenes();
        }

        static void Busy(float x, float z, float r) => _tBusy.Add(new Vector3(x, z, r));

        // ================================================================== ground: ocean, island, grass, paths

        /// <summary>Returns the water renderers (for scrolling).</summary>
        static List<Renderer> BuildIsland()
        {
            var root = B.Node("Ground", _world, Vector3.zero).transform;
            var water = new List<Renderer>();

            var ocean = B.MeshObj("Ocean", root, B.Quad, _tOcean, new Vector3(0f, -0.35f, 0f), new Vector3(420f, 420f, 1f), new Vector3(90f, 0f, 0f), false);
            water.Add(ocean.GetComponent<Renderer>());

            var island = B.MeshObj("Island", root, IslandMesh("T_Island", IslandCenter, IslandRadius, 160, 3.2f, 1.1f), _tSand, Vector3.zero, Vector3.one, null, false);
            island.AddComponent<MeshCollider>();
            var foam = B.MeshObj("Foam", root, BandMesh("T_Foam", IslandCenter, IslandRadius, 160, 1.7f, 5.2f, -0.33f), _tFoam, Vector3.zero, Vector3.one, null, false);
            water.Add(foam.GetComponent<Renderer>());
            // Turquoise shallows fading into deeper water.
            B.MeshObj("Shallows", root, BandMesh("T_Shallows", IslandCenter, IslandRadius, 160, 1.4f, 16f, -0.34f), _tShallows, Vector3.zero, Vector3.one, null, false);
            // Wet sand darkening just above the water line.
            var wet = MatLib.Sprite("T_WetSand", MatLib.Tex("foam_strip.png"), new Color(0.72f, 0.58f, 0.4f, 0.45f));
            wet.renderQueue = 2985;
            B.MeshObj("WetSand", root, BandMesh("T_WetSand", IslandCenter, IslandRadius, 160, 1.9f, -1.2f, -0.12f), wet, Vector3.zero, Vector3.one, null, false);

            // Organic grass patches (soft edges blend into the sand).
            void Patch(float x, float z, float size, float yaw)
            {
                var d = B.MeshObj("GrassPatch", root, B.Quad, _tGrassPatch, new Vector3(x, 0.012f + (float)_rnd.NextDouble() * 0.004f, z), new Vector3(size, size * 0.86f, 1f), new Vector3(90f, yaw, 0f), false);
                d.GetComponent<MeshRenderer>().receiveShadows = true;
            }
            Patch(-9f, 8.5f, 17f, 20f);
            Patch(9f, 8.5f, 17f, 140f);
            Patch(0f, 18.5f, 18f, 75f);
            Patch(-17.5f, 16.5f, 16f, 200f);
            Patch(0f, 3.5f, 12f, 310f);
            Patch(-16f, 5f, 12f, 40f);
            Patch(19f, 9f, 14f, 250f);
            Patch(-6f, 28f, 20f, 170f);
            Patch(12f, 27f, 18f, 95f);
            Patch(-26f, 22f, 14f, 15f);
            Patch(25f, 22f, 14f, 60f);
            Patch(-24f, -4f, 12f, 120f);

            // Packed-sand walkways as chains of soft blobs.
            void Path(params Vector3[] pts)
            {
                var line = Spline(pts, 1.1f);
                foreach (var p in line)
                {
                    float s = 2.5f + (float)_rnd.NextDouble() * 0.5f;
                    var d = B.Decal("PathBlob", root, _tPathBlob, new Vector3(p.x, 0.018f, p.z), new Vector2(s, s), (float)_rnd.NextDouble() * 360f);
                    d.GetComponent<MeshRenderer>().sortingOrder = -10;
                }
            }
            Path(new Vector3(0f, 0, -5f), new Vector3(0f, 0, 1.5f), new Vector3(0f, 0, 6.5f));
            Path(new Vector3(-2.2f, 0, 1.4f), new Vector3(-6f, 0, 3.2f), new Vector3(-9f, 0, 3.6f));
            Path(new Vector3(2.2f, 0, 1.4f), new Vector3(6f, 0, 3.2f), new Vector3(9f, 0, 3.6f));
            Path(new Vector3(0f, 0, 12.8f), new Vector3(0f, 0, 14.8f));
            Path(new Vector3(-6f, 0, 1f), new Vector3(-11.5f, 0, 2f), new Vector3(-15.6f, 0, 3.4f));
            Path(new Vector3(-15.6f, 0, 10f), new Vector3(-17f, 0, 13.4f));
            Path(new Vector3(5.5f, 0, -6f), new Vector3(10f, 0, -6.5f), new Vector3(14.5f, 0, -5.5f));

            // Wooden boardwalk deck under the juice hut.
            var deckMat = MatLib.Lit("T_DeckHut", new Color(1f, 0.95f, 0.88f), 0.1f, MatLib.Tex("wood.png"), new Vector2(10.5f / 3.2f, 6.6f / 3.2f));
            B.MeshObj("Deck", root, _disc, new[] { deckMat, _mWood }, new Vector3(0f, 0f, -8.6f), new Vector3(10.5f, 0.04f, 6.6f), null, false);

            // Invisible walls just inside the waterline.
            var bounds = B.Node("Bounds", _world, Vector3.zero).transform;
            const int n = 56;
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                var p0 = IslandCenter + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * (IslandRadius(a0) - 1.6f);
                var p1 = IslandCenter + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * (IslandRadius(a1) - 1.6f);
                var mid = (p0 + p1) * 0.5f + Vector3.up;
                var w = B.Node("Wall", bounds, mid, new Vector3(0f, Mathf.Atan2(p1.x - p0.x, p1.z - p0.z) * Mathf.Rad2Deg, 0f));
                w.AddComponent<BoxCollider>().size = new Vector3(0.6f, 2f, Vector3.Distance(p0, p1) + 0.4f);
            }
            return water;
        }

        // ================================================================== orchards

        static FruitField BuildTropicalField(int f, Vector3 center, Vector2 size, Vector2[] spots, string name)
        {
            var root = B.Node(name, _stations, Vector3.zero);
            // Rounded plot: darker rim under a soil oval.
            B.MeshObj("Rim", root.transform, _disc, new[] { _tSoilRim, _tSoilRim }, center + new Vector3(0f, 0.005f, 0f), new Vector3(size.x + 0.6f, 0.04f, size.y + 0.6f), null, false);
            var soilMat = MatLib.Lit("T_Soil_" + f, Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(size.x / 3f, size.y / 3f));
            B.MeshObj("Soil", root.transform, _disc, new[] { soilMat, soilMat }, center + new Vector3(0f, 0.01f, 0f), new Vector3(size.x, 0.05f, size.y), null, false);
            var fieldGo = B.Node("Field", root.transform, center);
            var field = fieldGo.AddComponent<FruitField>();
            field.kind = (FruitKind)f;
            field.idlePoint = B.Node("Idle", fieldGo.transform, new Vector3(0f, 0f, -size.y * 0.5f - 0.7f)).transform;
            foreach (var s in spots) BuildTropicalPlant(f, field, root.transform, new Vector3(s.x, 0f, s.y));
            OrchardDecor(f, root.transform, center, size);
            Busy(center.x, center.z, Mathf.Max(size.x, size.y) * 0.62f);
            return field;
        }

        static GameObject BuildTropicalFieldExpansion(int f, FruitField field, Vector3 center, Vector2 size, Vector2[] spots)
        {
            var root = B.Node(FruitKey[f] + "Expansion", field.transform.parent, Vector3.zero);
            B.MeshObj("Rim", root.transform, _disc, new[] { _tSoilRim, _tSoilRim }, center + new Vector3(0f, 0.004f, 0f), new Vector3(size.x + 0.6f, 0.04f, size.y + 0.6f), null, false);
            var soilMat = MatLib.Lit("T_SoilX_" + f, Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(size.x / 3f, size.y / 3f));
            B.MeshObj("Soil", root.transform, _disc, new[] { soilMat, soilMat }, center + new Vector3(0f, 0.009f, 0f), new Vector3(size.x, 0.05f, size.y), null, false);
            foreach (var s in spots) BuildTropicalPlant(f, field, root.transform, new Vector3(s.x, 0f, s.y));
            Busy(center.x, center.z, Mathf.Max(size.x, size.y) * 0.62f);
            return root;
        }

        /// <summary>Each orchard dresses differently: coconut piles and a hammock, mango crates and a ladder, banana racks, papaya baskets.</summary>
        static void OrchardDecor(int f, Transform parent, Vector3 c, Vector2 size)
        {
            var t = B.Node("Decor", parent, Vector3.zero).transform;
            float ex = size.x * 0.5f + 0.9f;
            switch ((FruitKind)f)
            {
                case FruitKind.Coconut:
                    CrateOfFruit(t, c + new Vector3(-ex, 0f, -1.4f), f, 20f);
                    for (int i = 0; i < 5; i++)
                        B.MeshObj("Nut", t, _lowSphere, _mShell, c + new Vector3(-ex + 0.2f + (i % 3) * 0.34f, 0.18f + (i / 3) * 0.28f, 0.6f + (i % 2) * 0.1f), Vector3.one * 0.38f);
                    // Hammock slung between two short posts.
                    B.Cyl("PostA", t, _tBamboo, c + new Vector3(-ex, 0.8f, 1.8f), 0.14f, 1.6f);
                    B.Cyl("PostB", t, _tBamboo, c + new Vector3(-ex, 0.8f, 4.1f), 0.14f, 1.6f);
                    B.MeshObj("Hammock", t, _lowSphere, MatLib.Lit("Hammock", new Color(1f, 0.55f, 0.35f), 0.1f), c + new Vector3(-ex, 0.75f, 2.95f), new Vector3(0.7f, 0.16f, 2.3f));
                    break;
                case FruitKind.Mango:
                    CrateOfFruit(t, c + new Vector3(ex, 0f, -1.4f), f, -15f);
                    CrateOfFruit(t, c + new Vector3(ex + 0.2f, 0.62f, -1.3f), f, 5f);
                    B.Box("LadderL", t, _mWood, c + new Vector3(ex - 0.2f, 1.1f, 2f), new Vector3(0.08f, 2.3f, 0.08f), new Vector3(-12f, 0f, 0f));
                    B.Box("LadderR", t, _mWood, c + new Vector3(ex + 0.4f, 1.1f, 2f), new Vector3(0.08f, 2.3f, 0.08f), new Vector3(-12f, 0f, 0f));
                    for (int i = 0; i < 5; i++)
                        B.Box("Rung", t, _mWood, c + new Vector3(ex + 0.1f, 0.3f + i * 0.42f, 2.2f - i * 0.09f), new Vector3(0.62f, 0.06f, 0.06f));
                    break;
                case FruitKind.Banana:
                    // Drying rack with hanging bunches.
                    B.Cyl("RackA", t, _tBamboo, c + new Vector3(-ex, 1f, -0.8f), 0.14f, 2f);
                    B.Cyl("RackB", t, _tBamboo, c + new Vector3(-ex, 1f, 1.2f), 0.14f, 2f);
                    B.MeshObj("Bar", t, B.Cylinder, _tBamboo, c + new Vector3(-ex, 1.95f, 0.2f), new Vector3(0.1f, 1.05f, 0.1f), new Vector3(90f, 0f, 0f));
                    for (int i = 0; i < 3; i++)
                    {
                        var bunch = B.Node("Bunch", t, c + new Vector3(-ex, 1.45f, -0.4f + i * 0.6f)).transform;
                        FruitCluster(5, bunch);
                        bunch.localScale = Vector3.one * 0.7f;
                        Sway(bunch, 4f, 1.4f + i * 0.2f);
                    }
                    CrateOfFruit(t, c + new Vector3(ex, 0f, -0.8f), f, 10f);
                    break;
                default:
                    CrateOfFruit(t, c + new Vector3(ex, 0f, -1f), f, -20f);
                    B.MeshObj("Basket", t, _cup, _mStraw, c + new Vector3(ex, 0f, 0.8f), new Vector3(0.9f, 0.5f, 0.9f));
                    for (int i = 0; i < 3; i++)
                        B.MeshObj("Papaya", t, _lowSphere, _tPapayaSkin, c + new Vector3(ex - 0.15f + i * 0.15f, 0.55f, 0.8f + (i % 2) * 0.1f), new Vector3(0.28f, 0.46f, 0.28f), new Vector3(0f, 0f, 70f));
                    break;
            }
            // Ferns and flowers soften the plot edge.
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f + 0.3f;
                var p = c + new Vector3(Mathf.Cos(a) * (size.x * 0.5f + 0.35f), 0f, Mathf.Sin(a) * (size.y * 0.5f + 0.35f));
                if (Mathf.Sin(a) < -0.6f) continue; // keep the camera side open
                if (i % 2 == 0) Fern(t, p, 0.9f);
                else Hibiscus(t, p);
            }
        }

        static void TropicalMixerDress(Juicer j)
        {
            var p = j.transform.position;
            var t = B.Node("MixerDecor_" + j.kind, _decor, p).transform;
            Fern(t, new Vector3(1.25f, 0f, 0.7f), 0.9f);
            Fern(t, new Vector3(-1.25f, 0f, 0.7f), 0.8f);
            Hibiscus(t, new Vector3(-1.3f, 0f, -0.5f));
            TikiTorch(t, new Vector3(1.35f, 0f, -0.9f));
            // Bamboo ring on the plinth.
            B.MeshObj("BambooRing", j.body, _disc, new[] { _tBamboo, _tBamboo }, new Vector3(0f, 0.24f, 0f), new Vector3(2.02f, 0.06f, 1.82f), null, false);
            Busy(p.x, p.z, 3.4f);
        }

        /// <summary>Bamboo tiki frame around the juice counter: posts, a sign beam and leafy garlands (no roof, so the counter stays visible from above).</summary>
        static void TropicalHut(Transform stand)
        {
            // The frame stands behind the counter and the stock pad and rises well above them, so from the camera it never
            // covers the cups, the stock readout or the player working at the counter.
            var t = B.Node("Hut", stand, Vector3.zero).transform;
            const float z = 2.95f, top = 3.9f; // behind the stock pad (z 0.95-2.45), so it frames the scene from behind the player
            foreach (var sx in new[] { -2.6f, 2.6f })
            {
                B.Cyl("Post", t, _tBamboo, new Vector3(sx, top * 0.5f, z), 0.18f, top);
                B.MeshObj("PostCap", t, _lowSphere, _tThatch, new Vector3(sx, top + 0.05f, z), new Vector3(0.5f, 0.3f, 0.5f));
                for (int i = 0; i < 4; i++)
                    B.MeshObj("PostLeaf", t, _shBroadLeaf, _tPalmLeaf, new Vector3(sx, top + 0.1f, z), Vector3.one * 0.42f, new Vector3(-25f, i * 90f + 45f, 0f));
            }
            B.MeshObj("Beam", t, B.Cylinder, _tBamboo, new Vector3(0f, top - 0.2f, z), new Vector3(0.16f, 2.65f, 0.16f), new Vector3(0f, 0f, 90f));
            RB("SignBoard", t, _mWood, new Vector3(0f, top + 0.12f, z - 0.03f), new Vector3(3.3f, 0.66f, 0.12f), 0.1f);
            var sign = B.Text("HutSign", t, "TROPIC JUICE", 4.8f, new Color(1f, 0.85f, 0.25f), new Vector3(0f, top + 0.13f, z - 0.11f));
            sign.rectTransform.sizeDelta = new Vector2(3.1f, 0.6f);
            // Garland of leaves hanging off the beam.
            for (int i = 0; i < 8; i++)
                B.MeshObj("Garland", t, _shBroadLeaf, _tPalmLeaf, new Vector3(-2.2f + i * 0.63f, top - 0.25f, z - 0.05f), Vector3.one * 0.26f, new Vector3(70f, 180f + (i % 2) * 20f, 0f), false);
            foreach (var x in new[] { -1.9f, 1.9f })
                B.MeshObj("Flower", t, _lowSphere, _tHibiscus, new Vector3(x, top - 0.25f, z - 0.1f), new Vector3(0.22f, 0.14f, 0.22f), null, false);
            TikiTorch(t, new Vector3(-3.3f, 0f, -1.4f));
            TikiTorch(t, new Vector3(3.3f, 0f, -1.4f));
        }

        // ================================================================== delivery bay

        static DeliveryBay BuildDeliveryBay(Transform systems, Material padMat, out GameObject revealRoot, out DeliveryZone loadZone)
        {
            var root = B.Node("Delivery", _world, Vector3.zero).transform;
            var park = new Vector3(18.4f, 0f, -5f);

            // Road: in over the southern causeway, through the bay, out over the eastern causeway.
            var inCtrl = new[] { new Vector3(56f, 0f, -52f), new Vector3(40f, 0f, -34f), new Vector3(28f, 0f, -21f), new Vector3(21.5f, 0f, -12.5f), new Vector3(18.8f, 0f, -8f), park };
            var outCtrl = new[] { park, new Vector3(18.6f, 0f, 0.5f), new Vector3(20.5f, 0f, 6.5f), new Vector3(25.5f, 0f, 10.8f), new Vector3(33f, 0f, 12.8f), new Vector3(46f, 0f, 14f), new Vector3(70f, 0f, 16f) };
            var inPts = Spline(inCtrl, 1.2f);
            var outPts = Spline(outCtrl, 1.2f);
            var all = new List<Vector3>(inPts);
            all.AddRange(outPts.GetRange(1, outPts.Count - 1));
            var road = B.MeshObj("Road", root, RibbonMesh("T_Road", all, 4.6f, 0.03f, 0.9f, 0.18f), _tRoad, Vector3.zero, Vector3.one, null, false);
            road.GetComponent<MeshRenderer>().receiveShadows = true;
            MarkStatic(road);
            // Piles under the causeway where the road leaves the island.
            for (int i = 0; i < all.Count; i += 4)
            {
                if (OnIsland(all[i], -1f)) continue;
                Vector3 fwd = all[Mathf.Min(all.Count - 1, i + 1)] - all[Mathf.Max(0, i - 1)];
                fwd.y = 0f;
                var right = new Vector3(fwd.z, 0f, -fwd.x).normalized;
                foreach (var s in new[] { -1f, 1f })
                    B.Cyl("Pile", root, _mWood, all[i] + right * s * 2.1f + Vector3.down * 1.2f, 0.34f, 2.4f);
            }

            var arriveT = new List<Transform>();
            var pathRoot = B.Node("Path", root, Vector3.zero).transform;
            for (int i = 0; i < inPts.Count - 1; i++) arriveT.Add(B.Node("In" + i, pathRoot, inPts[i]).transform);
            var departT = new List<Transform>();
            for (int i = 1; i < outPts.Count; i++) departT.Add(B.Node("Out" + i, pathRoot, outPts[i]).transform);
            var parkT = B.Node("Park", pathRoot, park, new Vector3(0f, 0f, 0f)).transform;
            // The truck arrives heading north.
            parkT.rotation = Quaternion.LookRotation(Vector3.forward);

            // Bay (revealed by the unlock): loading pad, booth, bay markings, crates.
            var bayGo = B.Node("Bay", root, new Vector3(15.2f, 0f, -5.4f));
            revealRoot = bayGo;
            var bt = bayGo.transform;
            loadZone = MakeZone<DeliveryZone>("LoadZone", bt, new Vector3(15.2f, 0f, -5.4f), new Vector2(2.6f, 2.4f), padMat, _sTruck);
            var booth = B.Node("Booth", bt, new Vector3(-0.8f, 0f, -3.6f)).transform;
            RB("Hut", booth, _tBamboo, new Vector3(0f, 0.9f, 0f), new Vector3(1.8f, 1.8f, 1.5f), 0.2f);
            B.MeshObj("Roof", booth, _shThatch, _tThatch, new Vector3(0f, 1.85f, 0f), new Vector3(2.8f, 1.8f, 2.4f));
            // Order sign beside the truck stop (replaces the old floating board over the truck).
            var orderBoard = BuildOrderBoard(bt, new Vector3(0f, 0f, 3.3f));
            var lineMat = MatLib.Lit("BayLine", new Color(1f, 0.95f, 0.85f), 0.1f);
            foreach (var sx in new[] { -1.6f, 1.6f })
                B.Box("BayLine", bt, lineMat, new Vector3(3.2f + sx, 0.05f, 0.4f), new Vector3(0.14f, 0.02f, 6.2f), null, false);
            // Crates stacked west of the board so they never cover it.
            for (int i = 0; i < 3; i++)
                B.Prop("Survival/box", bt, new Vector3(-2.7f + (i % 2) * 0.1f, i == 2 ? 0.8f : 0f, -0.3f + (i % 2) * 0.9f), 2.6f, i * 25f);
            TikiTorch(bt, new Vector3(1.4f, 0f, 1.6f));

            // Trucks (one reusable per kind) live outside the reveal root so they are not scaled by it.
            var trucksRoot = B.Node("Trucks", _actors, Vector3.zero).transform;
            var trucks = new DeliveryTruck[4];
            for (int k = 0; k < 4; k++) trucks[k] = BuildTruck((TruckKind)k, trucksRoot);

            var bay = bayGo.AddComponent<DeliveryBay>();
            bay.arrivePath = arriveT.ToArray();
            bay.park = parkT;
            bay.departPath = departT.ToArray();
            bay.loadZone = loadZone;
            bay.trucks = trucks;
            bay.board = orderBoard;
            Busy(17f, -4f, 5.5f);
            Busy(20f, -14f, 4f);
            Busy(19.5f, 4f, 4f);
            Busy(25f, 10f, 4f);
            return bay;
        }

        // ================================================================== unlock pads (tropical glow style)

        /// <summary>Size of the next glow unlock pads (smaller where space is tight, e.g. Berry Blast farmhands).</summary>
        static float _tPadSize = 2.9f;

        static UnlockZone TUnlock(string id, string title, int price, Vector3 pos, Sprite icon, GameObject[] reveal, string subtitle, params UnlockZone[] next)
        {
            var go = B.Node("Unlock_" + id, _unlocks, pos);
            var size = new Vector2(_tPadSize, _tPadSize);
            Busy(pos.x, pos.z, _tPadSize * 0.8f);
            var pad = B.Node("Pad", go.transform, Vector3.zero, null, new Vector3(size.x, 1f, size.y));
            // Soft glowing disc, breathing gold ring, radial progress, big icon and price.
            B.Decal("Base", pad.transform, _tGlowBase, new Vector3(0f, 0.03f, 0f), Vector2.one * 1.25f).GetComponent<MeshRenderer>().sortingOrder = -1;
            var glow = B.Sprite("Glow", go.transform, _sGlowRing, new Vector3(0f, 0.04f, 0f), SpriteScale(_sGlowRing, size.x * 1.12f), true, 0, new Color(1f, 0.78f, 0.2f, 1f));

            var canvasGo = new GameObject("Progress", typeof(RectTransform));
            canvasGo.transform.SetParent(go.transform, false);
            var crt = (RectTransform)canvasGo.transform;
            crt.localPosition = new Vector3(0f, 0.05f, 0f);
            crt.localRotation = Quaternion.Euler(90f, 0f, 0f);
            crt.sizeDelta = new Vector2(300f, 300f);
            crt.localScale = Vector3.one * (size.x * 0.92f / 300f);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;
            var track = Img(Stretch("Track", crt), _sRingProgress, new Color(0.3f, 0.2f, 0.1f, 0.5f));
            _ = track;
            var ring = Img(Stretch("Fill", crt), _sRingProgress, new Color(0.45f, 1f, 0.5f, 0.95f));
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;

            var gIcon = B.Sprite("GroundIcon", go.transform, icon, new Vector3(0f, 0.06f, 0.28f), SpriteScale(icon, 1.0f), true, 2);
            _ = gIcon;
            var coin = B.Sprite("Coin", go.transform, _sCoin, new Vector3(-0.58f, 0.06f, -0.5f), SpriteScale(_sCoin, 0.42f), true, 3);
            _ = coin;
            var gPrice = B.Text("GroundPrice", go.transform, price.ToString(), 8f, Color.white, new Vector3(0.2f, 0.07f, -0.5f));
            gPrice.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gPrice.rectTransform.sizeDelta = new Vector2(1.6f, 0.6f);
            gPrice.enableAutoSizing = true;
            gPrice.fontSizeMin = 4f;
            gPrice.fontSizeMax = 8f;
            gPrice.GetComponent<MeshRenderer>().sortingOrder = 3;

            var label = B.Node("Label", go.transform, new Vector3(0f, 1.45f, 0f));
            label.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", label.transform, _sRound, new Vector3(0f, 0f, 0.02f), 1f, false, 4, new Color(0.1f, 0.08f, 0.12f, 0.72f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(2.8f, 0.72f);
            var ic = B.Sprite("Icon", label.transform, icon, new Vector3(-1.1f, 0.02f, 0f), SpriteScale(icon, 0.46f), false, 5);
            // Title fills the space right of the icon so the two never overlap.
            var titleT = B.Text("Title", label.transform, title, 4.4f, Color.white, new Vector3(0.26f, 0.02f, 0f));
            titleT.rectTransform.sizeDelta = new Vector2(2.05f, 0.6f);
            titleT.enableAutoSizing = true;
            titleT.fontSizeMin = 2.4f;
            titleT.fontSizeMax = 4.4f;
            var priceT = B.Text("Price", label.transform, "$" + price, 6.5f, new Color(1f, 0.9f, 0.35f), new Vector3(0.26f, 0.02f, 0f));
            priceT.rectTransform.sizeDelta = new Vector2(2.05f, 0.7f);
            priceT.enableAutoSizing = true;
            priceT.fontSizeMin = 3f;
            priceT.fontSizeMax = 6.5f;
            priceT.gameObject.SetActive(false);

            var z = go.AddComponent<UnlockZone>();
            z.id = id;
            z.title = title;
            z.price = price;
            z.size = size;
            z.padVisual = pad.transform;
            z.reveal = reveal;
            z.next = next;
            z.priceText = priceT;
            z.groundPriceText = gPrice;
            z.titleText = titleT;
            z.icon = ic;
            z.label = label.transform;
            z.radialFill = ring;
            z.glow = glow;
            z.subtitle = subtitle;
            _zones.Add(z);
            return z;
        }

        // ================================================================== decor

        static GameObject BuildCabana(Vector3 pos)
        {
            var root = B.Node("Cabana", _decor, pos);
            var t = root.transform;
            var rugMat = MatLib.Lit("T_DeckCabana", new Color(1f, 0.95f, 0.88f), 0.1f, MatLib.Tex("wood.png"), new Vector2(5f / 3.2f, 4.2f / 3.2f));
            B.MeshObj("Rug", t, _disc, new[] { rugMat, _mWood }, new Vector3(0f, 0.01f, 0f), new Vector3(5f, 0.04f, 4.2f), null, false);
            ThatchShelter(t, new Vector3(0f, 0f, 0.4f), new Vector2(3.4f, 2.6f), 2.5f);
            var towels = new[] { MatLib.Lit("Towel_A", new Color(0.3f, 0.75f, 0.95f), 0.1f), MatLib.Lit("Towel_B", new Color(1f, 0.5f, 0.6f), 0.1f) };
            Lounger(t, new Vector3(-0.8f, 0f, 0.3f), 180f, towels[0]);
            Lounger(t, new Vector3(0.8f, 0f, 0.3f), 180f, towels[1]);
            B.Prop("Furniture/tableRound", t, new Vector3(0f, 0f, -1.3f), 0.18f);
            B.MeshObj("Drink", t, _cup, _mJuice[3], new Vector3(0f, 0.72f, -1.3f), new Vector3(0.22f, 0.26f, 0.22f));
            TikiTorch(t, new Vector3(-2.2f, 0f, -1.6f));
            TikiTorch(t, new Vector3(2.2f, 0f, -1.6f));
            Busy(pos.x, pos.z, 3.4f);
            return root;
        }

        static void BuildTropicalDecor()
        {
            var trees = B.Node("Trees", _nature, Vector3.zero).transform;
            var small = B.Node("Small", _nature, Vector3.zero).transform;

            // Jungle ring: palms along the shore, dense canopy inland to the north.
            for (int i = 0; i < 70; i++)
            {
                float a = i / 70f * Mathf.PI * 2f + (float)_rnd.NextDouble() * 0.05f;
                float edge = IslandRadius(a);
                float r = edge - 3.5f - (float)_rnd.NextDouble() * 5f;
                var p = IslandCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                // Leave the south beach, hut front and the road clear.
                if (p.z < -13f || TBusy(p)) continue;
                int kind = r < edge - 6.5f && p.z > 10f ? (_rnd.Next(3) == 0 ? 1 : 0) : 0;
                DecorPlant(kind, trees, p, 0.95f + (float)_rnd.NextDouble() * 0.35f);
            }
            // Inner jungle clumps.
            Vector3[] clumps =
            {
                new Vector3(-26f, 0f, 14f), new Vector3(-22f, 0f, 28f), new Vector3(-10f, 0f, 30f), new Vector3(4f, 0f, 29f), new Vector3(16f, 0f, 25f),
                new Vector3(26f, 0f, 18f), new Vector3(-27f, 0f, 2f), new Vector3(12f, 0f, 18.5f), new Vector3(-10.5f, 0f, 21f)
            };
            foreach (var c in clumps)
            {
                for (int k = 0; k < 4; k++)
                {
                    var p = c + new Vector3(((float)_rnd.NextDouble() - 0.5f) * 5f, 0f, ((float)_rnd.NextDouble() - 0.5f) * 5f);
                    if (!OnIsland(p, 3f) || TBusy(p)) continue;
                    DecorPlant(_rnd.Next(4), trees, p, 0.9f + (float)_rnd.NextDouble() * 0.4f);
                }
            }

            // Ferns, flowers and rocks.
            for (int i = 0; i < 70; i++)
            {
                var p = IslandCenter + new Vector3(((float)_rnd.NextDouble() - 0.5f) * 62f, 0f, ((float)_rnd.NextDouble() - 0.5f) * 62f);
                if (!OnIsland(p, 4f) || TBusy(p) || (p.z < -12f)) continue;
                int k = _rnd.Next(5);
                if (k < 2) Fern(small, p, 0.8f + (float)_rnd.NextDouble() * 0.5f);
                else if (k < 4) Hibiscus(small, p);
                else RockCluster(small, p, 0.7f);
            }
            for (int i = 0; i < 10; i++)
            {
                float a = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                var p = IslandCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (IslandRadius(a) - 1.2f);
                if (TBusy(p)) continue;
                RockCluster(small, p, 1.2f);
            }

            // A pair of tiki torches marks the path up to the banana plantation.
            foreach (var p in new[] { new Vector3(-2f, 0f, 12.5f), new Vector3(2f, 0f, 12.5f) })
                TikiTorch(_decor, p);
        }

        static void BuildBeach()
        {
            var root = B.Node("Beach", _decor, Vector3.zero).transform;
            Material[] canopies =
            {
                MatLib.Lit("Canopy_Red", new Color(1f, 0.42f, 0.38f), 0.2f), MatLib.Lit("Canopy_Blue", new Color(0.3f, 0.7f, 1f), 0.2f),
                MatLib.Lit("Canopy_Yellow", new Color(1f, 0.85f, 0.3f), 0.2f), MatLib.Lit("Canopy_Green", new Color(0.4f, 0.85f, 0.5f), 0.2f)
            };
            var towels = new[] { MatLib.Lit("Towel_A", new Color(0.3f, 0.75f, 0.95f), 0.1f), MatLib.Lit("Towel_B", new Color(1f, 0.5f, 0.6f), 0.1f), MatLib.Lit("Towel_C", new Color(1f, 0.85f, 0.35f), 0.1f) };
            Vector3[] spots = { new Vector3(-16f, 0f, -20f), new Vector3(-9.5f, 0f, -23f), new Vector3(8.5f, 0f, -22.5f), new Vector3(15f, 0f, -20f), new Vector3(-2.5f, 0f, -25f) };
            for (int i = 0; i < spots.Length; i++)
            {
                if (!OnIsland(spots[i], 2.5f)) continue;
                BeachUmbrella(root, spots[i], canopies[i % canopies.Length]);
                Lounger(root, spots[i] + new Vector3(-0.8f, 0f, -0.4f), 190f, towels[i % towels.Length]);
                Lounger(root, spots[i] + new Vector3(0.8f, 0f, -0.4f), 170f, towels[(i + 1) % towels.Length]);
            }
            Surfboard(root, new Vector3(-12.5f, 0f, -17.2f), 10f, canopies[1]);
            Surfboard(root, new Vector3(-11.8f, 0f, -17.4f), -8f, canopies[2]);
            Surfboard(root, new Vector3(12f, 0f, -17.5f), 5f, canopies[0]);
            // Beach balls, sandcastle, starfish.
            B.MeshObj("BeachBall", root, _lowSphere, canopies[2], new Vector3(4.5f, 0.3f, -21f), Vector3.one * 0.6f);
            B.MeshObj("BeachBall", root, _lowSphere, canopies[1], new Vector3(-5.5f, 0.3f, -19.5f), Vector3.one * 0.6f);
            var castle = B.Node("Sandcastle", root, new Vector3(1.5f, 0f, -23.5f)).transform;
            var sandMat = MatLib.Lit("SandCastle", new Color(0.95f, 0.84f, 0.6f), 0.05f);
            B.MeshObj("Base", castle, _tower, sandMat, Vector3.zero, new Vector3(1.4f, 0.5f, 1.4f));
            foreach (var o in new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f) })
                B.MeshObj("Tower", castle, _tower, sandMat, o + Vector3.up * 0.4f, new Vector3(0.45f, 0.5f, 0.45f));
            var starMat = MatLib.Sprite("Starfish", MatLib.Tex("fx_star.png"), new Color(1f, 0.55f, 0.45f, 1f));
            for (int i = 0; i < 8; i++)
            {
                float a = (float)_rnd.NextDouble() * Mathf.PI - Mathf.PI;
                var p = IslandCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (IslandRadius(a) - 2.6f);
                if (TBusy(p)) continue;
                B.Decal("Starfish", root, starMat, new Vector3(p.x, 0.02f, p.z), Vector2.one * 0.5f, (float)_rnd.NextDouble() * 360f);
            }
            // Palms framing the beach (clear of the queue).
            foreach (var p in new[] { new Vector3(-20f, 0f, -14f), new Vector3(-17.5f, 0f, -13f), new Vector3(16f, 0f, -19f), new Vector3(-5f, 0f, -20.5f), new Vector3(6f, 0f, -24f) })
                if (OnIsland(p, 2f) && !TBusy(p)) DecorPlant(0, root, p, 1.05f);
            // Pier with a rowing boat.
            var pier = B.Node("Pier", root, new Vector3(-3.5f, 0f, -27f)).transform;
            for (int i = 0; i < 8; i++)
                B.Box("Plank", pier, _mWood, new Vector3(0f, 0.05f, -i * 0.9f), new Vector3(2.2f, 0.12f, 0.8f), null, false);
            for (int i = 0; i < 4; i++)
                foreach (var sx in new[] { -1f, 1f })
                    B.Cyl("Pile", pier, _mWood, new Vector3(sx * 1.1f, -0.4f, -i * 2.2f), 0.22f, 1.2f);
            RB("Rowboat", pier, MatLib.Lit("Rowboat", new Color(0.95f, 0.45f, 0.3f), 0.3f), new Vector3(2.4f, -0.2f, -4.5f), new Vector3(1.2f, 0.4f, 2.8f), 0.5f);
            Sway(pier.Find("Rowboat"), 2.5f, 1.1f);
            TikiTorch(root, new Vector3(-5f, 0f, -26f));
            TikiTorch(root, new Vector3(-2f, 0f, -26f));
        }

        /// <summary>Freshwater lagoon fed by a little waterfall over a rocky ledge.</summary>
        static void BuildLagoon(Vector3 c, float r, List<Renderer> water)
        {
            var root = B.Node("Lagoon", _decor, c).transform;
            var shoreMat = MatLib.Lit("T_LagoonShore", Color.white, 0.05f, MatLib.Tex("ground_path.png"), new Vector2(r * 2.5f / 3f, r * 2.2f / 3f));
            B.MeshObj("Shore", root, _disc, new[] { shoreMat, shoreMat }, new Vector3(0f, 0.004f, 0f), new Vector3(r * 2.5f, 0.02f, r * 2.2f), null, false);
            var bed = MatLib.Lit("LagoonBed", new Color(0.15f, 0.55f, 0.6f), 0.3f);
            B.MeshObj("Deep", root, _disc, new[] { bed, bed }, new Vector3(0f, 0.01f, 0f), new Vector3(r * 1.9f, 0.01f, r * 1.7f), null, false);
            var wtr = B.MeshObj("Water", root, _disc, new[] { _tLagoon, _tLagoon }, new Vector3(0f, 0.02f, 0f), new Vector3(r * 2f, 0.02f, r * 1.8f), null, false);
            water.Add(wtr.GetComponent<Renderer>());
            // Rocky ledge on the north side with a falling ribbon of water.
            var ledge = B.Node("Ledge", root, new Vector3(0f, 0f, r * 0.95f)).transform;
            for (int i = 0; i < 6; i++)
                B.MeshObj("Boulder", ledge, _lowSphere, _tRock, new Vector3(-1.8f + i * 0.72f, 0.7f + (i % 2) * 0.5f, 0.3f + (i % 3) * 0.2f), new Vector3(1.4f, 1.6f + (i % 2) * 0.6f, 1.2f));
            B.MeshObj("TopRock", ledge, _lowSphere, _tRock, new Vector3(0f, 2.2f, 0.6f), new Vector3(2.6f, 1.2f, 1.6f));
            var fall = B.MeshObj("Waterfall", ledge, WaterfallMesh(), _tWaterfall, new Vector3(0f, 0f, -0.15f), Vector3.one, null, false);
            water.Add(fall.GetComponent<Renderer>());
            // Foam where it pours over the lip and where it lands.
            var foamLit = MatLib.Lit("WaterfallFoam", new Color(0.95f, 1f, 1f), 0.3f);
            foamLit.SetColor("_EmissionColor", new Color(0.18f, 0.22f, 0.24f));
            B.MeshObj("LipFoam", ledge, _lowSphere, foamLit, new Vector3(0f, 2.3f, -0.12f), new Vector3(1f, 0.16f, 0.3f), null, false);
            foreach (var fx in new[] { -0.45f, 0f, 0.45f })
                B.MeshObj("SplashFoam", ledge, _lowSphere, foamLit, new Vector3(fx, 0.04f, -0.72f + Mathf.Abs(fx) * 0.2f), new Vector3(0.6f, 0.14f, 0.42f), null, false);
            var foamMat = MatLib.Sprite("LagoonFoam", MatLib.Tex("soft_blob.png"), new Color(1f, 1f, 1f, 0.75f));
            B.Decal("Splash", root, foamMat, new Vector3(0f, 0.035f, r * 0.62f), new Vector2(2.2f, 1.2f));
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                if (Mathf.Sin(a) > 0.55f) continue;
                var p = new Vector3(Mathf.Cos(a) * r * 1.08f, 0f, Mathf.Sin(a) * r * 0.98f);
                if (i % 3 == 0) Fern(root, p, 0.8f);
                else B.Prop("Nature/rock_smallA", root, p, 1.5f, i * 40f);
            }
            var lily = MatLib.Lit("LilyPad", new Color(0.35f, 0.72f, 0.3f), 0.3f);
            foreach (var p in new[] { new Vector3(-1f, 0f, 0.4f), new Vector3(1.2f, 0f, -0.6f), new Vector3(0.2f, 0f, -1.3f) })
            {
                var pad = B.MeshObj("Lily", root, _disc, new[] { lily, lily }, p + Vector3.up * 0.035f, new Vector3(0.6f, 0.02f, 0.6f), new Vector3(0f, (float)_rnd.NextDouble() * 360f, 0f), false);
                Sway(pad.transform, 3f, 0.8f);
            }
            var col = root.gameObject.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.4f, 0f);
            col.radius = r * 1.02f;
            Busy(c.x, c.z, r + 2.5f);
        }

        /// <summary>Re-home the butterflies built by <see cref="BuildButterflies"/> for this map.</summary>
        static void MoveButterflies(Vector3[] homes)
        {
            var bf = _decor.GetComponentInChildren<Butterflies>();
            if (bf == null) return;
            for (int i = 0; i < bf.flies.Length; i++)
            {
                var f = bf.flies[i];
                f.home = homes[i % homes.Length];
                f.body.position = f.home + Vector3.up;
                bf.flies[i] = f;
            }
        }
    }
}
