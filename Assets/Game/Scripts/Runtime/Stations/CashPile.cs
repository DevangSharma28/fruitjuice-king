using UnityEngine;

namespace JuiceKing
{
    /// <summary>Bills paid by customers pile up here until the player grabs them.</summary>
    public class CashPile : MonoBehaviour
    {
        public ItemPile pile;
        public CashZone zone;

        int _overflow;

        public int TotalValue
        {
            get
            {
                int v = _overflow;
                foreach (var b in pile.items) v += b.value;
                return v;
            }
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
                Tweener.Delay(delay, () =>
                {
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
