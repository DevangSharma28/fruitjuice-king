using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// World-aware gameplay numbers. The original farm (world 0) keeps its classic <see cref="Balance"/> formulas and
    /// upgrade fields; the Tropical Farm (world 1) runs on its own scale and a data-driven upgrade tree (<see cref="Upgrades"/>).
    /// Every system asks here instead of reading Balance directly, so a new expansion only needs a new branch.
    /// </summary>
    public static class Economy
    {
        public static int World => GameManager.I != null ? GameManager.I.data.expansion : 0;
        static SaveData D => GameManager.I != null ? GameManager.I.data : null;
        static int L(string id) => GameManager.I != null ? GameManager.I.GetUpgrade(id) : 0;

        /// <summary>Money you start the Tropical Farm with (enough to feel rich, not enough to skip the first loop).</summary>
        public const long TropicalStartMoney = 250;
        public const long BerryStartMoney = 1500;

        /// <summary>Starting money of each world.</summary>
        public static long StartMoney(int world) => world == 1 ? TropicalStartMoney : world == 2 ? BerryStartMoney : 0;

        // ---------------------------------------------------------------- player
        public static float SawDps => World == 0 ? Balance.SawDps(D?.sawLevel ?? 0) : 14f + L(Upgrades.Harvest) * 6f;
        public static int BagCapacity => World == 0 ? Balance.BagCapacity(D?.bagLevel ?? 0) : 16 + L(Upgrades.Backpack) * 6;
        public static float MoveSpeed => World == 0 ? Balance.MoveSpeed(D?.speedLevel ?? 0) : 6.2f + L(Upgrades.Move) * 0.5f;
        /// <summary>Speed factor for pad loading / unloading (player only).</summary>
        public static float TransferSpeed => World == 0 ? 1f : 1f + L(Upgrades.CarrySpeed) * 0.35f;

        // ---------------------------------------------------------------- selling
        public static float PriceMult => World == 0 ? Balance.PriceMult(D?.priceLevel ?? 0) : 1f + L(Upgrades.Price) * 0.2f;
        public static int CounterLayers => World == 0 ? Balance.CounterLayers(D?.counterLevel ?? 0) : 3 + L(Upgrades.Counter);
        /// <summary>Layers in the cake display case (Berry Cake Shop).</summary>
        public static int CakeCaseLayers => 2 + L(Upgrades.CakeCase);
        public static float CakePriceMult => 1f + L(Upgrades.CakePrice) * 0.2f;
        public static int MaxOrder(int sold) => World == 0 ? Balance.MaxOrder(sold) : sold < 6 ? 2 : sold < 30 ? 3 : sold < 90 ? 4 : 5;
        /// <summary>Cake orders stay small: a cake is a treat, and it takes two machines to make.</summary>
        public static int MaxCakeOrder(int sold) => sold < 4 ? 1 : sold < 24 ? 2 : 3;

        // ---------------------------------------------------------------- farming
        public static int SlicesPerFruit(FruitKind k) => Balance.SlicesPerFruit[(int)k] + (World == 0 ? 0 : L(Upgrades.Yield));
        public static float RegrowDelay(FruitKind k) => Balance.RegrowDelay[(int)k] * (World == 0 ? 1f : 1f - 0.12f * L(Upgrades.Regrow));

        // ---------------------------------------------------------------- mixers
        public static float JuiceTime(FruitKind k) => Balance.JuiceTime[(int)k] / (World == 0 ? 1f : 1f + 0.25f * L(Upgrades.MixSpeed));
        /// <summary>Chance a batch produces a second cup.</summary>
        public static float BonusCupChance => World == 0 ? 0f : 0.2f * L(Upgrades.MixOutput);
        /// <summary>Layers of 8 cups on a mixer's output tray.</summary>
        public static int TrayLayers => World == 0 ? 3 : 3 + L(Upgrades.MixCapacity);

        // ---------------------------------------------------------------- bakery (world 2)
        public static float BatterTime(FruitKind k) => Balance.BatterTime[(int)k] / (1f + 0.25f * L(Upgrades.BakeSpeed));
        public static float BakeTime(FruitKind k) => Balance.BakeTime[(int)k] / (1f + 0.25f * L(Upgrades.BakeSpeed));
        /// <summary>Layers of 4 cakes on an oven's cooling rack.</summary>
        public static int RackLayers => 2 + L(Upgrades.BakeCapacity);

        // ---------------------------------------------------------------- fox raids (world 2)
        /// <summary>Seconds between fox raids (Fox Fence makes them rarer).</summary>
        public static float FoxInterval => UnityEngine.Random.Range(240f, 400f) * (1f + 0.3f * L(Upgrades.FoxFence));
        /// <summary>How long a raided farm takes to grow back on its own.</summary>
        public static float FoxRegrowTime => Mathf.Max(120f, 300f * (1f - 0.2f * L(Upgrades.FoxCare)));

        // ---------------------------------------------------------------- golden apples (premium, all worlds)
        public const int StartingApples = 5;
        public const int ApplesRestoreFarm = 3;
        public const int ApplesCallTruck = 1;
        /// <summary>Finish a truck order instantly: about one apple per dozen missing items.</summary>
        public static int ApplesFinishOrder(int remaining) => Mathf.Max(1, Mathf.CeilToInt(remaining / 12f));
        /// <summary>Finish an unlock pad instantly: one apple per "free cash bag" still missing.</summary>
        public static int ApplesFinishUnlock(long remaining, int unlocked) => Mathf.Clamp(Mathf.CeilToInt(remaining / (float)Mathf.Max(1, FreeCash(unlocked))), 1, 20);

        // ---------------------------------------------------------------- helpers
        public static float WorkerSpeedMult => World == 0 ? 1f : 1f + 0.15f * L(Upgrades.WorkerSpeed);
        public static int WorkerCarryBonus => World == 0 ? 0 : 2 * L(Upgrades.WorkerCarry);

        // ---------------------------------------------------------------- delivery
        /// <summary>Delivery pays about twice the shop value of the same juice (before truck type and upgrades).</summary>
        public const float DeliveryBaseMult = 2f;
        public static float DeliveryRewardMult => 1f + 0.2f * L(Upgrades.TruckReward);
        public static float DeliverySizeMult => 1f + 0.2f * L(Upgrades.TruckSize);
        public static float TruckInterval => Mathf.Max(45f, 90f * (1f - 0.1f * L(Upgrades.TruckFreq)));
        public static float PremiumChance => 0.08f + 0.07f * L(Upgrades.Premium);
        /// <summary>2x CASH pays half as much extra on deliveries, so ad boosts cannot trivialise truck orders.</summary>
        public static float DeliveryBoostMult => Boosts.IsActive(BoostKind.Cash2x) ? 1.5f : 1f;
        public const float TruckWait = 180f;

        // ---------------------------------------------------------------- misc
        public static int FreeCash(int unlocked) => World == 0 ? Balance.FreeCash(unlocked) : World == 1 ? 300 + unlocked * 650 : 1200 + unlocked * 1800;
        public static float OfflineRate(int mixers, int helpers)
        {
            if (World == 0) return Balance.OfflineRate(mixers, helpers);
            if (helpers <= 0) return 0f;
            float scale = World == 1 ? 1f : 3.2f;
            return (mixers * 0.4f + helpers * 1.5f) * scale * (1f + 0.4f * L(Upgrades.Offline));
        }

        public static string[] HelperIds => World == 0
            ? new[] { "hire_waiter", "farmer_orange", "farmer_melon", "farmer_pine" }
            : World == 1
                ? new[] { "t_runner", "t_loader", "t_farmer_coconut", "t_farmer_mango", "t_farmer_banana", "t_farmer_papaya" }
                : new[] { "b_runner", "b_baker", "b_loader", "b_farmer_straw", "b_farmer_rasp", "b_farmer_blue", "b_farmer_cran" };

        public static string[] MixerIds => World == 0
            ? new[] { "melon_juicer", "pine_juicer" }
            : World == 1
                ? new[] { "t_mango_mixer", "t_banana_mixer", "t_papaya_mixer" }
                : new[] { "b_rasp_press", "b_blue_press", "b_cran_press", "b_straw_oven", "b_rasp_oven", "b_blue_oven", "b_cran_oven" };

        public static string Money(long v)
        {
            if (v >= 1000000000) return (v / 1000000000f).ToString("0.##") + "B";
            if (v >= 1000000) return (v / 1000000f).ToString("0.##") + "M";
            if (v >= 10000) return (v / 1000f).ToString("0.#") + "K";
            return v.ToString("N0");
        }
    }

    /// <summary>One purchasable upgrade line (data-driven so each world can have its own tree).</summary>
    public class UpgradeDef
    {
        public string id;
        public string name;
        public string category;
        /// <summary>Key into GameRefs' upgrade icon table.</summary>
        public string icon;
        public int maxLevel = 5;
        public Func<int> level;
        public Func<int> cost;
        public Action apply;
        /// <summary>Stat text for a given level ("Cut power 20").</summary>
        public Func<int, string> stat;

        public int Level() => level();
        public int Cost() => Level() >= maxLevel ? -1 : cost();
        public void Apply() => apply();
        public bool Maxed => Level() >= maxLevel;
    }

    /// <summary>Upgrade trees per world. World 0 wraps the classic fields; world 1 is a six-tab tree stored by id.</summary>
    public static class Upgrades
    {
        public const string Harvest = "harvest", Yield = "yield", Regrow = "regrow";
        public const string MixSpeed = "mixspeed", MixOutput = "mixout", MixCapacity = "mixcap";
        public const string TruckReward = "truckreward", TruckSize = "trucksize", TruckFreq = "truckfreq", Premium = "premium";
        public const string Backpack = "backpack", Move = "move", CarrySpeed = "carryspeed";
        public const string WorkerSpeed = "wspeed", WorkerCarry = "wcarry";
        public const string Price = "price", Counter = "counter", Offline = "offline";
        // Berry Blast (world 2)
        public const string FoxFence = "foxfence", FoxCare = "foxcare";
        public const string BakeSpeed = "bakespeed", BakeCapacity = "bakecap", CakePrice = "cakeprice", CakeCase = "cakecase";

        public static readonly string[] Categories = { "FARM", "MIXER", "DELIVERY", "PLAYER", "WORKERS", "BUSINESS" };
        static readonly string[] BerryCategories = { "FARM", "MIXER", "BAKERY", "DELIVERY", "PLAYER", "BUSINESS" };

        /// <summary>Tabs of the upgrade panel in each world (world 0 has a single list).</summary>
        public static string[] CategoriesFor(int world) => world >= 2 ? BerryCategories : Categories;

        static List<UpgradeDef> _world0, _world1, _world2;

        public static List<UpgradeDef> ForWorld(int world) => world == 0 ? World0() : world == 1 ? World1() : World2();

        static UpgradeDef Classic(UpgradeKind k, string name, string icon, Func<int, string> stat) => new UpgradeDef
        {
            id = k.ToString().ToLowerInvariant(), name = name, category = "ALL", icon = icon, maxLevel = Balance.MaxUpgradeLevel,
            level = () => GameManager.I.GetLevel(k),
            cost = () => GameManager.I.GetCost(k),
            apply = () => GameManager.I.ApplyClassicUpgrade(k),
            stat = stat
        };

        static List<UpgradeDef> World0()
        {
            if (_world0 != null) return _world0;
            _world0 = new List<UpgradeDef>
            {
                Classic(UpgradeKind.Saw, "Chainsaw", "saw", l => $"Cut power {Balance.SawDps(l):0}"),
                Classic(UpgradeKind.Bag, "Backpack", "bag", l => $"Carry {Balance.BagCapacity(l)}"),
                Classic(UpgradeKind.Speed, "Speed", "speed", l => $"Speed {Balance.MoveSpeed(l):0.0}"),
                Classic(UpgradeKind.Price, "Recipe", "recipe", l => $"Price +{(Balance.PriceMult(l) - 1f) * 100f:0}%"),
                Classic(UpgradeKind.Counter, "Counter", "counter", l => $"Stack {Balance.CounterLayers(l) * 12}"),
            };
            return _world0;
        }

        static UpgradeDef Tree(string id, string name, string cat, string icon, int baseCost, float growth, int max, Func<int, string> stat) => new UpgradeDef
        {
            id = id, name = name, category = cat, icon = icon, maxLevel = max,
            level = () => GameManager.I.GetUpgrade(id),
            cost = () => Mathf.RoundToInt(baseCost * Mathf.Pow(growth, GameManager.I.GetUpgrade(id)) / 10f) * 10,
            apply = () => GameManager.I.SetUpgrade(id, GameManager.I.GetUpgrade(id) + 1),
            stat = stat
        };

        static List<UpgradeDef> World1()
        {
            if (_world1 != null) return _world1;
            _world1 = new List<UpgradeDef>
            {
                Tree(Harvest, "Harvest Speed", "FARM", "saw", 400, 2.3f, 5, l => $"Cut power {14 + l * 6}"),
                Tree(Yield, "Fruit Yield", "FARM", "yield", 900, 2.8f, 3, l => $"Pieces +{l}"),
                Tree(Regrow, "Regrowth", "FARM", "regrow", 700, 2.4f, 4, l => $"Regrow time -{l * 12}%"),
                Tree(MixSpeed, "Mixer Speed", "MIXER", "mixspeed", 800, 2.3f, 5, l => $"Blend speed +{l * 25}%"),
                Tree(MixOutput, "Bonus Cup", "MIXER", "mixout", 1500, 2.4f, 5, l => $"Double cup chance {l * 20}%"),
                Tree(MixCapacity, "Tray Size", "MIXER", "mixcap", 1000, 2.2f, 4, l => $"Tray size {(3 + l) * 8}"),
                Tree(TruckReward, "Truck Reward", "DELIVERY", "truckreward", 2000, 2.3f, 5, l => $"Rewards +{l * 20}%"),
                Tree(TruckSize, "Big Orders", "DELIVERY", "trucksize", 2500, 2.4f, 5, l => $"Order size +{l * 20}%"),
                Tree(TruckFreq, "Truck Frequency", "DELIVERY", "truckfreq", 1800, 2.3f, 5, l => $"Next truck {Mathf.Max(45f, 90f * (1f - 0.1f * l)):0}s"),
                Tree(Premium, "Premium Orders", "DELIVERY", "premium", 5000, 2.6f, 3, l => $"Premium chance {(0.08f + 0.07f * l) * 100f:0}%"),
                Tree(Backpack, "Backpack", "PLAYER", "bag", 350, 2.2f, 5, l => $"Carry {16 + l * 6}"),
                Tree(Move, "Speed", "PLAYER", "speed", 300, 2.2f, 5, l => $"Speed {6.2f + l * 0.5f:0.0}"),
                Tree(CarrySpeed, "Quick Hands", "PLAYER", "carryspeed", 600, 2.5f, 3, l => $"Load speed +{l * 35}%"),
                Tree(WorkerSpeed, "Helper Speed", "WORKERS", "wspeed", 4000, 2.3f, 4, l => $"Helper speed +{l * 15}%"),
                Tree(WorkerCarry, "Helper Carry", "WORKERS", "wcarry", 3500, 2.3f, 4, l => $"Helper carry +{l * 2}"),
                Tree(Price, "Juice Price", "BUSINESS", "recipe", 1200, 2.3f, 5, l => $"Price +{l * 20}%"),
                Tree(Counter, "Counter", "BUSINESS", "counter", 900, 2.2f, 4, l => $"Stack {(3 + l) * 12}"),
                Tree(Offline, "Night Shift", "BUSINESS", "offline", 2500, 2.5f, 3, l => $"Night income +{l * 40}%"),
            };
            return _world1;
        }

        /// <summary>
        /// Berry Blast: the tropical tree at a bigger scale, plus fox defences on the farm and a BAKERY tab for the cake shop.
        /// Helpers moved into BUSINESS so the panel keeps six tabs. Ids are shared with world 1 on purpose: each world
        /// starts with its upgrades reset (<see cref="GameManager.BeginExpansion"/>).
        /// </summary>
        static List<UpgradeDef> World2()
        {
            if (_world2 != null) return _world2;
            _world2 = new List<UpgradeDef>
            {
                Tree(Harvest, "Harvest Speed", "FARM", "saw", 1500, 2.3f, 5, l => $"Cut power {14 + l * 6}"),
                Tree(Yield, "Berry Yield", "FARM", "berryyield", 3500, 2.8f, 3, l => $"Berries +{l}"),
                Tree(Regrow, "Regrowth", "FARM", "regrow", 2500, 2.4f, 4, l => $"Regrow time -{l * 12}%"),
                Tree(FoxFence, "Fox Fence", "FARM", "foxfence", 6000, 2.5f, 3, l => $"Fox raids -{Mathf.RoundToInt((1f - 1f / (1f + 0.3f * l)) * 100f)}%"),
                Tree(FoxCare, "Garden Care", "FARM", "foxcare", 5000, 2.5f, 3, l => $"Farm recovery {Mathf.Max(120f, 300f * (1f - 0.2f * l)) / 60f:0.#} min"),
                Tree(MixSpeed, "Press Speed", "MIXER", "mixspeed", 2800, 2.3f, 5, l => $"Press speed +{l * 25}%"),
                Tree(MixOutput, "Bonus Cup", "MIXER", "mixout", 5500, 2.4f, 5, l => $"Double cup chance {l * 20}%"),
                Tree(MixCapacity, "Tray Size", "MIXER", "mixcap", 3500, 2.2f, 4, l => $"Tray size {(3 + l) * 8}"),
                Tree(BakeSpeed, "Oven Heat", "BAKERY", "bakespeed", 6000, 2.3f, 5, l => $"Baking speed +{l * 25}%"),
                Tree(BakeCapacity, "Cooling Rack", "BAKERY", "bakecap", 5000, 2.3f, 4, l => $"Rack size {(2 + l) * 4}"),
                Tree(CakePrice, "Cake Recipe", "BAKERY", "cakeprice", 8000, 2.4f, 5, l => $"Cake price +{l * 20}%"),
                Tree(CakeCase, "Pastry Case", "BAKERY", "cakecase", 4500, 2.2f, 4, l => $"Case holds {(2 + l) * 8}"),
                Tree(TruckReward, "Truck Reward", "DELIVERY", "truckreward", 7000, 2.3f, 5, l => $"Rewards +{l * 20}%"),
                Tree(TruckSize, "Big Orders", "DELIVERY", "trucksize", 8500, 2.4f, 5, l => $"Order size +{l * 20}%"),
                Tree(TruckFreq, "Truck Frequency", "DELIVERY", "truckfreq", 6500, 2.3f, 5, l => $"Next truck {Mathf.Max(45f, 90f * (1f - 0.1f * l)):0}s"),
                Tree(Premium, "Premium Orders", "DELIVERY", "premium", 18000, 2.6f, 3, l => $"Premium chance {(0.08f + 0.07f * l) * 100f:0}%"),
                Tree(Backpack, "Backpack", "PLAYER", "bag", 1200, 2.2f, 5, l => $"Carry {16 + l * 6}"),
                Tree(Move, "Speed", "PLAYER", "speed", 1000, 2.2f, 5, l => $"Speed {6.2f + l * 0.5f:0.0}"),
                Tree(CarrySpeed, "Quick Hands", "PLAYER", "carryspeed", 2200, 2.5f, 3, l => $"Load speed +{l * 35}%"),
                Tree(Price, "Juice Price", "BUSINESS", "recipe", 4500, 2.3f, 5, l => $"Price +{l * 20}%"),
                Tree(Counter, "Juice Counter", "BUSINESS", "counter", 3200, 2.2f, 4, l => $"Stack {(3 + l) * 12}"),
                Tree(WorkerSpeed, "Helper Speed", "BUSINESS", "wspeed", 14000, 2.3f, 4, l => $"Helper speed +{l * 15}%"),
                Tree(WorkerCarry, "Helper Carry", "BUSINESS", "wcarry", 12000, 2.3f, 4, l => $"Helper carry +{l * 2}"),
                Tree(Offline, "Night Shift", "BUSINESS", "offline", 9000, 2.5f, 3, l => $"Night income +{l * 40}%"),
            };
            return _world2;
        }

        /// <summary>True when every upgrade of the world is maxed.</summary>
        public static bool AllMaxed(int world)
        {
            foreach (var d in ForWorld(world))
                if (!d.Maxed) return false;
            return true;
        }
    }
}
