using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Expansion 2 — Berry Blast. A cozy berry village laid out in columns, so every walk is short:
    ///   south: the Berry Bar (juice counter) on a cobbled square;
    ///   the four berry presses in a row;
    ///   the four berry patches side by side (strawberry, raspberry, blueberry, cranberry);
    ///   a lane the fox sneaks along;
    ///   the Cake Hall: each berry's cake mixer, conveyor and oven straight north of its patch;
    ///   north: the pastry case, with cake customers queuing from the village street;
    ///   east: two delivery desks on a lay-by road where trucks stop for boxed orders.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        public const string BerryScenePath = "Assets/Game/Scenes/Berry.unity";

        static readonly float[] BerryX = { -9f, -3f, 3f, 9f };
        const float BerryFarmZ = 6.75f, BerryPressZ = -0.8f, BerryLaneZ = 10.3f, BerryMixerZ = 14.6f, BerryOvenZ = 18.3f;
        static readonly Vector3 BerryCounterPos = new Vector3(0f, 0f, -9.5f);
        static readonly Vector3 CakeStandPos = new Vector3(0f, 0f, 25.2f);
        static readonly Vector3 DeskAPark = new Vector3(19.3f, 0f, -5f);
        static readonly Vector3 DeskBPark = new Vector3(19.3f, 0f, 5f);
        const float BerryRoadX = 23f;

        static readonly string[] BerryPatchNames = { "Strawberry Patch", "Raspberry Patch", "Blueberry Patch", "Cranberry Bog" };
        static readonly string[] BerryIdKeys = { "straw", "rasp", "blue", "cran" };

        static void BuildBerryScene()
        {
            _rnd = new System.Random(2718);
            _zones.Clear();
            _tBusy.Clear();
            _tPadSize = 2.9f;

            BuildStationMaterials();
            BuildTropicalMaterials();
            BuildTropicalMeshes();
            BuildBerryMaterials();
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
            gm.sceneExpansion = 2;
            gm.startMoney = Economy.BerryStartMoney;
            var refs = systems.AddComponent<GameRefs>();
            systems.AddComponent<LooseItems>();
            systems.AddComponent<Boosts>();
            _ambient = systems.AddComponent<Ambient>();
            _nature = B.Node("Nature", _world, Vector3.zero).transform;
            BuildEnvMeshes();

            BuildLighting(false, true);
            var water = BuildBerryGround();

            // ---------------- the Berry Bar: counter, cash, workshop, bin
            var counter = BuildCounter(BerryCounterPos, "BERRY BAR");
            foreach (var sx in new[] { -2.9f, 2.9f }) FlowerPot(counter.transform, new Vector3(sx, 0f, 0.9f));
            var cash = BuildCashPile(new Vector3(4.4f, 0f, -8.8f));
            var upgradeStation = BuildUpgradeStation(new Vector3(-8.4f, 0f, -8.2f));
            FlowerPot(upgradeStation.transform, new Vector3(-2.2f, 0f, -1.9f));
            FlowerPot(upgradeStation.transform, new Vector3(2.2f, 0f, -1.9f));
            _trash = BuildTrashBin(new Vector3(10.8f, 0f, -8.6f), new Vector3(9f, 0f, -8.6f));
            Busy(0f, -10f, 6.5f);
            Busy(-8.4f, -9f, 3f);
            Busy(10f, -8.6f, 2.4f);
            Busy(4.4f, -8.8f, 2f);

            // ---------------- farms, presses, cake lines (one column per berry)
            var fields = new FruitField[4];
            var presses = new BerryPress[4];
            var mixers = new CakeMixer[4];
            var ovens = new Oven[4];
            for (int i = 0; i < 4; i++)
            {
                int f = 7 + i;
                fields[i] = BuildBerryFarm(f, new Vector3(BerryX[i], 0f, BerryFarmZ), new Vector2(5f, 4.5f), FruitKey[f] + "Farm", BerryPatchNames[i]);
                presses[i] = BuildBerryPress(f, new Vector3(BerryX[i], 0f, BerryPressZ));
                mixers[i] = BuildCakeMixer(f, new Vector3(BerryX[i], 0f, BerryMixerZ));
                ovens[i] = BuildOven(f, new Vector3(BerryX[i], 0f, BerryOvenZ), mixers[i]);
                Busy(BerryX[i], BerryPressZ, 3.2f);
                Busy(BerryX[i], 16.5f, 4f);
            }

            // ---------------- cake shop: pastry case, its own till and customers
            var cakeCounter = BuildPastryCase(CakeStandPos);
            var cakeCash = BuildCashPile(CakeStandPos + new Vector3(4.2f, 0f, -1.9f));
            cakeCash.name = "CakeCashPile";
            Busy(CakeStandPos.x, CakeStandPos.z, 6f);
            var cakeShop = B.Node("CakeShop", _decor, Vector3.zero);
            BuildCakeShopDressing(cakeShop.transform);

            var cm = BuildCustomers(systems.transform, counter, cash, BerryCounterPos - new Vector3(0f, 0f, -7f));
            var cakeCm = BuildCakeCustomers(systems.transform, cakeCounter, cakeCash);

            // ---------------- player & helpers
            var player = BuildPlayerObject();
            player.transform.SetParent(_actors, false);
            player.transform.position = new Vector3(0f, 0f, -5f);
            SetLayerRecursive(player, 2);

            var runner = BuildWaiter(new Vector3(6.2f, 0f, -6.4f), presses, counter, cm);
            runner.name = "Runner";
            var baker = BuildWaiter(new Vector3(-3.6f, 0f, 21.6f), new Juicer[0], cakeCounter, cakeCm);
            baker.name = "Baker";
            var bai = baker.GetComponent<WorkerAI>();
            bai.producers = ovens;
            bai.juicers = null;
            BakerHat(baker.transform);

            // ---------------- delivery: road, two desks, trucks
            var delivery = B.Node("Delivery", _world, Vector3.zero).transform;
            var trucksRoot = B.Node("Trucks", _actors, Vector3.zero).transform;
            BuildBerryRoad(delivery, out var pathA, out var parkA, out var departA, out var pathB, out var parkB, out var departB);
            var trucksA = new DeliveryTruck[4];
            var trucksB = new DeliveryTruck[4];
            for (int k = 0; k < 4; k++)
            {
                trucksA[k] = BuildBerryTruck((TruckKind)k, trucksRoot, "_A");
                trucksB[k] = BuildBerryTruck((TruckKind)k, trucksRoot, "_B");
            }
            var bayA = BuildBerryDesk(delivery, "DeskA", DeskAPark, parkA, pathA, departA, padDelivery, trucksA, out var deskARoot, out var zoneA);
            var bayB = BuildBerryDesk(delivery, "DeskB", DeskBPark, parkB, pathB, departB, padDelivery, trucksB, out var deskBRoot, out var zoneB);
            Busy(15.5f, -4f, 5.5f);
            Busy(15.5f, 6f, 5.5f);
            Busy(BerryRoadX, 0f, 3.5f);

            var loader = BuildWaiter(new Vector3(14.4f, 0f, 0.2f), presses, counter, cm);
            loader.name = "Loader";
            var lai = loader.GetComponent<WorkerAI>();
            lai.role = WorkerAI.Role.Loader;
            lai.deliveryZone = zoneA;
            lai.deliveryZones = new[] { zoneA, zoneB };
            var prod = new List<MonoBehaviour>(presses);
            prod.AddRange(ovens);
            lai.producers = prod.ToArray();
            lai.altCounter = cakeCounter;
            lai.customers = null;
            lai.idlePoint.name = "LoaderIdle";

            string[] farmerModels = { "character-male-d", "character-female-d", "character-male-f", "character-female-b" };
            var farmers = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                farmers[i] = BuildFarmer(7 + i, fields[i], presses[i], new Vector3(BerryX[i] + 1.2f, 0f, BerryLaneZ), farmerModels[i]);
                farmers[i].GetComponent<WorkerAI>().feedZones = new[] { mixers[i].inputZone };
            }

            // ---------------- fox: den, actor, restore pads
            var foxRoot = B.Node("FoxDenArea", _decor, Vector3.zero).transform;
            FoxDen(foxRoot, new Vector3(-16.2f, 0f, BerryLaneZ));
            var den = B.Node("DenPoint", _actors, new Vector3(-15f, 0f, BerryLaneZ)).transform;
            var fox = BuildFox(_actors);
            var foxFarms = new FoxRaid.Farm[4];
            for (int i = 0; i < 4; i++)
            {
                foxFarms[i] = new FoxRaid.Farm
                {
                    field = fields[i],
                    entry = B.Node("FoxEntry" + i, _actors, new Vector3(BerryX[i], 0f, BerryFarmZ + 2.6f)).transform,
                    restorePad = BuildRestorePad(new Vector3(BerryX[i] - 1.15f, 0f, BerryLaneZ), fields[i]),
                };
            }

            // ---------------- decor unlocks
            var gazebo = BuildGazebo(new Vector3(-13.6f, 0f, -5.5f));
            var picnic = BuildPicnic(new Vector3(-13.8f, 0f, 16f));

            // ---------------- unlock chain (written last-first: each pad names the pads it reveals)
            var fruitIcon = _sFruit;
            var zPremium = TUnlock("b_premium", "Premium Contracts", 45000, new Vector3(14.2f, 0f, -12.2f), _uCrown ? _uCrown : _sCrown, new GameObject[0],
                "Royal trucks bring luxury orders!");
            _tPadSize = 2.3f;
            var zFarmerCran = TUnlock("b_farmer_cran", "Cranberry Farmer", 22000, farmers[3].transform.position, _uFarmer, new[] { farmers[3] }, "Cranberries picked for you");
            _tPadSize = 2.9f;
            var zCranOven = TUnlock("b_cran_oven", "Cranberry Oven", 28000, ovens[3].transform.position, _sOven, new[] { ovens[3].gameObject },
                "Cranberry tarts - the priciest treat!", zFarmerCran, zPremium);
            var zCranMixer = TUnlock("b_cran_mixer", "Cranberry Cake Mixer", 21000, mixers[3].transform.position, _sMixer, new[] { mixers[3].gameObject },
                "Whip cranberries into tart filling", zCranOven);
            var zDesk2 = TUnlock("b_desk2", "Second Desk", 19000, zoneB.transform.position, _sTruck, new[] { deskBRoot }, "Two trucks at once!");
            _tPadSize = 2.3f;
            var zFarmerBlue = TUnlock("b_farmer_blue", "Blueberry Farmer", 13000, farmers[2].transform.position, _uFarmer, new[] { farmers[2] }, "Blueberries picked for you");
            _tPadSize = 2.9f;
            var zCranFarm = TUnlock("b_cran_farm", "Cranberry Bog", 23000, fields[3].transform.position, fruitIcon[10], new[] { fields[3].transform.parent.gameObject },
                "Glossy cranberries for the finest juice", zCranMixer, zFarmerBlue, zDesk2);
            var zCranPress = TUnlock("b_cran_press", "Cranberry Press", 17000, presses[3].transform.position, _uMachine, new[] { presses[3].gameObject },
                "Cranberry Cooler sells for top dollar", zCranFarm);
            var zPicnic = TUnlock("b_picnic", "Picnic Garden", 6000, picnic.transform.position, _uStar, new[] { picnic }, "Visitors love a picnic");
            var zLoader = TUnlock("b_loader", "Hire Loader", 14000, loader.transform.position, _uWaiter, new[] { loader }, "The loader packs the boxes for you");
            var zBlueOven = TUnlock("b_blue_oven", "Blueberry Oven", 12000, ovens[2].transform.position, _sOven, new[] { ovens[2].gameObject },
                "Blueberry cheesecake is on the menu", zLoader, zCranPress, zPicnic);
            var zBlueMixer = TUnlock("b_blue_mixer", "Blueberry Cake Mixer", 9000, mixers[2].transform.position, _sMixer, new[] { mixers[2].gameObject },
                "Blueberries into creamy batter", zBlueOven);
            var zBaker = TUnlock("b_baker", "Hire Baker", 11000, baker.transform.position, _uChef ? _uChef : _uWaiter, new[] { baker }, "The baker serves the cake queue");
            _tPadSize = 2.3f;
            var zFarmerRasp = TUnlock("b_farmer_rasp", "Raspberry Farmer", 8000, farmers[1].transform.position, _uFarmer, new[] { farmers[1] }, "Raspberries picked for you");
            _tPadSize = 2.9f;
            var zBlueFarm = TUnlock("b_blue_farm", "Blueberry Patch", 9500, fields[2].transform.position, fruitIcon[9], new[] { fields[2].transform.parent.gameObject },
                "Plump blueberries, bigger profits", zBlueMixer, zFarmerRasp, zBaker);
            var zBluePress = TUnlock("b_blue_press", "Blueberry Press", 7000, presses[2].transform.position, _uMachine, new[] { presses[2].gameObject },
                "Blueberry Shake unlocked", zBlueFarm);
            var zRaspOven = TUnlock("b_rasp_oven", "Raspberry Oven", 10000, ovens[1].transform.position, _sOven, new[] { ovens[1].gameObject }, "Raspberry velvet cakes!");
            var zRaspMixer = TUnlock("b_rasp_mixer", "Raspberry Cake Mixer", 7500, mixers[1].transform.position, _sMixer, new[] { mixers[1].gameObject },
                "Raspberries into pink batter", zRaspOven);
            var zDelivery = TUnlock("b_delivery", "Delivery Desk", 5500, zoneA.transform.position, _sTruck, new[] { deskARoot },
                "Pack boxes for trucks - BIG rewards!", zBluePress, zRaspMixer);
            zDelivery.peekOnUnlock = true;
            var zGazebo = TUnlock("b_gazebo", "Flower Gazebo", 2000, gazebo.transform.position, _uStar, new[] { gazebo }, "A sweet spot for the village");
            _tPadSize = 2.3f;
            var zFarmerStraw = TUnlock("b_farmer_straw", "Strawberry Farmer", 4500, farmers[0].transform.position, _uFarmer, new[] { farmers[0] }, "Your first berry picker");
            _tPadSize = 2.9f;
            var zStrawOven = TUnlock("b_straw_oven", "Strawberry Oven", 3800, ovens[0].transform.position, _sOven, new[] { ovens[0].gameObject },
                "Bake your first cake!", zDelivery, zFarmerStraw, zGazebo);
            var zStrawMixer = TUnlock("b_straw_mixer", "Strawberry Cake Mixer", 2600, mixers[0].transform.position, _sMixer, new[] { mixers[0].gameObject },
                "Berries in, batter out", zStrawOven);
            var zCakeShop = TUnlock("b_cakeshop", "Berry Cake Shop", 3200, CakeStandPos + new Vector3(0f, 0f, -4.2f), _sCakeShop,
                new[] { cakeCounter.gameObject, cakeCash.gameObject, cakeShop }, "A whole new business: CAKES!", zStrawMixer);
            zCakeShop.peekOnUnlock = true;
            var zRunner = TUnlock("b_runner", "Hire Runner", 2400, runner.transform.position, _uWaiter, new[] { runner }, "Serves the queue so you can farm");
            var zRaspFarm = TUnlock("b_rasp_farm", "Raspberry Patch", 1500, fields[1].transform.position, fruitIcon[8], new[] { fields[1].transform.parent.gameObject },
                "Juicy raspberries", zRunner, zCakeShop);
            var zRaspPress = TUnlock("b_rasp_press", "Raspberry Press", 1000, presses[1].transform.position, _uMachine, new[] { presses[1].gameObject },
                "Raspberry Fizz is on the menu", zRaspFarm);
            var zUpgrades = TUnlock("b_upgrades", "Berry Workshop", 400, new Vector3(-8.4f, 0f, -8.2f), _uTools, new[] { upgradeStation }, "Upgrade farms, presses, ovens and trucks");
            zUpgrades.startVisible = true;
            zRaspPress.startVisible = true;
            foreach (var z in new[] { zRaspFarm, zBlueFarm, zCranFarm, zRaspPress, zBluePress, zCranPress, zStrawMixer, zRaspMixer, zBlueMixer, zCranMixer, zStrawOven, zRaspOven, zBlueOven, zCranOven, zDesk2 })
                z.peekOnUnlock = true;
            _ = zCakeShop;

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
            var cam = BuildCamera(player.transform, new Color(0.84f, 0.86f, 0.98f), 140f);
            BuildUI(systems.transform, out var hud, 2);

            var tut = systems.AddComponent<Tutorial>();
            tut.firstField = fields[0];
            tut.firstJuicer = presses[0];
            tut.counter = counter;
            tut.cash = cash;
            tut.upgradeZone = upgradeStation.GetComponentInChildren<UpgradeZone>(true);
            tut.firstCakeMixer = mixers[0];
            tut.firstOven = ovens[0];
            tut.cakeCounter = cakeCounter;
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

            // ---------------- delivery desks (two managers, two save slots)
            var dmA = systems.AddComponent<DeliveryManager>();
            dmA.bay = bayA;
            dmA.slot = 0;
            dmA.unlockId = "b_delivery";
            dmA.deskName = "DESK A";
            dmA.acceptsCakes = true;
            var dmB = systems.AddComponent<DeliveryManager>();
            dmB.bay = bayB;
            dmB.slot = 1;
            dmB.unlockId = "b_desk2";
            dmB.deskName = "DESK B";
            dmB.acceptsCakes = true;
            bayA.manager = dmA;
            bayB.manager = dmB;
            zoneA.manager = dmA;
            zoneB.manager = dmB;
            bayA.board.manager = dmA;
            bayB.board.manager = dmB;

            // ---------------- fox raids
            var raid = systems.AddComponent<FoxRaid>();
            raid.fox = fox;
            raid.den = den;
            raid.farms = foxFarms;
            raid.cutscene = _uiIntro;

            // ---------------- world intro
            var em = systems.AddComponent<ExpansionManager>();
            em.nextExpansion = -1;
            em.intro = _uiIntro;
            if (_uiIntro != null)
            {
                _uiIntro.title = "Welcome to Berry Blast!";
                _uiIntro.subtitle = "BERRY BLAST";
                var shots = B.Node("IntroShots", systems.transform, Vector3.zero).transform;
                _uiIntro.openingPoint = B.Node("Opening", shots, new Vector3(0f, 0f, 7f)).transform;
                _uiIntro.openingDistance = 2.3f;
                _uiIntro.demoTruck = trucksA[(int)TruckKind.JuiceTruck];
                _uiIntro.demoPark = parkA;
                _uiIntro.shots = new[]
                {
                    new ExpansionIntro.Shot { point = B.Node("Farms", shots, new Vector3(0f, 0f, BerryFarmZ)).transform, caption = "Grow strawberries, raspberries, blueberries & cranberries", distance = 1.2f, hold = 1.9f,
                        preview = new[] { fields[1].transform.parent.gameObject, fields[2].transform.parent.gameObject, fields[3].transform.parent.gameObject } },
                    new ExpansionIntro.Shot { point = B.Node("Presses", shots, new Vector3(0f, 0f, -1.5f)).transform, caption = "Press them into four berry drinks", distance = 1.1f, hold = 1.6f,
                        preview = new[] { presses[1].gameObject, presses[2].gameObject, presses[3].gameObject } },
                    new ExpansionIntro.Shot { point = B.Node("Cakes", shots, new Vector3(0f, 0f, 18f)).transform, caption = "Bake berry cakes in the new Cake Shop", distance = 1.15f, hold = 1.8f,
                        preview = new[] { mixers[0].gameObject, ovens[0].gameObject, mixers[1].gameObject, ovens[1].gameObject, mixers[2].gameObject, ovens[2].gameObject, mixers[3].gameObject, ovens[3].gameObject, cakeCounter.gameObject, cakeShop } },
                    new ExpansionIntro.Shot { point = B.Node("Desks", shots, new Vector3(17f, 0f, 0f)).transform, caption = "Pack boxes for the delivery trucks", distance = 1.25f, hold = 1.8f, showTruck = true,
                        preview = new[] { deskARoot, deskBRoot } },
                    new ExpansionIntro.Shot { point = B.Node("Den", shots, new Vector3(-14f, 0f, BerryLaneZ)).transform, caption = "...and keep an eye out for the fox!", distance = 1f, hold = 1.5f },
                    new ExpansionIntro.Shot { point = B.Node("Bar", shots, new Vector3(0f, 0f, -9.5f)).transform, caption = "Your Berry Bar opens now. Build the Berry Cake Empire!", distance = 1f, hold = 1.7f },
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

            // ---------------- living village
            BuildBerryDecor(water);
            BuildButterflies();
            MoveButterflies(new[]
            {
                new Vector3(-12f, 0f, 4f), new Vector3(12f, 0f, 4f), new Vector3(-6f, 0f, 10.5f), new Vector3(6f, 0f, 10.5f),
                new Vector3(-13f, 0f, 20f), new Vector3(13f, 0f, 18f), new Vector3(-12f, 0f, -12f), new Vector3(8f, 0f, -13f)
            });
            BuildBirds(_decor, new[] { new Vector3(-10f, 0f, 30f), new Vector3(12f, 0f, 32f), new Vector3(-20f, 0f, 8f) }, false);
            BuildClouds();
            _ambient.water = new Renderer[0];

            PolishBerry();

            // ---------------- mobile optimisation
            foreach (var t in new[] { _decor, _nature, _stations })
                foreach (var tr in t.GetComponentsInChildren<Transform>(true))
                    if (tr.name == "Flowers" || tr.name == "FlowerPot" || tr.name == "Rocks" || tr.name == "Mushroom" || tr.name == "Bloom" || tr.name == "Petals")
                        B.NoShadows(tr.gameObject);
            OptimizeScene("Berry");
            foreach (var node in _stations.GetComponentsInChildren<BerryBushNode>(true))
                CombineUnder(node.visual);
            MarkStatic(_decor.gameObject);
            MarkStatic(_nature.gameObject);
            foreach (var n in new[] { "Parrots", "Butterflies", "CloudShadows", "Rabbits" })
                foreach (Transform c in _decor)
                    if (c.name == n) ClearStatic(c.gameObject);
            foreach (var w in _decor.GetComponentsInChildren<Wanderer>(true)) ClearStatic(w.gameObject);
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
            EditorSceneManager.SaveScene(scene, BerryScenePath);
            SetBuildScenes();
        }

        // ================================================================== ground

        /// <summary>Meadow, cobbled squares, the bakery floor, soft paths, the village lane and a pond. Returns the water renderers.</summary>
        static List<Renderer> BuildBerryGround()
        {
            var root = B.Node("Ground", _world, Vector3.zero).transform;
            var water = new List<Renderer>();
            var meadow = B.Box("Meadow", root, _bMeadow, new Vector3(0f, -0.05f, 8f), new Vector3(170f, 0.1f, 170f), null, false);
            meadow.AddComponent<BoxCollider>();
            meadow.isStatic = true;

            // Flat slabs (unit-cube UVs, so tiling = size / tile metres).
            Material Cobble(string key, float w, float d) => MatLib.Lit("B_Cobble_" + key, Color.white, 0.18f, MatLib.Tex("ground_cobble.png"), new Vector2(w / 3f, d / 3f));
            void Slab(string n, Material m, Vector3 c, Vector2 size, float h) => B.Box(n, root, m, c + Vector3.up * (h * 0.5f), new Vector3(size.x, h, size.y), null, false);
            // Juice square: warm pavers (the cobbles read too bright and busy under the bar and pads).
            var paverMat = MatLib.Lit("B_Pavers", new Color(0.93f, 0.92f, 0.9f), 0.14f, MatLib.Tex("ground_pavers.png"), new Vector2(23.5f / 2.4f, 12.5f / 2.4f));
            Slab("Square", paverMat, new Vector3(-0.75f, 0f, -7f), new Vector2(23.5f, 12.5f), 0.024f);
            Slab("Lane", Cobble("Lane", 60f, 3.6f), new Vector3(0f, 0f, -16.9f), new Vector2(60f, 3.6f), 0.02f);
            Slab("NorthStreet", Cobble("North", 44f, 4.4f), new Vector3(0f, 0f, 31.6f), new Vector2(44f, 4.4f), 0.014f);
            Slab("CakePlaza", Cobble("CakePlaza", 12f, 6f), new Vector3(0f, 0f, 28f), new Vector2(12f, 6f), 0.026f);
            // Desk yard runs from the square's east edge to the loading apron: no overlap, so no z-fighting.
            Slab("DeskYard", Cobble("Desks", 6.4f, 22f), new Vector3(14.2f, 0f, 0f), new Vector2(6.4f, 22f), 0.028f);
            // Cake Hall floor: soft pink tiles under the mixers, conveyors and ovens.
            var floorMat = MatLib.Lit("B_BakeryFloor", new Color(1f, 0.93f, 0.9f), 0.15f, MatLib.Tex("ground_tiles.png"), new Vector2(24.5f / 3.5f, 12.2f / 3.5f));
            Slab("BakeryFloor", floorMat, new Vector3(0f, 0f, 17.5f), new Vector2(24.5f, 12.2f), 0.024f);

            // Soft dirt paths: the fox lane, between the patches, to the desks and the side gardens.
            var pathMat = MatLib.Sprite("B_PathBlob", MatLib.Tex("soft_blob.png"), new Color(0.84f, 0.7f, 0.52f, 0.6f));
            void Path(float size, params Vector3[] pts)
            {
                foreach (var p in Spline(pts, 1.1f))
                {
                    float s = size + (float)_rnd.NextDouble() * 0.5f;
                    var d = B.Decal("PathBlob", root, pathMat, new Vector3(p.x, 0.018f, p.z), new Vector2(s, s), (float)_rnd.NextDouble() * 360f);
                    d.GetComponent<MeshRenderer>().sortingOrder = -10;
                }
            }
            Path(2.8f, new Vector3(-15.5f, 0, BerryLaneZ), new Vector3(0f, 0, BerryLaneZ), new Vector3(12.5f, 0, BerryLaneZ));
            foreach (var x in new[] { -12f, -6f, 0f, 6f, 12f })
                Path(1.8f, new Vector3(x, 0, 1.2f), new Vector3(x, 0, 4.5f), new Vector3(x, 0, 9f));
            Path(2.4f, new Vector3(12.5f, 0, -3f), new Vector3(12.5f, 0, 3f), new Vector3(12.5f, 0, 9f));
            Path(2.2f, new Vector3(-12.5f, 0, -2f), new Vector3(-13.5f, 0, 3f), new Vector3(-13.8f, 0, 9f));
            Path(2.2f, new Vector3(-12.8f, 0, 12f), new Vector3(-13.6f, 0, 16f), new Vector3(-12.5f, 0, 21f));

            // Lily pond beside the fox's thicket.
            var pond = B.Node("Pond", _decor, new Vector3(-15.2f, 0f, 2.2f)).transform;
            B.MeshObj("Shore", pond, _disc, new[] { _mPath, _mPath }, new Vector3(0f, 0.004f, 0f), new Vector3(5.2f, 0.02f, 4.4f), null, false);
            var wtr = B.MeshObj("Water", pond, _disc, new[] { _mWater, _mWater }, new Vector3(0f, 0.012f, 0f), new Vector3(4.4f, 0.03f, 3.6f), null, false);
            water.Add(wtr.GetComponent<Renderer>());
            foreach (var p in new[] { new Vector3(-0.8f, 0f, 0.4f), new Vector3(0.9f, 0f, -0.5f), new Vector3(0.2f, 0f, 0.9f) })
            {
                var pad = B.MeshObj("Lily", pond, _disc, new[] { _mLily, _mLily }, p + Vector3.up * 0.045f, new Vector3(0.6f, 0.02f, 0.6f), new Vector3(0f, (float)_rnd.NextDouble() * 360f, 0f), false);
                Sway(pad.transform, 3f, 0.8f);
            }
            B.MeshObj("LilyBloom", pond, LowSphere(), _bFlowerPink, new Vector3(-0.75f, 0.12f, 0.4f), new Vector3(0.22f, 0.14f, 0.22f), null, false);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                B.Prop(i % 2 == 0 ? "Nature/rock_smallA" : "Nature/rock_smallB", pond, new Vector3(Mathf.Cos(a) * 2.5f, 0f, Mathf.Sin(a) * 2.1f), 1.3f, i * 37f);
            }
            var pc = pond.gameObject.AddComponent<SphereCollider>();
            pc.center = new Vector3(0f, 0.4f, 0f);
            pc.radius = 2.3f;
            Busy(-15.2f, 2.2f, 3.5f);

            // Invisible walls round the village.
            var bounds = B.Node("Bounds", _world, Vector3.zero).transform;
            void Wall(Vector3 c, Vector3 size) => B.Node("Wall", bounds, c).AddComponent<BoxCollider>().size = size;
            Wall(new Vector3(-17.5f, 1f, 8f), new Vector3(0.6f, 2f, 56f));
            Wall(new Vector3(21.6f, 1f, 8f), new Vector3(0.6f, 2f, 56f));
            Wall(new Vector3(2f, 1f, -18.9f), new Vector3(40f, 2f, 0.6f));
            Wall(new Vector3(2f, 1f, 34.2f), new Vector3(40f, 2f, 0.6f));
            return water;
        }

        /// <summary>The delivery road (north-south, east of the desks) with a lay-by for each desk.</summary>
        static void BuildBerryRoad(Transform root, out Transform[] arriveA, out Transform parkA, out Transform[] departA,
            out Transform[] arriveB, out Transform parkB, out Transform[] departB)
        {
            float x = BerryRoadX;
            var main = new[] { new Vector3(x, 0f, -70f), new Vector3(x, 0f, 70f) };
            var road = B.MeshObj("Road", root, RibbonMesh("B_Road", Spline(main, 2f), 4.6f, 0.03f, 0.9f, 0.18f), _tRoad, Vector3.zero, Vector3.one, null, false);
            road.GetComponent<MeshRenderer>().receiveShadows = true;
            MarkStatic(road);

            var paths = B.Node("Path", root, Vector3.zero).transform;
            void Bay(string key, Vector3 park, out Transform[] arrive, out Transform parkT, out Transform[] depart)
            {
                var inCtrl = new[] { new Vector3(x, 0f, -70f), new Vector3(x, 0f, park.z - 9f), new Vector3(x - 1.5f, 0f, park.z - 4.2f), park };
                var outCtrl = new[] { park, new Vector3(park.x + 0.3f, 0f, park.z + 3.4f), new Vector3(x - 0.8f, 0f, park.z + 6.6f), new Vector3(x, 0f, park.z + 13f), new Vector3(x, 0f, 70f) };
                var inPts = Spline(inCtrl, 1.2f);
                var outPts = Spline(outCtrl, 1.2f);
                var a = new List<Transform>();
                for (int i = 0; i < inPts.Count - 1; i++) a.Add(B.Node(key + "In" + i, paths, inPts[i]).transform);
                var d = new List<Transform>();
                for (int i = 1; i < outPts.Count; i++) d.Add(B.Node(key + "Out" + i, paths, outPts[i]).transform);
                parkT = B.Node(key + "Park", paths, park).transform;
                parkT.rotation = Quaternion.LookRotation(Vector3.forward);
                arrive = a.ToArray();
                depart = d.ToArray();
            }
            Bay("A", DeskAPark, out arriveA, out parkA, out departA);
            Bay("B", DeskBPark, out arriveB, out parkB, out departB);

            // One flat loading apron for both bays, west of the road: plain asphalt (no lane marks), tapered ends, a
            // white bay outline per desk and a cream kerb against the desk yard. Its top sits 1 cm under the road's.
            float west = DeskAPark.x - 1.9f, east = x - 1.9f;
            float south = DeskAPark.z - 8.5f, north = DeskBPark.z + 8f;
            var apronMat = MatLib.Lit("B_Apron", new Color(0.47f, 0.46f, 0.5f), 0.12f);
            var apron = B.MeshObj("Apron", root, ApronMesh("B_Apron", west, east, south, north, 3.2f, 0.02f), apronMat, Vector3.zero, Vector3.one, null, false);
            apron.GetComponent<MeshRenderer>().receiveShadows = true;
            MarkStatic(apron);
            var line = MatLib.Lit("B_BayLine", new Color(0.96f, 0.95f, 0.9f), 0.1f);
            foreach (var park in new[] { DeskAPark, DeskBPark })
            {
                const float lw = 0.12f, bx = 1.45f, bz = 3.3f;
                B.Box("BayLine", root, line, new Vector3(park.x - bx, 0.026f, park.z), new Vector3(lw, 0.012f, bz * 2f), null, false);
                foreach (var sz in new[] { -1f, 1f })
                    B.Box("BayLine", root, line, new Vector3(park.x - 0.25f, 0.026f, park.z + sz * bz), new Vector3(bx * 2f - 0.5f, 0.012f, lw), null, false);
            }
            var kerb = MatLib.Lit("B_Kerb", new Color(0.88f, 0.84f, 0.76f), 0.1f);
            RB("Kerb", root, kerb, new Vector3(west - 0.06f, 0.05f, (south + north) * 0.5f), new Vector3(0.22f, 0.1f, north - south - 6.4f), 0.04f, false);
        }

        /// <summary>Flat trapezoid slab: full length on the road side, shortened by <paramref name="taper"/> at each end on the west.</summary>
        static Mesh ApronMesh(string name, float west, float east, float south, float north, float taper, float top)
        {
            // Only 2 cm thick, so the top face is enough (the kerb hides the west edge).
            var v = new[] { new Vector3(west, top, south + taper), new Vector3(west, top, north - taper), new Vector3(east, top, north), new Vector3(east, top, south) };
            var tris = new List<int> { 0, 1, 2, 0, 2, 3 };
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++) uv[i] = new Vector2(v[i].x / 3f, v[i].z / 3f);
            var m = new Mesh { name = name };
            m.vertices = v;
            m.uv = uv;
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        // ================================================================== customers

        /// <summary>Cake customers come along the north street, queue at the pastry case's north side and leave east.</summary>
        static CustomerManager BuildCakeCustomers(Transform systems, Counter counter, CashPile cash)
        {
            var root = B.Node("CakeCustomers", systems, Vector3.zero);
            var cm = root.AddComponent<CustomerManager>();
            cm.line = ProductLine.Cake;
            cm.prefabs = _cakeCustomerPrefabs.ToArray();
            cm.counter = counter;
            cm.cash = cash;
            var c = CakeStandPos;
            Vector3[] entry = { new Vector3(-20f, 0f, 32.2f), new Vector3(-3.4f, 0f, 32.2f) };
            Vector3[] queue =
            {
                c + new Vector3(0f, 0f, 1.35f), c + new Vector3(0f, 0f, 2.45f), c + new Vector3(0f, 0f, 3.55f), c + new Vector3(0f, 0f, 4.65f),
                c + new Vector3(-1.1f, 0f, 5.9f), c + new Vector3(-2.2f, 0f, 6.3f)
            };
            Vector3[] exit = { c + new Vector3(1.8f, 0f, 1.8f), c + new Vector3(3.4f, 0f, 6.8f), new Vector3(20f, 0f, 32.4f) };
            cm.entryPath = Points(root.transform, "Entry", entry);
            cm.queueSlots = Points(root.transform, "Queue", queue);
            cm.exitPath = Points(root.transform, "Exit", exit);
            return cm;
        }

        // ================================================================== dressing

        static void FlowerPot(Transform parent, Vector3 pos)
        {
            var t = B.Node("FlowerPot", parent, pos).transform;
            B.MeshObj("Pot", t, _cup, _bBrick, Vector3.zero, new Vector3(0.5f, 0.42f, 0.5f));
            B.MeshObj("Soil", t, _disc, new[] { _mSoilDark, _mSoilDark }, new Vector3(0f, 0.4f, 0f), new Vector3(0.48f, 0.02f, 0.48f), null, false);
            Material[] blooms = { _bFlowerPink, _bFlowerYellow, _bFlowerWhite };
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                B.MeshObj("Bloom", t, LowSphere(), blooms[i % 3], new Vector3(Mathf.Cos(a) * 0.12f, 0.55f + (i % 2) * 0.06f, Mathf.Sin(a) * 0.12f), Vector3.one * 0.16f, null, false);
            }
            B.MeshObj("Leaves", t, _shLeafBall[0], _bLeaf[7], new Vector3(0f, 0.46f, 0f), new Vector3(0.46f, 0.22f, 0.46f), null, false);
        }

        /// <summary>Baker helper wears a white chef's toque.</summary>
        static void BakerHat(Transform worker)
        {
            var anim = worker.GetComponentInChildren<Animator>();
            var head = anim != null ? B.Find(anim.transform, "head") : null;
            var hat = B.Node("ChefHat", worker, new Vector3(0f, 1.45f, 0f));
            B.MeshObj("Band", hat.transform, _disc, new[] { _mWhite, _mWhite }, Vector3.zero, new Vector3(0.34f, 0.1f, 0.34f));
            B.MeshObj("Puff", hat.transform, LowSphere(), _mWhite, new Vector3(0f, 0.2f, 0f), new Vector3(0.44f, 0.32f, 0.44f));
            hat.transform.SetParent(head != null ? head : worker, true);
        }

        /// <summary>Glowing pad in front of a patch, shown only while the fox has wrecked it.</summary>
        static FoxRestoreZone BuildRestorePad(Vector3 pos, FruitField field)
        {
            var go = B.Node("RestorePad_" + field.kind, _unlocks, pos);
            var size = new Vector2(2f, 1.7f);
            var pad = B.Node("Pad", go.transform, Vector3.zero, null, new Vector3(size.x, 1f, size.y));
            B.Decal("Base", pad.transform, MatLib.Sprite("B_RestoreBase", MatLib.Tex("soft_blob.png"), new Color(1f, 0.55f, 0.35f, 0.7f)),
                new Vector3(0f, 0.03f, 0f), Vector2.one * 1.3f).GetComponent<MeshRenderer>().sortingOrder = -1;
            var glow = B.Sprite("Glow", go.transform, _sGlowRing, new Vector3(0f, 0.04f, 0f), SpriteScale(_sGlowRing, 2.2f), true, 0, new Color(1f, 0.45f, 0.3f, 1f));
            B.Sprite("Fox", go.transform, _sFox, new Vector3(0f, 0.06f, 0f), SpriteScale(_sFox, 0.95f), true, 2);
            var label = B.Node("Label", go.transform, new Vector3(0f, 1.45f, 0f));
            label.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", label.transform, _sRound, new Vector3(0f, 0f, 0.02f), 1f, false, 4, new Color(0.35f, 0.08f, 0.06f, 0.8f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(2.3f, 0.72f);
            B.Sprite("Apple", label.transform, _sApple, new Vector3(-0.82f, 0.02f, 0f), SpriteScale(_sApple, 0.5f), false, 5);
            var timer = B.Text("Timer", label.transform, "5:00", 5f, new Color(1f, 0.9f, 0.6f), new Vector3(0.22f, 0.02f, 0f));
            timer.rectTransform.sizeDelta = new Vector2(1.6f, 0.6f);
            var z = go.AddComponent<FoxRestoreZone>();
            z.size = size;
            z.padVisual = pad.transform;
            z.field = field;
            z.timerText = timer;
            z.label = label.transform;
            z.glow = glow;
            go.SetActive(false);
            return z;
        }

        /// <summary>Awnings, bunting and a big cake sign around the pastry case, plus the bakery cottage.</summary>
        static void BuildCakeShopDressing(Transform root)
        {
            var c = CakeStandPos;
            foreach (var sx in new[] { -3.6f, 3.6f }) FlowerPot(root, c + new Vector3(sx, 0f, -1.1f));
            // Big cake sign on two posts behind the queue (north), facing the camera.
            var sign = B.Node("CakeSign", root, c + new Vector3(-5.6f, 0f, 1.6f)).transform;
            foreach (var sx in new[] { -1.3f, 1.3f }) B.Cyl("Post", sign, _bFence, new Vector3(sx, 1.4f, 0.1f), 0.14f, 2.8f);
            RB("Board", sign, _bPink, new Vector3(0f, 2.5f, 0f), new Vector3(2.9f, 1f, 0.14f), 0.14f);
            var t = B.Text("Text", sign, "CAKES!", 5.4f, Color.white, new Vector3(0.3f, 2.5f, -0.09f));
            t.rectTransform.sizeDelta = new Vector2(2f, 0.8f);
            B.Sprite("Icon", sign, _sCakeIcons[7], new Vector3(-0.95f, 2.5f, -0.09f), SpriteScale(_sCakeIcons[7], 0.7f), false, 6);
            // Bakery cottage west of the case, chimney smoking.
            var bakery = Cottage(root, c + new Vector3(-9.8f, 0f, 2.6f), 0f, _bLilac, _bRoofPlum, 4.6f, 3.4f, 2.6f);
            bakery.name = "Bakery";
            var bt = bakery.transform;
            var brand = B.Text("Brand", bt, "BAKERY", 4f, new Color(0.6f, 0.25f, 0.5f), new Vector3(0f, 2.2f, -1.78f));
            brand.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
            var aw = B.Node("Awning", bt, new Vector3(0f, 1.62f, -1.95f), new Vector3(-25f, 0f, 0f)).transform;
            B.Box("Cloth", aw, _bGingham, Vector3.zero, new Vector3(3.6f, 0.05f, 0.9f));
            var smoke = NewParticles("ChimneySmoke", bt, new Vector3(4.6f * 0.3f, 4.6f, 3.4f * 0.2f), MatLib.Sprite("Fx_Smoke", MatLib.Tex("soft_blob.png"), Color.white), 20);
            var m = smoke.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            m.startColor = new Color(1f, 1f, 1f, 0.55f);
            var e = smoke.emission;
            e.rateOverTime = 4f;
            var sh = smoke.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 12f;
            sh.radius = 0.12f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var so = smoke.sizeOverLifetime;
            so.enabled = true;
            so.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 2f)));
            FadeInOut(smoke, 0.6f);
            Busy(bt.position.x, bt.position.z, 3.6f);
        }

        static GameObject BuildGazebo(Vector3 pos)
        {
            var root = B.Node("Gazebo", _decor, pos);
            var t = root.transform;
            B.MeshObj("Floor", t, _disc, new[] { _mWood, _mWood }, Vector3.zero, new Vector3(3.6f, 0.12f, 3.6f), null, false);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                B.Cyl("Post", t, _bFence, new Vector3(Mathf.Cos(a) * 1.55f, 1.2f, Mathf.Sin(a) * 1.55f), 0.14f, 2.4f);
            }
            StripedUmbrella(t, MatLib.Lit("Canopy_Gazebo", new Color(0.95f, 0.5f, 0.68f), 0.2f), new Vector3(0f, 2.4f, 0f), new Vector3(3.8f, 1.1f, 3.8f));
            B.Prop("Furniture/tableRound", t, new Vector3(0f, 0.12f, 0f), 0.2f);
            B.MeshObj("Cake", t, _disc, new[] { _mSponge, _mCreamWhite }, new Vector3(0f, 0.86f, 0f), new Vector3(0.36f, 0.2f, 0.36f));
            B.Prop("Furniture/chair", t, new Vector3(-0.9f, 0.12f, 0f), 0.2f, 90f);
            B.Prop("Furniture/chair", t, new Vector3(0.9f, 0.12f, 0f), 0.2f, -90f);
            foreach (var p in new[] { new Vector3(-1.9f, 0f, -1.2f), new Vector3(1.9f, 0f, -1.2f) }) FlowerPot(t, p);
            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.radius = 1.6f;
            Busy(pos.x, pos.z, 3f);
            return root;
        }

        static GameObject BuildPicnic(Vector3 pos)
        {
            var root = B.Node("Picnic", _decor, pos);
            var t = root.transform;
            B.MeshObj("Blanket", t, B.Quad, _bGingham, new Vector3(0f, 0.03f, 0f), new Vector3(2.6f, 2f, 1f), new Vector3(90f, 12f, 0f), false);
            B.Prop("Market/shopping-basket", t, new Vector3(0.6f, 0.04f, 0.3f), 1.8f, 30f);
            var basket = B.Node("Treats", t, new Vector3(-0.5f, 0.05f, -0.3f)).transform;
            B.MeshObj("Plate", basket, _disc, new[] { _mWhite, _mWhite }, Vector3.zero, new Vector3(0.5f, 0.03f, 0.5f), null, false);
            for (int i = 0; i < 5; i++) Berry(basket, 7 + i % 4, new Vector3(-0.12f + (i % 3) * 0.12f, 0.08f, -0.08f + (i / 3) * 0.14f), 0.12f, i * 50f);
            BlossomTree(t, new Vector3(-1.6f, 0f, 1.4f), 1.1f, false);
            B.Prop("Nature/stump_round", t, new Vector3(1.5f, 0f, 1.1f), 1.6f);
            Busy(pos.x, pos.z, 3f);
            return root;
        }

        /// <summary>Forest ring, blossom trees, cottages, gardens, hives, rabbits, lamps and benches.</summary>
        static void BuildBerryDecor(List<Renderer> water)
        {
            var trees = B.Node("Trees", _nature, Vector3.zero).transform;
            var small = B.Node("Small", _nature, Vector3.zero).transform;
            var village = B.Node("Village", _decor, Vector3.zero).transform;
            string[] kenney = { "Nature/tree_oak", "Nature/tree_fat", "Nature/tree_default", "Survival/tree", "Survival/tree-tall", "Survival/tree-autumn" };

            bool Free(Vector3 p, float margin)
            {
                if (TBusy(p)) return false;
                // Keep the play area, roads and streets open.
                if (p.x > -17f && p.x < 21f && p.z > -18.5f && p.z < 33.6f) return false;
                if (Mathf.Abs(p.x - BerryRoadX) < 3.6f + margin) return false;
                return true;
            }
            // Forest ring: a dense belt of mixed trees round the village.
            for (int i = 0; i < 260; i++)
            {
                var p = new Vector3(-40f + (float)_rnd.NextDouble() * 80f, 0f, -34f + (float)_rnd.NextDouble() * 84f);
                if (!Free(p, 0.5f)) continue;
                float d = Mathf.Min(Mathf.Abs(p.x + 17f), Mathf.Abs(p.x - 21f), Mathf.Abs(p.z + 18.5f), Mathf.Abs(p.z - 33.6f));
                if (d > 16f && _rnd.NextDouble() < 0.6) continue;
                if (_rnd.NextDouble() < 0.28) BlossomTree(trees, p, 0.9f + (float)_rnd.NextDouble() * 0.5f, _rnd.NextDouble() < 0.4);
                else B.Prop(kenney[_rnd.Next(kenney.Length)], trees, p, 2.4f + (float)_rnd.NextDouble() * 1.4f, (float)_rnd.NextDouble() * 360f);
            }

            // Inside the ring: blossom trees along the village edges.
            foreach (var p in new[]
                     {
                         new Vector3(-15.8f, 0f, -14f), new Vector3(-16f, 0f, 6.5f), new Vector3(-15.8f, 0f, 13.5f), new Vector3(-16.2f, 0f, 26f),
                         new Vector3(-10f, 0f, 29.8f), new Vector3(9.5f, 0f, 29.8f), new Vector3(19.8f, 0f, 30f), new Vector3(12f, 0f, -14.2f), new Vector3(-6f, 0f, -14.4f)
                     })
                if (!TBusy(p)) BlossomTree(trees, p, 1f + (float)_rnd.NextDouble() * 0.3f, _rnd.NextDouble() < 0.35);

            // Cottages round the squares.
            Cottage(village, new Vector3(-15.2f, 0f, -9.2f), 90f, _bMint, _bRoofBlue);
            Cottage(village, new Vector3(14.6f, 0f, -12.9f), 0f, _bCream, _bRoofRed, 3.4f, 2.8f, 2.3f);
            Cottage(village, new Vector3(14.8f, 0f, 23.8f), -90f, _bSky, _bRoofRed);
            Cottage(village, new Vector3(-8.2f, 0f, 36.5f), 180f, _bPink, _bRoofPlum);
            Cottage(village, new Vector3(8.5f, 0f, 36.8f), 180f, _bButter, _bRoofBlue);
            Picket(village, new Vector3(-12.4f, 0f, -12.8f), new Vector3(-12.4f, 0f, -5.8f));
            Picket(village, new Vector3(12.2f, 0f, -12.8f), new Vector3(12.2f, 0f, -10.6f));
            Picket(village, new Vector3(-11f, 0f, 30f), new Vector3(-5f, 0f, 30f));
            Picket(village, new Vector3(5f, 0f, 30f), new Vector3(11f, 0f, 30f));

            // Gardens: flower beds on the square's edges and around the patches.
            var garden = B.Node("Garden", _decor, Vector3.zero).transform;
            string[] flowers = { "Nature/flower_redA", "Nature/flower_yellowA", "Nature/flower_purpleA", "Nature/flower_redB", "Nature/flower_yellowB" };
            void FlowerRow(Vector3 a, Vector3 b, int n)
            {
                var fl = B.Node("Flowers", garden, Vector3.zero).transform;
                for (int i = 0; i <= n; i++)
                {
                    var p = Vector3.Lerp(a, b, i / (float)n) + new Vector3(((float)_rnd.NextDouble() - 0.5f) * 0.3f, 0f, ((float)_rnd.NextDouble() - 0.5f) * 0.3f);
                    if (TBusy(p)) continue;
                    B.Prop(flowers[_rnd.Next(flowers.Length)], fl, p, 1.6f + (float)_rnd.NextDouble() * 0.5f, (float)_rnd.NextDouble() * 360f);
                }
            }
            FlowerRow(new Vector3(-12.2f, 0f, -1f), new Vector3(-12.2f, 0f, 9f), 12);
            FlowerRow(new Vector3(12.2f, 0f, 3f), new Vector3(12.2f, 0f, 9f), 8);
            FlowerRow(new Vector3(-12f, 0f, 12f), new Vector3(-12f, 0f, 22f), 12);
            FlowerRow(new Vector3(-11f, 0f, 29.4f), new Vector3(-5f, 0f, 29.4f), 8);
            FlowerRow(new Vector3(5f, 0f, 29.4f), new Vector3(11f, 0f, 29.4f), 8);
            FlowerRow(new Vector3(-12f, 0f, -14.9f), new Vector3(12f, 0f, -14.9f), 26);
            foreach (var p in new[] { new Vector3(-12.3f, 0f, -12.4f), new Vector3(12.3f, 0f, -2.2f), new Vector3(-12.3f, 0f, 23.4f), new Vector3(12.3f, 0f, 11.6f) })
                if (!TBusy(p)) FlowerPot(garden, p);

            // Lamps and benches.
            foreach (var p in new[] { new Vector3(-12f, 0f, -3f), new Vector3(12f, 0f, -3.4f), new Vector3(-12f, 0f, 11.2f), new Vector3(12.2f, 0f, 11.4f), new Vector3(-6f, 0f, 30f), new Vector3(6f, 0f, 30f) })
                B.Prop("Furniture/lampRoundFloor", village, p, 0.3f);
            B.Prop("Furniture/bench", village, new Vector3(-10.8f, 0f, -12.6f), 0.28f, 0f);
            B.Prop("Furniture/bench", village, new Vector3(8.2f, 0f, -13.6f), 0.28f, 0f);

            // Beehives by the patches, bees buzzing.
            Beehive(garden, new Vector3(-13.4f, 0f, 7.8f));
            Beehive(garden, new Vector3(13.2f, 0f, 12.8f));

            // Rabbits in the meadows.
            var rabbits = B.Node("Rabbits", _decor, Vector3.zero).transform;
            Rabbit(rabbits, new Vector3(-14.4f, 0f, 18f), new Vector2(3f, 8f), false);
            Rabbit(rabbits, new Vector3(-14f, 0f, -12f), new Vector2(3f, 4f), true);
            Rabbit(rabbits, new Vector3(18f, 0f, 16f), new Vector2(3f, 6f), true);

            // Small undergrowth: bushes, mushrooms and stumps along the edges.
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3(-24f + (float)_rnd.NextDouble() * 50f, 0f, -24f + (float)_rnd.NextDouble() * 64f);
                bool edge = (p.x < -16.8f || p.x > 21.2f || p.z < -18.3f || p.z > 33.8f) && Mathf.Abs(p.x - BerryRoadX) > 3.2f;
                if (!edge || TBusy(p)) continue;
                int k = _rnd.Next(4);
                string m = k == 0 ? "Nature/plant_bushLarge" : k == 1 ? "Nature/plant_bush" : k == 2 ? "Nature/mushroom_red" : "Nature/stump_round";
                var prop = B.Prop(m, small, p, 1.5f + (float)_rnd.NextDouble() * 0.6f, (float)_rnd.NextDouble() * 360f);
                if (k == 2) prop.name = "Mushroom";
            }
            _ = water;
        }
    }
}
