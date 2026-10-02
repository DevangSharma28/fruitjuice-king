using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Bills paid by customers pile up here until the player grabs them. What lies on the piles is saved
    /// (<see cref="SaveData.pendingCash"/>) and comes back on the next launch, so closing the app never loses a sale.
    /// </summary>
    public class CashPile : MonoBehaviour
    {
        public ItemPile pile;
        public CashZone zone;

        static readonly List<CashPile> All = new List<CashPile>();

        /// <summary>The saved pile money has been put back on a pile in this scene (until then a save keeps the saved value).</summary>
        public static bool Restored { get; private set; }

        /// <summary>Called when a world scene starts.</summary>
        public static void ResetRestored() => Restored = false;

        int _overflow;
        // Paid but still on its way to the pile (bills are spawned a few frames apart).
        int _inFlight;

        public int TotalValue
        {
            get
            {
                int v = _overflow + _inFlight;
                foreach (var b in pile.items) v += b.value;
                return v;
            }
        }

        /// <summary>Money on every active cash pile of the scene (saved with the game).</summary>
        public static long TotalAll()
        {
            long v = CashZone.InFlight;
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && All[i].isActiveAndEnabled) v += All[i].TotalValue;
            return v;
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable() => All.Remove(this);

        void Start()
        {
            // The first pile that wakes up takes back what was lying on the piles at the last save.
            var gm = GameManager.I;
            if (gm == null || GameManager.Redirecting || Restored) return;
            Restored = true;
            if (gm.data.pendingCash <= 0) return;
            long owed = gm.data.pendingCash;
            gm.data.pendingCash = 0;
            int stacked = 0;
            while (owed > 0 && pile.HasSpace && stacked < 24)
            {
                int value = (int)System.Math.Min(owed, System.Math.Max(4L, owed / 12));
                var bill = GameRefs.I.SpawnItem(ItemType.Money, pile.transform.position, Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f));
                bill.value = value;
                pile.AddInstant(bill);
                owed -= value;
                stacked++;
            }
            _overflow += (int)System.Math.Min(owed, int.MaxValue);
        }

        public void Deposit(int amount, Vector3 from)
        {
            if (amount <= 0) return;
            int bills = Mathf.Clamp(Mathf.CeilToInt(amount / 4f), 1, 6);
            int per = amount / bills;
            int rem = amount - per * bills;
            for (int i = 0; i < bills; i++)
            {
                int value = per + (i == 0 ? rem : 0);
                if (value <= 0) continue;
                float delay = i * 0.06f;
                _inFlight += value;
                Tweener.Delay(delay, () =>
                {
                    if (this == null) return;
                    _inFlight -= value;
                    if (!pile.HasSpace)
                    {
                        _overflow += value;
                        return;
                    }
                    var bill = GameRefs.I.SpawnItem(ItemType.Money, from, Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f));
                    bill.value = value;
                    pile.Add(bill, 1.6f, 0.4f, () =>
                    {
                        Sfx.Play(SfxId.Coin, 0.13f, 0.9f);
                        Fx.Glint(bill.transform.position + Vector3.up * 0.15f, new Color(0.8f, 1f, 0.8f), 1);
                    });
                });
            }
        }

        public StackItem TakeBill() => pile.TakeLast();

        public int TakeOverflow()
        {
            int v = _overflow;
            _overflow = 0;
            return v;
        }
    }
}
