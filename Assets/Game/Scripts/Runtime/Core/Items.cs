using UnityEngine;

namespace JuiceKing
{
    public enum FruitKind { Orange = 0, Watermelon = 1, Pineapple = 2 }

    public enum ItemType
    {
        OrangeSlice = 0, WatermelonSlice = 1, PineappleSlice = 2,
        OrangeJuice = 3, WatermelonJuice = 4, PineappleJuice = 5,
        Money = 6
    }

    public static class ItemTypes
    {
        public const int FruitCount = 3;

        public static bool IsSlice(this ItemType t) => (int)t < 3;
        public static bool IsJuice(this ItemType t) => (int)t >= 3 && (int)t < 6;
        public static FruitKind Fruit(this ItemType t) => (FruitKind)((int)t % 3);
        public static ItemType Slice(FruitKind f) => (ItemType)(int)f;
        public static ItemType Juice(FruitKind f) => (ItemType)(3 + (int)f);
    }

    /// <summary>All tuning numbers in one place.</summary>
    public static class Balance
    {
        public static readonly string[] FruitNames = { "Orange", "Watermelon", "Pineapple" };

        public static readonly Color[] FruitColors =
        {
            new Color(1.00f, 0.55f, 0.08f),
            new Color(0.95f, 0.22f, 0.30f),
            new Color(1.00f, 0.84f, 0.20f),
        };

        public static readonly Color[] JuiceColors =
        {
            new Color(1.00f, 0.62f, 0.10f),
            new Color(0.98f, 0.30f, 0.38f),
            new Color(1.00f, 0.88f, 0.30f),
        };

        // Fruit harvesting
        public static readonly float[] FruitHp = { 3f, 5f, 7f };
        public static readonly int[] SlicesPerFruit = { 3, 4, 4 };
        public static readonly float[] RegrowDelay = { 2.5f, 3.5f, 4.5f };

        // Juicer
        public static readonly int[] SlicesPerJuice = { 2, 2, 2 };
        public static readonly float[] JuiceTime = { 0.9f, 1.1f, 1.3f };

        // Selling
        public static readonly int[] JuicePrice = { 6, 10, 16 };

        // Player upgrades (index = current level, value = cost to reach next level)
        public static readonly int[] SawCosts = { 40, 110, 240, 480, 900 };
        public static readonly int[] BagCosts = { 30, 90, 200, 420, 800 };
        public static readonly int[] SpeedCosts = { 30, 90, 200, 420, 800 };
        public static readonly int[] PriceCosts = { 60, 160, 350, 700, 1300 };
        public static readonly int[] CounterCosts = { 50, 140, 300, 600, 1100 };

        public static float SawDps(int lvl) => 7f + lvl * 4f;
        public static int BagCapacity(int lvl) => 8 + lvl * 4;
        public static float MoveSpeed(int lvl) => 5.2f + lvl * 0.55f;
        public static float PriceMult(int lvl) => 1f + lvl * 0.15f;
        /// <summary>Juice cups stacked per slot on the counter (6 x 2 slots per layer).</summary>
        public static int CounterLayers(int lvl) => 2 + lvl;

        // Customers
        public static readonly Vector2 CustomerSpawnGap = new Vector2(0.35f, 0.9f);
        public static readonly Vector2 CustomerSpeed = new Vector2(3.6f, 4.3f);
        public const float CustomerPatience = 40f;
        /// <summary>Largest order size, growing with the number of cups sold.</summary>
        public static int MaxOrder(int sold) => sold < 5 ? 1 : sold < 25 ? 2 : sold < 80 ? 3 : 4;

        public const int MaxUpgradeLevel = 5;

        // Rewarded boosts
        public const float CashBoostSeconds = 120f;
        public const float TurboSeconds = 90f;
        public const float BoostMaxSeconds = 600f;
        public const float CashBoostMult = 2f;
        public const float TurboMoveMult = 1.35f;
        public const float TurboWorkMult = 1.8f;
        public const float FreeCashCooldown = 120f;
        public const float UnlockAssistCooldown = 60f;

        /// <summary>Free cash reward scales with how far the player has progressed.</summary>
        public static int FreeCash(int unlockedCount) => 40 + unlockedCount * 35;

        // Offline earnings (only once a helper is hired)
        public const float OfflineMinSeconds = 60f;
        public const float OfflineMaxSeconds = 30f * 60f;
        public static float OfflineRate(int juicers, int helpers) => helpers <= 0 ? 0f : juicers * 0.03f + helpers * 0.12f;
    }
}
