using System.Collections.Generic;

namespace JuiceKing
{
    public enum IapKind { Apples, Tickets, RemoveAds }

    public enum IapBadge { None, Popular, BestValue }

    /// <summary>One store product: what it grants and how the shop card shows it.</summary>
    public class IapProduct
    {
        /// <summary>Store product id (Google Play / App Store). Saved receipts refer to it: never rename a shipped id.</summary>
        public string id;
        public IapKind kind;
        /// <summary>Pack name on the card ("STARTER"...).</summary>
        public string title;
        /// <summary>Apples or tickets granted (ignored for Remove Ads).</summary>
        public int amount;
        /// <summary>"+20% BONUS" line on the card; 0 hides it.</summary>
        public int bonusPercent;
        /// <summary>Shown until the store reports its localized price.</summary>
        public string fallbackPrice;
        public IapBadge badge;
        /// <summary>Consumables can be bought again; Remove Ads is bought once and restored.</summary>
        public bool Consumable => kind != IapKind.RemoveAds;
    }

    /// <summary>
    /// Every in-app product the shop sells. The shop builder makes one card per entry (rebuild the scenes after adding
    /// or reordering products); quantities, bonus lines, badges and prices are read from here at runtime, so retuning
    /// them needs no rebuild. Register the same ids with the store (Unity IAP catalog) when a real provider is plugged in.
    /// </summary>
    public static class IapCatalog
    {
        public const string RemoveAdsId = "jk_remove_ads";

        public static readonly IapProduct[] Products =
        {
            Apples("jk_apples_starter", "STARTER", 15, 0, "$0.99"),
            Apples("jk_apples_small", "SMALL", 85, 10, "$4.99"),
            Apples("jk_apples_medium", "MEDIUM", 180, 20, "$9.99", IapBadge.Popular),
            Apples("jk_apples_large", "LARGE", 375, 25, "$19.99"),
            Apples("jk_apples_mega", "MEGA", 1000, 30, "$49.99"),
            Apples("jk_apples_ultimate", "ULTIMATE", 2200, 45, "$99.99", IapBadge.BestValue),

            Tickets("jk_tickets_starter", "STARTER", 5, 0, "$0.99"),
            Tickets("jk_tickets_small", "SMALL", 16, 5, "$2.99"),
            Tickets("jk_tickets_medium", "MEDIUM", 30, 20, "$4.99", IapBadge.Popular),
            Tickets("jk_tickets_large", "LARGE", 70, 40, "$9.99"),
            Tickets("jk_tickets_mega", "MEGA", 160, 60, "$19.99"),
            Tickets("jk_tickets_ultimate", "ULTIMATE", 450, 80, "$49.99", IapBadge.BestValue),

            new IapProduct { id = RemoveAdsId, kind = IapKind.RemoveAds, title = "REMOVE ADS", fallbackPrice = "$3.99" },
        };

        public static IapProduct Get(string id)
        {
            for (int i = 0; i < Products.Length; i++)
                if (Products[i].id == id) return Products[i];
            return null;
        }

        public static List<IapProduct> OfKind(IapKind kind)
        {
            var list = new List<IapProduct>();
            for (int i = 0; i < Products.Length; i++)
                if (Products[i].kind == kind) list.Add(Products[i]);
            return list;
        }

        static IapProduct Apples(string id, string title, int amount, int bonus, string price, IapBadge badge = IapBadge.None) =>
            new IapProduct { id = id, kind = IapKind.Apples, title = title, amount = amount, bonusPercent = bonus, fallbackPrice = price, badge = badge };

        static IapProduct Tickets(string id, string title, int amount, int bonus, string price, IapBadge badge = IapBadge.None) =>
            new IapProduct { id = id, kind = IapKind.Tickets, title = title, amount = amount, bonusPercent = bonus, fallbackPrice = price, badge = badge };
    }
}
