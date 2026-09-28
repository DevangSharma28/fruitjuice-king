using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Stand on it to pour money in. When fully paid it reveals new stations and the next unlock pads.
    /// </summary>
    public class UnlockZone : Zone
    {
        public string id;
        public int price = 50;
        public string title;
        public bool startVisible;
        public GameObject[] reveal;
        public UnlockZone[] next;
        public Transform focusPoint;

        [Header("Visuals")]
        public TextMeshPro priceText;
        public TextMeshPro titleText;
        public Transform fill;
        public SpriteRenderer icon;

        public int Paid { get; private set; }
        public bool IsUnlocked { get; private set; }

        float _acc;
        float _coinTimer;
        Vector3 _baseScale;

        protected override void Awake()
        {
            base.Awake();
            playerOnly = true;
            _baseScale = transform.localScale;
        }

        /// <summary>Called by <see cref="UnlockManager"/> on load.</summary>
        public void InitState(bool unlocked, bool visible)
        {
            IsUnlocked = unlocked;
            Paid = unlocked ? price : Mathf.Clamp(GameManager.I.GetPaid(id), 0, price);
            foreach (var r in reveal)
                if (r != null) r.SetActive(unlocked);
            gameObject.SetActive(!unlocked && visible);
            RefreshVisual();
        }

        public void Show(bool animate)
        {
            if (IsUnlocked || gameObject.activeSelf) return;
            gameObject.SetActive(true);
            RefreshVisual();
            if (animate)
            {
                Tweener.Scale(transform, Vector3.zero, _baseScale, 0.5f, Ease.OutBack);
                Fx.Poof(transform.position + Vector3.up * 0.3f, 10);
            }
        }

        protected override float TickCarrier(Carrier c, float timer)
        {
            if (IsUnlocked || timer < 0.35f) return timer;
            var gm = GameManager.I;
            if (gm.Money <= 0) return timer;

            float rate = Mathf.Max(25f, price / 1.6f);
            // Accelerate the longer the player stands here.
            rate *= 1f + Mathf.Clamp01((timer - 0.35f) / 2f) * 2f;
            _acc += rate * Time.deltaTime;
            int want = Mathf.FloorToInt(_acc);
            if (want <= 0) return timer;
            _acc -= want;
            int pay = (int)Mathf.Min(want, price - Paid, gm.Money);
            if (pay <= 0) return timer;

            gm.AddMoney(-pay);
            Paid += pay;
            gm.SetPaid(id, Paid);
            RefreshVisual();

            _coinTimer -= Time.deltaTime;
            if (_coinTimer <= 0f)
            {
                _coinTimer = 0.05f;
                var bill = GameRefs.I.SpawnItem(ItemType.Money, c.transform.position + Vector3.up * 1.2f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                Tweener.Arc(bill.transform, () => transform.position, 1.2f, 0.3f, () =>
                {
                    bill.Despawn();
                    Sfx.Play(SfxId.Coin, 0.25f, 1.3f);
                }, null, Vector3.one * 0.3f);
            }

            if (Paid >= price) Unlock();
            return timer;
        }

        void RefreshVisual()
        {
            int remaining = Mathf.Max(0, price - Paid);
            if (priceText != null) priceText.text = "$" + Format(remaining);
            if (titleText != null) titleText.text = title;
            if (fill != null)
            {
                float f = price <= 0 ? 1f : Mathf.Clamp01(Paid / (float)price);
                var s = fill.localScale;
                s.z = Mathf.Max(0.0001f, f);
                fill.localScale = s;
                fill.localPosition = new Vector3(0f, fill.localPosition.y, -0.5f + f * 0.5f);
            }
        }

        public static string Format(long v)
        {
            if (v >= 1000000) return (v / 1000000f).ToString("0.#") + "M";
            if (v >= 10000) return (v / 1000f).ToString("0.#") + "K";
            return v.ToString();
        }

        void Unlock()
        {
            IsUnlocked = true;
            GameManager.I.MarkUnlocked(id);
            Sfx.Play(SfxId.Unlock, 0.8f);

            Vector3 fx = focusPoint != null ? focusPoint.position : transform.position;
            Fx.Confetti(fx + Vector3.up * 0.5f);
            CameraFollow.Shake(0.15f, 0.25f);

            float delay = 0f;
            foreach (var r in reveal)
            {
                if (r == null) continue;
                var tr = r.transform;
                Vector3 target = tr.localScale;
                r.SetActive(true);
                Tweener.Scale(tr, Vector3.zero, target, 0.55f, Ease.OutBack, null, delay);
                tr.localScale = Vector3.zero;
                delay += 0.08f;
            }

            UnlockManager.OnZoneUnlocked(this);
            Tweener.Scale(transform, _baseScale, Vector3.zero, 0.25f, Ease.InQuad, () =>
            {
                transform.localScale = _baseScale;
                gameObject.SetActive(false);
            });
        }
    }
}
