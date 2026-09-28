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

        public static float SawDps(int lvl) => 7f + lvl * 4f;
        public static int BagCapacity(int lvl) => 8 + lvl * 4;
        public static float MoveSpeed(int lvl) => 5.2f + lvl * 0.55f;

        public const int MaxUpgradeLevel = 5;
    }
}
