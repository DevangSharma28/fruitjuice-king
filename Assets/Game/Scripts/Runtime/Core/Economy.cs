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

        // ---------------------------------------------------------------- player
        public static float SawDps => World == 0 ? Balance.SawDps(D?.sawLevel ?? 0) : 14f + L(Upgrades.Harvest) * 6f;
        public static int BagCapacity => World == 0 ? Balance.BagCapacity(D?.bagLevel ?? 0) : 16 + L(Upgrades.Backpack) * 6;
        public static float MoveSpeed => World == 0 ? Balance.MoveSpeed(D?.speedLevel ?? 0) : 6.2f + L(Upgrades.Move) * 0.5f;
        /// <summary>Speed factor for pad loading / unloading (player only).</summary>
        public static float TransferSpeed => World == 0 ? 1f : 1f + L(Upgrades.CarrySpeed) * 0.35f;

        // ---------------------------------------------------------------- selling
        public static float PriceMult => World == 0 ? Balance.PriceMult(D?.priceLevel ?? 0) : 1f + L(Upgrades.Price) * 0.2f;
        public static int CounterLayers => World == 0 ? Balance.CounterLayers(D?.counterLevel ?? 0) : 3 + L(Upgrades.Counter);
        public static int MaxOrder(int sold) => World == 0 ? Balance.MaxOrder(sold) : sold < 6 ? 2 : sold < 30 ? 3 : sold < 90 ? 4 : 5;

        // ---------------------------------------------------------------- farming
        public static int SlicesPerFruit(FruitKind k) => Balance.SlicesPerFruit[(int)k] + (World == 0 ? 0 : L(Upgrades.Yield));
        public static float RegrowDelay(FruitKind k) => Balance.RegrowDelay[(int)k] * (World == 0 ? 1f : 1f - 0.12f * L(Upgrades.Regrow));

        // ---------------------------------------------------------------- mixers
        public static float JuiceTime(FruitKind k) => Balance.JuiceTime[(int)k] / (World == 0 ? 1f : 1f + 0.25f * L(Upgrades.MixSpeed));
        /// <summary>Chance a batch produces a second cup.</summary>
        public static float BonusCupChance => World == 0 ? 0f : 0.2f * L(Upgrades.MixOutput);
        /// <summary>Layers of 8 cups on a mixer's output tray.</summary>
        public static int TrayLayers => World == 0 ? 3 : 3 + L(Upgrades.MixCapacity);

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
        public static int FreeCash(int unlocked) => World == 0 ? Balance.FreeCash(unlocked) : 300 + unlocked * 650;
        public static float OfflineRate(int mixers, int helpers)
        {
            if (World == 0) return Balance.OfflineRate(mixers, helpers);
            if (helpers <= 0) return 0f;
            return (mixers * 0.4f + helpers * 1.5f) * (1f + 0.4f * L(Upgrades.Offline));
        }

        public static string[] HelperIds => World == 0
            ? new[] { "hire_waiter", "farmer_orange", "farmer_melon", "farmer_pine" }
            : new[] { "t_runner", "t_loader", "t_farmer_coconut", "t_farmer_mango", "t_farmer_banana", "t_farmer_papaya" };

        public static string[] MixerIds => World == 0
            ? new[] { "melon_juicer", "pine_juicer" }
            : new[] { "t_mango_mixer", "t_banana_mixer", "t_papaya_mixer" };

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

        public static readonly string[] Categories = { "FARM", "MIXER", "DELIVERY", "PLAYER", "WORKERS", "BUSINESS" };

        static List<UpgradeDef> _world0, _world1;

        public static List<UpgradeDef> ForWorld(int world) => world == 0 ? World0() : World1();

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

        /// <summary>True when every upgrade of the world is maxed.</summary>
        public static bool AllMaxed(int world)
        {
            foreach (var d in ForWorld(world))
                if (!d.Maxed) return false;
            return true;
        }
    }
}
