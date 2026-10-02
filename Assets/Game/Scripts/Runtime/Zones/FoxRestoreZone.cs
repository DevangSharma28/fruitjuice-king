using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Glowing pad in front of a fox-raided farm with the regrow countdown: step on it to restore the farm.</summary>
    public class FoxRestoreZone : Zone
    {
        public FruitField field;
        public TextMeshPro timerText;
        public Transform label;
        public SpriteRenderer glow;

        int _shown = -1;
        Vector3 _labelBase;
        float _glowA = -1f;

        protected override void Awake()
        {
            base.Awake();
            playerOnly = true;
            if (label != null) _labelBase = label.localPosition;
        }

        protected override void Update()
        {
            base.Update();
            var raid = FoxRaid.I;
            if (raid == null || field == null) return;
            int s = Mathf.CeilToInt(raid.TimeLeft(field));
            if (s != _shown && timerText != null)
            {
                _shown = s;
                timerText.text = FoxRaid.Clock(s);
            }
            if (label != null) label.localPosition = _labelBase + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.08f);
            if (glow != null)
            {
                if (_glowA < 0f) _glowA = glow.color.a;
                var c = glow.color;
                c.a = _glowA * (0.6f + 0.4f * Mathf.Sin(Time.time * 4f));
                glow.color = c;
            }
        }

        protected override void OnPlayerEnter()
        {
            // Not while the first-raid cutscene still plays (the pad scales in under the letterbox).
            if (FoxRaid.I != null && !ExpansionIntro.Playing && !FoxRaid.I.Raiding) FoxRaid.I.OfferRestore(field);
        }

        protected override float TickCarrier(Carrier c, float timer) => timer;
    }
}
