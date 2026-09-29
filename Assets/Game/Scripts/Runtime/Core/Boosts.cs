using System;
using UnityEngine;

namespace JuiceKing
{
    public enum BoostKind { Cash2x = 0, Turbo = 1 }

    /// <summary>
    /// Timed rewarded-ad boosts. Everything reads the static multipliers, which fall back to 1 when no instance exists.
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

        void Awake() => I = this;

        void Update()
        {
            float dt = Time.deltaTime;
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
            if (_freeCashCooldown > 0f) _freeCashCooldown -= dt;
            if (_assistCooldown > 0f) _assistCooldown -= dt;
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
