using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceKing
{
    public enum BoostButtonKind { Cash2x, Turbo, FreeCash }

    [Serializable]
    public class BoostButton
    {
        public BoostButtonKind kind;
        public Button button;
        public RectTransform rect;
        public Image icon;
        public Image ring;
        public TextMeshProUGUI label;
        public GameObject adBadge;
        [NonSerialized] public float wiggleT;
    }

    /// <summary>
    /// Rewarded-ad boosts on the side of the screen: 2x cash, turbo and a free cash bag.
    /// Also owns the "get it free" chip shown while the player stands on an unlock pad they cannot afford.
    /// </summary>
    public class BoostBar : MonoBehaviour
    {
        public BoostButton[] buttons;
        public RectTransform root;
        public Sprite cashIcon, turboIcon, freeCashIcon, unlockIcon;

        [Header("Unlock assist chip")]
        public Button assistButton;
        public RectTransform assistRect;
        public TextMeshProUGUI assistText;

        [Tooltip("Tutorial step after which boosts appear.")]
        public int showFromStep = 5;

        bool _shown;
        bool _assistShown;
        float _assistShownAt;

        void Start()
        {
            foreach (var b in buttons)
            {
                var bb = b;
                bb.button.onClick.AddListener(() => OnTap(bb));
                bb.wiggleT = UnityEngine.Random.Range(1f, 4f);
            }
            if (assistButton != null)
            {
                assistButton.onClick.AddListener(OnAssist);
                assistRect.gameObject.SetActive(false);
            }
            root.gameObject.SetActive(false);
            TryOfferOffline();
        }

        void Update()
        {
            var gm = GameManager.I;
            bool want = gm.TutorialStep >= showFromStep;
            if (want && !_shown)
            {
                _shown = true;
                root.gameObject.SetActive(true);
                float d = 0f;
                foreach (var b in buttons)
                {
                    Tweener.Scale(b.rect, Vector3.zero, Vector3.one, 0.5f, Ease.OutBack, null, d);
                    d += 0.1f;
                }
            }
            if (_shown) foreach (var b in buttons) Refresh(b);
            UpdateAssist();
        }

        void Refresh(BoostButton b)
        {
            float dt = Time.deltaTime;
            bool available;
            switch (b.kind)
            {
                case BoostButtonKind.Cash2x:
                case BoostButtonKind.Turbo:
                {
                    var k = b.kind == BoostButtonKind.Cash2x ? BoostKind.Cash2x : BoostKind.Turbo;
                    bool active = Boosts.IsActive(k);
                    float rem = Boosts.Remaining(k);
                    if (b.ring != null)
                    {
                        b.ring.enabled = active;
                        b.ring.fillAmount = Mathf.Clamp01(rem / Boosts.Duration(k));
                    }
                    b.label.text = active ? Boosts.FormatTime(rem) : (b.kind == BoostButtonKind.Cash2x ? "2x CASH" : "TURBO");
                    available = Ads.IsReady;
                    if (b.adBadge != null) b.adBadge.SetActive(!active);
                    break;
                }
                default:
                {
                    float cd = Boosts.FreeCashCooldown;
                    available = cd <= 0f && Ads.IsReady;
                    b.label.text = cd > 0f ? Boosts.FormatTime(cd) : "+$" + UnlockZone.Format(FreeCashAmount());
                    if (b.ring != null)
                    {
                        b.ring.enabled = cd > 0f;
                        b.ring.fillAmount = 1f - Mathf.Clamp01(cd / Balance.FreeCashCooldown);
                    }
                    if (b.adBadge != null) b.adBadge.SetActive(cd <= 0f);
                    break;
                }
            }

            b.button.interactable = available;
            if (b.icon != null) b.icon.color = available ? Color.white : new Color(1f, 1f, 1f, 0.5f);

            // Periodic attention wiggle while an offer is waiting.
            if (available && !(b.kind != BoostButtonKind.FreeCash && Boosts.IsActive(b.kind == BoostButtonKind.Cash2x ? BoostKind.Cash2x : BoostKind.Turbo)))
            {
                b.wiggleT -= dt;
                if (b.wiggleT <= 0f)
                {
                    b.wiggleT = UnityEngine.Random.Range(4f, 7f);
                    var rt = b.rect;
                    Tweener.Value(rt, 0.6f, t => rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * 14f),
                        () => rt.localRotation = Quaternion.identity);
                }
            }
        }

        int FreeCashAmount() => Balance.FreeCash(GameManager.I.UnlockedCount);

        void OnTap(BoostButton b)
        {
            if (OfferPopup.I == null || OfferPopup.I.IsOpen) return;
            Sfx.Play(SfxId.Click, 0.6f);
            Tweener.Punch(b.rect, 0.2f, 0.2f, Vector3.one);
            switch (b.kind)
            {
                case BoostButtonKind.Cash2x:
                    OfferPopup.I.Show("2x CASH", $"Double all sales for <b>{Boosts.FormatTime(Balance.CashBoostSeconds)}</b>!", cashIcon,
                        "WATCH", true, () => Ads.ShowRewarded(Ads.PlacementCash2x, () => GrantBoost(BoostKind.Cash2x, b)));
                    break;
                case BoostButtonKind.Turbo:
                    OfferPopup.I.Show("TURBO", $"Run faster, juice faster and get more customers for <b>{Boosts.FormatTime(Balance.TurboSeconds)}</b>!", turboIcon,
                        "WATCH", true, () => Ads.ShowRewarded(Ads.PlacementTurbo, () => GrantBoost(BoostKind.Turbo, b)));
                    break;
                default:
                    int amount = FreeCashAmount();
                    OfferPopup.I.Show("FREE CASH", $"Grab a bag of <b>${UnlockZone.Format(amount)}</b>!", freeCashIcon,
                        "WATCH", true, () => Ads.ShowRewarded(Ads.PlacementFreeCash, () =>
                        {
                            Boosts.StartFreeCashCooldown();
                            GiveCash(amount, RectTransformUtility.WorldToScreenPoint(null, b.rect.position));
                        }));
                    break;
            }
        }

        void GrantBoost(BoostKind k, BoostButton b)
        {
            Boosts.Grant(k);
            Sfx.Play(SfxId.Reward, 0.7f);
            Tweener.Punch(b.rect, 0.35f, 0.4f, Vector3.one);
            var p = GameRefs.I.player.transform.position;
            Fx.Stars(p + Vector3.up * 1.5f, 16, k == BoostKind.Turbo ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.85f, 0.25f));
            Fx.Ring(p, k == BoostKind.Turbo ? new Color(0.5f, 0.85f, 1f, 0.9f) : new Color(1f, 0.85f, 0.25f, 0.9f), 5f);
            FloatingText.Show(k == BoostKind.Turbo ? "TURBO!" : "2x CASH!", p + Vector3.up * 2.6f, new Color(1f, 0.9f, 0.3f), 1.3f, 1.2f, 1.2f);
        }

        static Vector2 ScreenCenter => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        static void GiveCash(int amount, Vector2 fromScreen)
        {
            GameManager.I.AddMoney(amount);
            Sfx.Play(SfxId.Reward, 0.7f);
            if (HUD.I != null) HUD.I.FlyCoinsFromScreen(fromScreen, 12);
            var p = GameRefs.I.player.transform.position;
            Fx.Coins(p + Vector3.up * 1.5f, 14);
            FloatingText.Show("+$" + UnlockZone.Format(amount), p + Vector3.up * 2.4f, new Color(0.5f, 1f, 0.5f), 1.3f, 1.2f, 1.2f);
        }

        // ---------------------------------------------------------------- unlock assist

        UnlockZone AssistTarget()
        {
            var z = UnlockZone.Current;
            if (z == null || z.IsUnlocked || !Ads.IsReady || Boosts.AssistCooldown > 0f) return null;
            if (z.StuckTime < 0.8f) return null;
            // Only for the last stretch so ads never replace the core loop.
            if (z.Remaining > Mathf.Max(60, z.price / 2)) return null;
            return z;
        }

        void UpdateAssist()
        {
            if (assistRect == null) return;
            var z = AssistTarget();
            bool show = z != null && (OfferPopup.I == null || !OfferPopup.I.IsOpen);
            if (show != _assistShown)
            {
                _assistShown = show;
                if (show)
                {
                    assistRect.gameObject.SetActive(true);
                    _assistShownAt = Time.time;
                    Tweener.Scale(assistRect, Vector3.zero, Vector3.one, 0.4f, Ease.OutBack);
                    Sfx.Play(SfxId.Pop, 0.3f, 1.4f);
                }
                else Tweener.Scale(assistRect, assistRect.localScale, Vector3.zero, 0.15f, Ease.InQuad, () =>
                {
                    if (!_assistShown) assistRect.gameObject.SetActive(false);
                });
            }
            if (show && assistText != null) assistText.text = "FINISH  $" + UnlockZone.Format(z.Remaining);
            if (show && Time.time - _assistShownAt > 0.45f)
            {
                float s = 1f + Mathf.Sin(Time.time * 6f) * 0.04f;
                assistRect.localScale = Vector3.one * s;
            }
        }

        void OnAssist()
        {
            var z = AssistTarget();
            if (z == null) return;
            Sfx.Play(SfxId.Click, 0.6f);
            Ads.ShowRewarded(Ads.PlacementUnlock, () =>
            {
                Boosts.StartAssistCooldown();
                Sfx.Play(SfxId.Reward, 0.6f);
                z.CompleteForFree();
            });
        }

        // ---------------------------------------------------------------- offline earnings

        void TryOfferOffline()
        {
            var gm = GameManager.I;
            long amount = gm.OfflineEarnings();
            gm.ConsumeOffline();
            if (amount < 10 || OfferPopup.I == null) return;
            Tweener.Delay(0.6f, () =>
            {
                OfferPopup.I.Show("WELCOME BACK!", $"Your helpers made\n<size=140%><color=#2E9E3E>${UnlockZone.Format(amount)}</color></size>\nwhile you were away.",
                    freeCashIcon, "x2", true,
                    () => Ads.ShowRewarded(Ads.PlacementOffline, () => GiveCash((int)(amount * 2), ScreenCenter), () => GiveCash((int)amount, ScreenCenter)),
                    "Collect", () => GiveCash((int)amount, ScreenCenter));
            });
        }
    }
}
