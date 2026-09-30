using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Berry Blast juicer: berries tumble in a glass drum, a piston presses them, juice pulses through a clear pipe and a
    /// gauge sweeps round as the bottle fills. Same production rules as <see cref="Juicer"/>; only the show is richer.
    /// </summary>
    public class BerryPress : Juicer
    {
        [Header("Berry press")]
        public Transform drum;
        public Transform[] drumBerries;
        public Transform piston;
        [Tooltip("Blobs of juice that travel down the pipe while pressing.")]
        public Transform[] pipeBlobs;
        public Transform pipeStart, pipeEnd;
        public Transform gaugeNeedle;
        public Renderer[] ringLights;
        public Material ringOn, ringOff;
        public Transform crown;

        float _drumSpin;
        Vector3 _pistonPos;
        Vector3 _crownScale;
        float _flow;
        int _litShown = -1;

        protected override void Awake()
        {
            base.Awake();
            if (piston != null) _pistonPos = piston.localPosition;
            if (crown != null) _crownScale = crown.localScale;
            SetBlobs(0f);
        }

        protected override void OnBatchStart()
        {
            if (drum != null) Tweener.Punch(drum, 0.08f, 0.25f, Vector3.one);
            if (crown != null) Tweener.Punch(crown, 0.15f, 0.3f, _crownScale);
        }

        protected override void AnimateWork(bool working, float progress, float dt)
        {
            // Drum tumbles the berries.
            _drumSpin = Mathf.MoveTowards(_drumSpin, working ? 260f * Boosts.WorkMult : 12f, dt * 400f);
            if (drum != null) drum.Rotate(_drumSpin * dt, 0f, 0f, Space.Self);
            if (drumBerries != null && working)
                for (int i = 0; i < drumBerries.Length; i++)
                    if (drumBerries[i] != null) drumBerries[i].localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(Time.time * 17f + i));

            // Piston: three strokes per batch, easing hard at the bottom of each.
            if (piston != null)
            {
                float stroke = working ? Mathf.Pow(Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 3f)), 3f) : 0f;
                piston.localPosition = _pistonPos + Vector3.down * (stroke * 0.22f);
            }

            // Juice pulses along the pipe while pressing.
            _flow = Mathf.MoveTowards(_flow, working ? 1f : 0f, dt * 3f);
            SetBlobs(_flow);

            if (gaugeNeedle != null) gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, 70f - (working ? progress : 0f) * 140f);

            // Ring of lights fills with the batch.
            if (ringLights != null && ringLights.Length > 0)
            {
                int lit = working ? Mathf.CeilToInt(progress * ringLights.Length) : 0;
                if (lit != _litShown)
                {
                    _litShown = lit;
                    for (int i = 0; i < ringLights.Length; i++)
                        if (ringLights[i] != null) ringLights[i].sharedMaterial = i < lit ? ringOn : ringOff;
                }
            }
        }

        void SetBlobs(float flow)
        {
            if (pipeBlobs == null || pipeStart == null || pipeEnd == null) return;
            for (int i = 0; i < pipeBlobs.Length; i++)
            {
                var b = pipeBlobs[i];
                if (b == null) continue;
                bool on = flow > 0.01f;
                if (b.gameObject.activeSelf != on) b.gameObject.SetActive(on);
                if (!on) continue;
                float t = Mathf.Repeat(Time.time * 1.6f + i / (float)pipeBlobs.Length, 1f);
                b.position = Vector3.Lerp(pipeStart.position, pipeEnd.position, t);
                b.localScale = Vector3.one * (0.7f + 0.3f * Mathf.Sin(t * Mathf.PI)) * flow;
            }
        }

        protected override void OnCupMade()
        {
            if (crown != null) Tweener.Punch(crown, 0.2f, 0.35f, _crownScale);
            var col = Balance.JuiceColors[(int)kind];
            Fx.Splash(spoutPoint.position + Vector3.up * 0.1f, col, 8);
            Fx.Stars(spoutPoint.position + Vector3.up * 0.5f, 3, Color.Lerp(col, Color.white, 0.3f));
        }
    }
}
