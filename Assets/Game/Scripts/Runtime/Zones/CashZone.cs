using UnityEngine;

namespace JuiceKing
{
    /// <summary>Player collects bills from the cash pile.</summary>
    public class CashZone : Zone
    {
        public CashPile cash;
        public float interval = 0.025f;

        int _pendingText;
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
                Tweener.Arc(bill.transform, () => target.position + Vector3.up * 1.1f, 1.2f, 0.28f, () =>
                {
                    GameManager.I.AddMoney(value);
                    Sfx.Play(SfxId.Coin, 0.5f, Random.Range(0.95f, 1.15f));
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
