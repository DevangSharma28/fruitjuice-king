using UnityEngine;

namespace JuiceKing
{
    /// <summary>Player collects bills from the cash pile.</summary>
    public class CashZone : Zone
    {
        public CashPile cash;
        public float interval = 0.025f;

        /// <summary>Bills flying from a pile to the player right now (saved with the piles, see <see cref="CashPile.TotalAll"/>).</summary>
        public static long InFlight { get; private set; }

        int _pendingText;
        int _streak;
        float _textTimer;

        protected override void Awake()
        {
            base.Awake();
            playerOnly = true;
        }

        protected override void Update()
        {
            base.Update();
            if (_pendingText > 0)
            {
                _textTimer -= Time.deltaTime;
                if (_textTimer <= 0f)
                {
                    var p = GameRefs.I.player;
                    FloatingText.Show("+$" + _pendingText, p.transform.position + Vector3.up * 2.2f, new Color(0.45f, 1f, 0.45f), 1.1f);
                    _pendingText = 0;
                }
            }
        }

        protected override void OnPlayerExit() => _streak = 0;

        void OnDestroy() => InFlight = 0;

        protected override float TickCarrier(Carrier c, float timer)
        {
            if (cash == null || timer < 0.1f + interval) return timer;
            while (timer >= 0.1f + interval)
            {
                timer -= interval;
                int overflow = cash.TakeOverflow();
                if (overflow > 0)
                {
                    GameManager.I.AddMoney(overflow);
                    AddText(overflow);
                }
                var bill = cash.TakeBill();
                if (bill == null) break;
                int value = bill.value;
                var target = c.transform;
                InFlight += value;
                Tweener.Arc(bill.transform, () => target.position + Vector3.up * 1.1f, 1.2f, 0.28f, () =>
                {
                    InFlight -= value;
                    GameManager.I.AddMoney(value);
                    Sfx.Play(SfxId.Coin, 0.45f, Random.Range(0.95f, 1.15f) + Mathf.Min(_streak, 20) * 0.015f);
                    _streak++;
                    if (HUD.I != null && (_streak % 2) == 1) HUD.I.FlyCoins(target.position + Vector3.up * 1.3f, 1);
                    if ((_streak % 6) == 0) Fx.Coins(target.position + Vector3.up * 1.4f, 3);
                    bill.Despawn();
                }, null, Vector3.one * 0.4f);
                AddText(value);
            }
            return timer;
        }

        void AddText(int v)
        {
            if (_pendingText == 0) _textTimer = 0.35f;
            _pendingText += v;
        }
    }
}
