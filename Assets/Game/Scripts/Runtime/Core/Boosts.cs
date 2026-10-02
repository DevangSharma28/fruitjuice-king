using System;
using UnityEngine;

namespace JuiceKing
{
    public enum BoostKind { Cash2x = 0, Turbo = 1 }

    /// <summary>
    /// Timed rewarded-ad boosts. Everything reads the static multipliers, which fall back to 1 when no instance exists.
    /// Time left is saved (<see cref="SaveData.boostCash"/>...), so a boost the player watched an ad for survives a
    /// restart or a world change, and it only runs down while the game is being played (not under menus or ads).
    /// </summary>
    public class Boosts : MonoBehaviour
    {
        public static Boosts I { get; private set; }

        public static event Action Changed;

        readonly float[] _remaining = new float[2];
        float _freeCashCooldown;
        float _assistCooldown;

        public static float MoneyMult => IsActive(BoostKind.Cash2x) ? Balance.CashBoostMult : 1f;
        public static float MoveMult => IsActive(BoostKind.Turbo) ? Balance.TurboMoveMult : 1f;
        /// <summary>Speed factor for machines, helpers and customer arrivals.</summary>
        public static float WorkMult => IsActive(BoostKind.Turbo) ? Balance.TurboWorkMult : 1f;

        public static bool IsActive(BoostKind k) => I != null && I._remaining[(int)k] > 0f;
        public static float Remaining(BoostKind k) => I != null ? I._remaining[(int)k] : 0f;
        public static float Duration(BoostKind k) => k == BoostKind.Cash2x ? Balance.CashBoostSeconds : Balance.TurboSeconds;

        public static float FreeCashCooldown => I != null ? I._freeCashCooldown : 0f;
        public static float AssistCooldown => I != null ? I._assistCooldown : 0f;

        bool _restored;

        // GameManager (execution order -100) has loaded the save by now. Restoring here, not in Start, matters: any
        // save made between this Awake and a Start would otherwise write empty timers over the saved ones.
        void Awake()
        {
            I = this;
            var gm = GameManager.I;
            if (gm == null || GameManager.Redirecting) return;
            _restored = true;
            var d = gm.data;
            _remaining[(int)BoostKind.Cash2x] = Mathf.Clamp(d.boostCash, 0f, Balance.BoostMaxSeconds);
            _remaining[(int)BoostKind.Turbo] = Mathf.Clamp(d.boostTurbo, 0f, Balance.BoostMaxSeconds);
            _freeCashCooldown = Mathf.Clamp(d.freeCashCooldown, 0f, Balance.FreeCashCooldown);
            _assistCooldown = Mathf.Clamp(d.assistCooldown, 0f, Balance.UnlockAssistCooldown);
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        /// <summary>Copies the timers into the save (GameManager calls this when it saves).</summary>
        public void WriteTo(SaveData d)
        {
            if (!_restored) return;
            d.boostCash = _remaining[(int)BoostKind.Cash2x];
            d.boostTurbo = _remaining[(int)BoostKind.Turbo];
            d.freeCashCooldown = _freeCashCooldown;
            d.assistCooldown = _assistCooldown;
        }

        void Update()
        {
            // Menus, popups and ads pause the boosts: the player paid for play time, not for reading a panel.
            float dt = Platform.Paused ? 0f : Time.deltaTime;
            for (int i = 0; i < _remaining.Length; i++)
            {
                if (_remaining[i] <= 0f) continue;
                _remaining[i] -= dt;
                if (_remaining[i] <= 0f)
                {
                    _remaining[i] = 0f;
                    Changed?.Invoke();
                }
            }
            // Cooldowns run in real time (they gate ads, not play).
            float rt = Time.unscaledDeltaTime;
            if (_freeCashCooldown > 0f) _freeCashCooldown -= rt;
            if (_assistCooldown > 0f) _assistCooldown -= rt;
        }

        public static void Grant(BoostKind k)
        {
            if (I == null) return;
            ref float r = ref I._remaining[(int)k];
            r = Mathf.Min(Balance.BoostMaxSeconds, Mathf.Max(0f, r) + Duration(k));
            Changed?.Invoke();
        }

        public static void StartFreeCashCooldown()
        {
            if (I != null) I._freeCashCooldown = Balance.FreeCashCooldown;
            Changed?.Invoke();
        }

        public static void StartAssistCooldown()
        {
            if (I != null) I._assistCooldown = Balance.UnlockAssistCooldown;
            Changed?.Invoke();
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
