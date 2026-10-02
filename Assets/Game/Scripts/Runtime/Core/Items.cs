using UnityEngine;

namespace JuiceKing
{
    /// <summary>0-2 original farm, 3-6 Tropical Farm (Expansion 1), 7-10 Berry Blast (Expansion 2). Values are saved: never renumber.</summary>
    public enum FruitKind
    {
        Orange = 0, Watermelon = 1, Pineapple = 2,
        Coconut = 3, Mango = 4, Banana = 5, Papaya = 6,
        Strawberry = 7, Raspberry = 8, Blueberry = 9, Cranberry = 10
    }

    /// <summary>What a counter sells and a customer queues for.</summary>
    public enum ProductLine { Juice = 0, Cake = 1 }

    /// <summary>
    /// Slices (harvested pieces) are 0..19, juices 20..39, money 40, cake batter 60..79, cakes 80..99: every block starts
    /// on a multiple of 20 so <see cref="ItemTypes.Fruit"/> works for all of them. Items are never saved, so values can
    /// be renumbered safely.
    /// </summary>
    public enum ItemType
    {
        OrangeSlice = 0, WatermelonSlice = 1, PineappleSlice = 2,
        CoconutPiece = 3, MangoSlice = 4, BananaPiece = 5, PapayaSlice = 6,
        Strawberries = 7, Raspberries = 8, Blueberries = 9, Cranberries = 10,
        OrangeJuice = 20, WatermelonJuice = 21, PineappleJuice = 22,
        CoconutJuice = 23, MangoJuice = 24, BananaShake = 25, PapayaJuice = 26,
        StrawberrySmoothie = 27, RaspberryFizz = 28, BlueberryShake = 29, CranberryCooler = 30,
        Money = 40,
        StrawberryBatter = 67, RaspberryBatter = 68, BlueberryBatter = 69, CranberryBatter = 70,
        StrawberryCake = 87, RaspberryCake = 88, BlueberryCake = 89, CranberryCake = 90
    }

    public static class ItemTypes
    {
        public const int FruitCount = 11;
        const int JuiceBase = 20;
        const int MoneyValue = 40;
        const int BatterBase = 60;
        const int CakeBase = 80;

        public static bool IsSlice(this ItemType t) => (int)t >= 0 && (int)t < JuiceBase;
        public static bool IsJuice(this ItemType t) => (int)t >= JuiceBase && (int)t < MoneyValue;
        public static bool IsBatter(this ItemType t) => (int)t >= BatterBase && (int)t < CakeBase;
        public static bool IsCake(this ItemType t) => (int)t >= CakeBase && (int)t < CakeBase + 20;
        public static FruitKind Fruit(this ItemType t) => (FruitKind)((int)t % 20);
        public static ItemType Slice(FruitKind f) => (ItemType)(int)f;
        public static ItemType Juice(FruitKind f) => (ItemType)(JuiceBase + (int)f);
        public static ItemType Batter(FruitKind f) => (ItemType)(BatterBase + (int)f);
        public static ItemType Cake(FruitKind f) => (ItemType)(CakeBase + (int)f);
        public static ItemType Product(ProductLine line, FruitKind f) => line == ProductLine.Cake ? Cake(f) : Juice(f);
        public static bool IsProduct(this ItemType t, ProductLine line) => line == ProductLine.Cake ? t.IsCake() : t.IsJuice();
        /// <summary>Something a counter or a delivery box takes (juice or cake).</summary>
        public static bool IsSellable(this ItemType t) => t.IsJuice() || t.IsCake();
        public static bool IsTropical(this FruitKind f) => (int)f >= 3 && (int)f < 7;
        public static bool IsBerry(this FruitKind f) => (int)f >= 7;
    }

    /// <summary>All tuning numbers in one place (index = FruitKind).</summary>
    public static class Balance
    {
        public static readonly string[] FruitNames =
            { "Orange", "Watermelon", "Pineapple", "Coconut", "Mango", "Banana", "Papaya", "Strawberry", "Raspberry", "Blueberry", "Cranberry" };
        public static readonly string[] JuiceNames =
        {
            "Orange Juice", "Melon Juice", "Pineapple Juice", "Coconut Juice", "Mango Juice", "Banana Shake", "Papaya Juice",
            "Strawberry Smoothie", "Raspberry Fizz", "Blueberry Shake", "Cranberry Cooler"
        };
        /// <summary>Cakes exist for the berries only (Berry Cake Shop).</summary>
        public static readonly string[] CakeNames =
            { "", "", "", "", "", "", "", "Strawberry Shortcake", "Raspberry Velvet", "Blueberry Cheesecake", "Cranberry Tart" };

        public static readonly Color[] FruitColors =
        {
            new Color(1.00f, 0.55f, 0.08f),
            new Color(0.95f, 0.22f, 0.30f),
            new Color(1.00f, 0.84f, 0.20f),
            new Color(0.55f, 0.36f, 0.2f),
            new Color(1.00f, 0.62f, 0.12f),
            new Color(1.00f, 0.88f, 0.25f),
            new Color(1.00f, 0.5f, 0.3f),
            new Color(0.95f, 0.16f, 0.22f),
            new Color(0.9f, 0.18f, 0.42f),
            new Color(0.28f, 0.36f, 0.86f),
            new Color(0.72f, 0.06f, 0.16f),
        };

        public static readonly Color[] JuiceColors =
        {
            new Color(1.00f, 0.62f, 0.10f),
            new Color(0.98f, 0.30f, 0.38f),
            new Color(1.00f, 0.88f, 0.30f),
            new Color(0.96f, 0.95f, 0.9f),
            new Color(1.00f, 0.7f, 0.12f),
            new Color(1.00f, 0.93f, 0.62f),
            new Color(1.00f, 0.48f, 0.32f),
            new Color(1.00f, 0.45f, 0.55f),
            new Color(0.96f, 0.26f, 0.52f),
            new Color(0.5f, 0.38f, 0.9f),
            new Color(0.86f, 0.12f, 0.26f),
        };

        // Fruit harvesting. Tropical fruit is tougher but yields more per fruit; berry bushes shake loose fast.
        public static readonly float[] FruitHp = { 3f, 5f, 7f, 10f, 12f, 16f, 20f, 10f, 13f, 16f, 20f };
        public static readonly int[] SlicesPerFruit = { 3, 4, 4, 4, 4, 5, 5, 4, 4, 5, 5 };
        public static readonly float[] RegrowDelay = { 2.5f, 3.5f, 4.5f, 3f, 3.5f, 4f, 4.5f, 3f, 3.5f, 4f, 4.5f };

        // Juicer / mixer / berry press
        public static readonly int[] SlicesPerJuice = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        public static readonly float[] JuiceTime = { 0.9f, 1.1f, 1.3f, 1.0f, 1.1f, 1.2f, 1.3f, 1.0f, 1.1f, 1.2f, 1.3f };

        // Selling (before the price upgrade). Each world is a new economic scale (tropical ~6x, berry ~3x tropical).
        public static readonly int[] JuicePrice = { 6, 10, 16, 40, 55, 75, 100, 120, 170, 230, 300 };

        // Berry Cake Shop: berries -> batter (cake mixer) -> cake (oven). Cakes sell for ~3.5x a juice of the same berry.
        public static readonly int[] CakePrice = { 0, 0, 0, 0, 0, 0, 0, 420, 600, 820, 1080 };
        public const int BerriesPerBatter = 3;
        public static readonly float[] BatterTime = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.5f, 1.6f, 1.7f, 1.8f };
        public static readonly float[] BakeTime = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2.4f, 2.6f, 2.8f, 3f };

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
        public const float FreeCashCooldown = 180f;
        public const float UnlockAssistCooldown = 150f;

        /// <summary>Legacy free cash formula (world 0 before the release pass). The bag now follows <see cref="Economy.FreeCash"/>.</summary>
        public static int FreeCash(int unlockedCount) => 40 + unlockedCount * 35;

        // Offline earnings (only once a helper is hired). The cap and rate now live in Economy (OfflineMaxSeconds,
        // OfflinePerSecond); OfflineRate is the fallback for saves made before income was measured.
        public const float OfflineMinSeconds = 60f;
        public const float OfflineMaxSeconds = 30f * 60f;
        public static float OfflineRate(int juicers, int helpers) => helpers <= 0 ? 0f : juicers * 0.03f + helpers * 0.12f;
    }
}
