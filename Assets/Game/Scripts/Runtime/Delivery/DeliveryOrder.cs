using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    public enum TruckKind { Van = 0, JuiceTruck = 1, Resort = 2, Premium = 3 }

    /// <summary>One kind of delivery vehicle: order size, pay bonus, how often it shows up and who sends it.</summary>
    public class TruckDef
    {
        public TruckKind kind;
        public string name;
        public Vector2Int qty;
        public float rewardMult;
        public float weight;
        public bool premium;
        /// <summary>Minimum number of juice types the player makes before this truck can appear.</summary>
        public int minKinds = 1;
        public string[] clients;
    }

    /// <summary>A delivery request: <see cref="qty"/> cups of one juice for a <see cref="reward"/>.</summary>
    [System.Serializable]
    public class DeliveryOrder
    {
        public TruckKind truck;
        public string client;
        public FruitKind kind;
        public int qty;
        public int delivered;
        public long reward;

        public bool Done => delivered >= qty;
        public int Remaining => Mathf.Max(0, qty - delivered);
        public float Progress => qty <= 0 ? 1f : Mathf.Clamp01(delivered / (float)qty);
    }

    /// <summary>Truck catalogue and order generation. Add a <see cref="TruckDef"/> to add a new truck type.</summary>
    public static class DeliveryOrders
    {
        public static readonly TruckDef[] Trucks =
        {
            new TruckDef { kind = TruckKind.Van, name = "Delivery Van", qty = new Vector2Int(8, 14), rewardMult = 1f, weight = 50f,
                clients = new[] { "SMOOTHIE STAND", "SURF SHOP", "BEACH KIOSK", "ICE CREAM CART" } },
            new TruckDef { kind = TruckKind.JuiceTruck, name = "Juice Truck", qty = new Vector2Int(16, 24), rewardMult = 1.15f, weight = 35f,
                clients = new[] { "HOTEL ORDER", "BEACH BAR", "TROPICAL CAFE", "YOGA RETREAT" } },
            new TruckDef { kind = TruckKind.Resort, name = "Resort Truck", qty = new Vector2Int(26, 40), rewardMult = 1.3f, weight = 15f, minKinds = 2,
                clients = new[] { "RESORT", "CRUISE SHIP", "SPA RESORT", "WATER PARK" } },
            new TruckDef { kind = TruckKind.Premium, name = "Premium Truck", qty = new Vector2Int(30, 45), rewardMult = 1.8f, weight = 0f, premium = true, minKinds = 2,
                clients = new[] { "ROYAL PALACE", "ISLAND FESTIVAL", "CELEBRITY YACHT", "GRAND GALA" } },
        };

        public static TruckDef Def(TruckKind k) => Trucks[(int)k];

        /// <summary>The very first truck: small and easy, to teach the system.</summary>
        public static DeliveryOrder First(FruitKind kind)
        {
            var o = new DeliveryOrder { truck = TruckKind.Van, client = "FIRST CUSTOMER", kind = kind, qty = 8 };
            o.reward = Reward(o);
            return o;
        }

        public static DeliveryOrder Generate(IReadOnlyList<FruitKind> kinds, bool premiumUnlocked)
        {
            int nKinds = Mathf.Max(1, kinds.Count);
            TruckDef def = null;
            if (premiumUnlocked && nKinds >= 2 && Random.value < Economy.PremiumChance) def = Def(TruckKind.Premium);
            if (def == null)
            {
                float total = 0f;
                foreach (var t in Trucks)
                    if (!t.premium && nKinds >= t.minKinds) total += t.weight;
                float r = Random.value * total;
                foreach (var t in Trucks)
                {
                    if (t.premium || nKinds < t.minKinds) continue;
                    r -= t.weight;
                    if (r <= 0f)
                    {
                        def = t;
                        break;
                    }
                }
                if (def == null) def = Trucks[0];
            }

            var o = new DeliveryOrder
            {
                truck = def.kind,
                client = def.clients[Random.Range(0, def.clients.Length)],
                // Newer juices a bit more often so fresh unlocks matter.
                kind = kinds[Random.value < 0.35f ? kinds.Count - 1 : Random.Range(0, kinds.Count)],
            };
            // Orders grow with the business: more juice lines, bigger trucks.
            float scale = Economy.DeliverySizeMult * (1f + 0.12f * (nKinds - 1));
            o.qty = Mathf.RoundToInt(Random.Range(def.qty.x, def.qty.y + 1) * scale);
            o.reward = Reward(o);
            return o;
        }

        /// <summary>~2x the shop value of the same cups, times the truck bonus and the Truck Reward upgrade, rounded nicely.</summary>
        public static long Reward(DeliveryOrder o)
        {
            float price = GameManager.I != null ? GameManager.I.JuicePrice(o.kind) : Balance.JuicePrice[(int)o.kind];
            float v = o.qty * price * Economy.DeliveryBaseMult * Def(o.truck).rewardMult * Economy.DeliveryRewardMult;
            long step = v > 20000 ? 500 : v > 2000 ? 100 : 50;
            return (long)Mathf.Max(step, Mathf.Round(v / step) * step);
        }
    }
}
