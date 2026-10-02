using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Stand on it to pour money in. When fully paid it reveals new stations and the next unlock pads.
    /// </summary>
    public class UnlockZone : Zone
    {
        /// <summary>The pad the player is standing on (null when none).</summary>
        public static UnlockZone Current { get; private set; }

        public string id;
        public int price = 50;
        public string title;
        public bool startVisible;
        public GameObject[] reveal;
        public UnlockZone[] next;
        public Transform focusPoint;

        [Header("Visuals")]
        public TextMeshPro priceText;
        public TextMeshPro groundPriceText;
        public TextMeshPro titleText;
        public Transform fill;
        [Tooltip("Depth of the fill area as a fraction of the pad.")]
        public float fillDepth = 1f;
        public SpriteRenderer icon;
        public Transform label;

        [Header("Glow pad (Tropical style)")]
        [Tooltip("Radial progress ring (world-space UI image, Filled / Radial360).")]
        public UnityEngine.UI.Image radialFill;
        [Tooltip("Soft glowing ring that breathes while the pad waits.")]
        public SpriteRenderer glow;
        [Tooltip("Discovery line shown under the NEW! banner.")]
        public string subtitle;
        [Tooltip("Glide the camera to the revealed area after unlocking.")]
        public bool peekOnUnlock;

        public int Paid { get; private set; }
        public int Remaining => Mathf.Max(0, price - Paid);
        public bool IsUnlocked { get; private set; }

        float _acc;
        float _glowA = -1f;
        float _coinTimer;
        float _glintTimer;
        float _stuckTime;
        Vector3 _baseScale;
        Vector3 _labelBase;
        float _glowScale = 1f;
        int _shownRemaining = -1;

        /// <summary>Seconds the player has been standing here unable to pay (drives the ad offer).</summary>
        public float StuckTime => _stuckTime;

        protected override void Awake()
        {
            base.Awake();
            playerOnly = true;
            _baseScale = transform.localScale;
            if (label != null) _labelBase = label.localPosition;
            if (glow != null) _glowScale = glow.transform.localScale.x;
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
                Fx.Ring(transform.position, new Color(1f, 1f, 1f, 0.7f), 3.2f);
                Sfx.Play(SfxId.Whoosh, 0.25f, 1.2f);
            }
        }

        protected override void Update()
        {
            base.Update();
            bool afford = GameManager.I != null && GameManager.I.Money >= Remaining && Remaining > 0;
            if (glow != null)
            {
                if (_glowA < 0f) _glowA = glow.color.a;
                float k = 0.65f + Mathf.Sin(Time.time * (afford ? 4.5f : 2f)) * 0.35f;
                var c = glow.color;
                c.a = _glowA * (afford ? Mathf.Lerp(0.8f, 1.2f, k) : Mathf.Lerp(0.45f, 0.8f, k));
                glow.color = c;
                float s = 1f + k * (afford ? 0.06f : 0.03f);
                glow.transform.localScale = new Vector3(s, s, 1f) * _glowScale;
            }
            // Bob the price label; bob faster when the player can afford it.
            if (label != null)
            {
                float amp = afford ? 0.12f : 0.05f;
                label.localPosition = _labelBase + Vector3.up * (Mathf.Sin(Time.time * (afford ? 5f : 2f)) * amp);
            }
            // Twinkle around the edge of a pad the player can afford, so it catches the eye from across the map.
            if (afford && !PlayerInside)
            {
                _glintTimer -= Time.deltaTime;
                if (_glintTimer <= 0f)
                {
                    _glintTimer = Random.Range(0.22f, 0.4f);
                    float a = Random.value * Mathf.PI * 2f;
                    var edge = new Vector3(Mathf.Cos(a) * size.x * 0.5f, Random.Range(0.15f, 0.6f), Mathf.Sin(a) * size.y * 0.5f);
                    Fx.Glint(transform.TransformPoint(edge), new Color(1f, 0.93f, 0.55f), 1);
                }
            }
        }

        protected override void OnPlayerEnter()
        {
            Current = this;
            _stuckTime = 0f;
            ShowPriceOnLabel(true);
        }

        protected override void OnPlayerExit()
        {
            if (Current == this) Current = null;
            _stuckTime = 0f;
            ShowPriceOnLabel(false);
        }

        /// <summary>The floating sign shows the name; while the player stands on the tile (hiding the painted price) it shows the price left.</summary>
        void ShowPriceOnLabel(bool price)
        {
            if (priceText == null || titleText == null) return;
            priceText.gameObject.SetActive(price);
            titleText.gameObject.SetActive(!price);
            if (label != null) Tweener.Punch(label, 0.12f, 0.2f, Vector3.one);
        }

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        protected override float TickCarrier(Carrier c, float timer)
        {
            if (IsUnlocked || timer < 0.35f) return timer;
            // Already paid in full (a price lowered by a balance update after the player paid): open it.
            if (Paid >= price)
            {
                Unlock();
                return timer;
            }
            // No spending while a popup or ad is up: the player is frozen on the pad, not choosing to pay.
            if (Platform.Paused) return timer;
            var gm = GameManager.I;
            if (gm.Money <= 0)
            {
                _stuckTime += Time.deltaTime;
                return timer;
            }
            _stuckTime = 0f;

            float rate = Mathf.Max(25f, price / 1.6f);
            // Accelerate the longer the player stands here.
            rate *= 1f + Mathf.Clamp01((timer - 0.35f) / 2f) * 2f;
            _acc += rate * Time.deltaTime;
            int want = Mathf.FloorToInt(_acc);
            if (want <= 0) return timer;
            _acc -= want;
            int pay = (int)System.Math.Min(System.Math.Min((long)want, (long)(price - Paid)), gm.Money);
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
                    Sfx.Play(SfxId.Coin, 0.22f, 1.1f + Paid / (float)Mathf.Max(1, price) * 0.6f);
                }, null, Vector3.one * 0.3f);
            }

            if (Paid >= price) Unlock();
            return timer;
        }

        /// <summary>Rewarded-ad assist: pays whatever is left.</summary>
        public void CompleteForFree()
        {
            if (IsUnlocked) return;
            Paid = price;
            GameManager.I.SetPaid(id, Paid);
            RefreshVisual();
            Fx.Coins(transform.position + Vector3.up * 1f, 16);
            Unlock();
        }

        void RefreshVisual()
        {
            int remaining = Remaining;
            // Texts only change with the amount shown (TMP rebuilds its mesh on every assignment).
            if (remaining != _shownRemaining)
            {
                _shownRemaining = remaining;
                if (priceText != null) priceText.text = "$" + Format(remaining);
                if (groundPriceText != null) groundPriceText.text = Format(remaining);
                if (titleText != null && titleText.text != title) titleText.text = title;
            }
            if (radialFill != null) radialFill.fillAmount = price <= 0 ? 1f : Mathf.Clamp01(Paid / (float)price);
            if (fill != null)
            {
                float f = price <= 0 ? 1f : Mathf.Clamp01(Paid / (float)price);
                var s = fill.localScale;
                s.z = Mathf.Max(0.0001f, f * fillDepth);
                fill.localScale = s;
                fill.localPosition = new Vector3(0f, fill.localPosition.y, -fillDepth * 0.5f + f * fillDepth * 0.5f);
            }
        }

        public static string Format(long v)
        {
            if (v >= 1000000000) return (v / 1000000000f).ToString("0.#") + "B";
            if (v >= 1000000) return (v / 1000000f).ToString("0.#") + "M";
            if (v >= 10000) return (v / 1000f).ToString("0.#") + "K";
            return v.ToString();
        }

        void Unlock()
        {
            IsUnlocked = true;
            if (Current == this) Current = null;
            GameManager.I.MarkUnlocked(id);
            Analytics.Log(Analytics.Unlock, "id", id, "price", price, "count", GameManager.I.UnlockedCount);
            Haptics.Play(HapticKind.Medium);
            Sfx.Play(SfxId.Unlock, 0.8f);
            Sfx.Play(SfxId.Sparkle, 0.4f);

            Vector3 fx = focusPoint != null ? focusPoint.position : transform.position;
            Fx.Confetti(fx + Vector3.up * 0.5f);
            Fx.Stars(fx + Vector3.up * 0.8f, 16);
            Fx.Ring(transform.position, new Color(1f, 0.9f, 0.45f, 0.9f), 6f);
            CameraFollow.Shake(0.15f, 0.25f);
            CameraFollow.Punch(0.08f);

            float delay = 0f;
            foreach (var r in reveal)
            {
                if (r == null) continue;
                var tr = r.transform;
                Vector3 target = tr.localScale;
                r.SetActive(true);
                Tweener.Scale(tr, Vector3.zero, target, 0.6f, Ease.OutBack, null, delay);
                tr.localScale = Vector3.zero;
                var p = tr.position;
                Tweener.Delay(delay + 0.05f, () =>
                {
                    Fx.Poof(p + Vector3.up * 0.3f, 8);
                    Fx.Ring(p, new Color(1f, 1f, 1f, 0.6f), 4.5f);
                });
                delay += 0.08f;
            }

            if (UnlockBanner.I != null) UnlockBanner.I.Show(title, icon != null ? icon.sprite : null, subtitle);

            // Reveal: glide over to what just appeared if it is not already in view.
            if (peekOnUnlock && GameRefs.I != null && GameRefs.I.player != null)
            {
                Vector3 d = fx - GameRefs.I.player.transform.position;
                d.y = 0f;
                if (d.magnitude > 5f) Tweener.Delay(0.35f, () => CameraFollow.Peek(fx, 1.1f, 1.1f));
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
