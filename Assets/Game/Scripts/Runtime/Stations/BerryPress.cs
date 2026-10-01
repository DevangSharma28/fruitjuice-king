using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Berry Blast juicer, a berry crusher: berries heaped in the hopper feed two spiked rollers that spin against each
    /// other and thump four times per batch, juice pulses through a glass pipe into a tank that fills with the batch
    /// (<see cref="Juicer.liquid"/>), and the spout pours a stream into every bottle. Same production rules as
    /// <see cref="Juicer"/>; only the show is richer.
    /// </summary>
    public class BerryPress : Juicer
    {
        [Header("Berry crusher")]
        [Tooltip("Roller assembly: dips on every crush stroke.")]
        public Transform crusher;
        [Tooltip("Front and back roller (they spin in opposite directions).")]
        public Transform[] rollers;
        public Transform pulp;
        [Tooltip("Between the rollers: juice drops spray from here.")]
        public Transform nip;
        public Transform heap;
        [Tooltip("Berries on the heap; as many show as are queued in the crate.")]
        public Transform[] heapBerries;
        [Tooltip("Blobs of juice that travel along the pipe while crushing.")]
        public Transform[] pipeBlobs;
        public Transform pipeStart, pipeEnd;
        [Tooltip("Juice pouring from the spout (scaled in Y).")]
        public Transform stream;
        public Renderer[] progressLights;
        public Material lightOn, lightOff;
        public Transform crown;

        const int Strokes = 4;

        float _spin;
        float _flow;
        float _dropT;
        int _litShown = -1;
        int _heapShown = -1;
        Vector3 _crusherPos, _pulpScale, _crownScale, _heapScale;
        Vector3[] _heapPos;

        protected override void Awake()
        {
            base.Awake();
            if (crusher != null) _crusherPos = crusher.localPosition;
            if (pulp != null) _pulpScale = pulp.localScale;
            if (crown != null) _crownScale = crown.localScale;
            if (heap != null) _heapScale = heap.localScale;
            if (heapBerries != null)
            {
                _heapPos = new Vector3[heapBerries.Length];
                for (int i = 0; i < heapBerries.Length; i++)
                    if (heapBerries[i] != null) _heapPos[i] = heapBerries[i].localPosition;
            }
            if (stream != null) stream.localScale = new Vector3(1f, 0f, 1f);
            SetBlobs(0f);
        }

        protected override void OnBatchStart()
        {
            if (heap != null) Tweener.Punch(heap, 0.12f, 0.25f, _heapScale);
            if (crown != null) Tweener.Punch(crown, 0.15f, 0.3f, _crownScale);
        }

        protected override void AnimateWork(bool working, float progress, float dt)
        {
            float time = Time.time;

            // Rollers spin against each other: fast while crushing, a lazy idle turn otherwise.
            _spin = Mathf.MoveTowards(_spin, working ? 540f * Boosts.WorkMult : 20f, dt * 900f);
            if (rollers != null)
                for (int i = 0; i < rollers.Length; i++)
                    if (rollers[i] != null) rollers[i].Rotate((i == 0 ? 1f : -1f) * _spin * dt, 0f, 0f, Space.Self);

            // Crush strokes: the roller assembly thumps down and the pulp squishes out.
            float stroke = working ? Mathf.Pow(Mathf.Abs(Mathf.Sin(progress * Mathf.PI * Strokes)), 6f) : 0f;
            if (crusher != null) crusher.localPosition = _crusherPos + Vector3.down * (stroke * 0.06f);
            if (pulp != null)
            {
                float k = working ? 1f + stroke * 0.25f : 0.6f;
                pulp.localScale = new Vector3(_pulpScale.x * k, _pulpScale.y, _pulpScale.z * k);
            }

            // Juice sprays from the nip now and then while crushing.
            if (working && nip != null)
            {
                _dropT -= dt;
                if (_dropT <= 0f)
                {
                    _dropT = 0.28f;
                    if (NearPlayer()) Fx.Drops(nip.position, Balance.JuiceColors[(int)kind], 2);
                }
            }

            UpdateHeap(working, time);

            // Juice pulses along the pipe into the tank while crushing.
            _flow = Mathf.MoveTowards(_flow, working ? 1f : 0f, dt * 3f);
            SetBlobs(_flow);

            // Light row fills with the batch.
            if (progressLights != null && progressLights.Length > 0)
            {
                int lit = working ? Mathf.CeilToInt(progress * progressLights.Length) : 0;
                if (lit != _litShown)
                {
                    _litShown = lit;
                    for (int i = 0; i < progressLights.Length; i++)
                        if (progressLights[i] != null) progressLights[i].sharedMaterial = i < lit ? lightOn : lightOff;
                }
            }
        }

        /// <summary>The heap shows the queued berries (two being crushed while working); they jostle while the rollers run.</summary>
        void UpdateHeap(bool working, float time)
        {
            if (heapBerries == null || _heapPos == null) return;
            int show = Mathf.Min(heapBerries.Length, inputPile.Count + (working ? 2 : 0));
            if (show != _heapShown)
            {
                _heapShown = show;
                for (int i = 0; i < heapBerries.Length; i++)
                    if (heapBerries[i] != null) heapBerries[i].gameObject.SetActive(i < show);
            }
            for (int i = 0; i < show; i++)
            {
                var b = heapBerries[i];
                if (b == null) continue;
                b.localPosition = working
                    ? _heapPos[i] + new Vector3(Mathf.Sin(time * 11f + i * 1.7f) * 0.015f, Mathf.Abs(Mathf.Sin(time * 14f + i)) * 0.03f, 0f)
                    : _heapPos[i];
            }
        }

        void SetBlobs(float flow)
        {
            if (pipeBlobs == null || pipeStart == null || pipeEnd == null) return;
            bool on = flow > 0.01f;
            for (int i = 0; i < pipeBlobs.Length; i++)
            {
                var b = pipeBlobs[i];
                if (b == null) continue;
                if (b.gameObject.activeSelf != on) b.gameObject.SetActive(on);
                if (!on) continue;
                float t = Mathf.Repeat(Time.time * 1.6f + i / (float)pipeBlobs.Length, 1f);
                b.position = Vector3.Lerp(pipeStart.position, pipeEnd.position, t);
                b.localScale = Vector3.one * ((0.7f + 0.3f * Mathf.Sin(t * Mathf.PI)) * flow * 0.08f);
            }
        }

        protected override void OnCupMade()
        {
            if (crown != null) Tweener.Punch(crown, 0.2f, 0.35f, _crownScale);
            var col = Balance.JuiceColors[(int)kind];
            Fx.Splash(spoutPoint.position + Vector3.up * 0.1f, col, 8);
            Fx.Stars(spoutPoint.position + Vector3.up * 0.5f, 3, Color.Lerp(col, Color.white, 0.3f));
            // Pour: the stream shoots down, holds, then thins away.
            if (stream != null)
                Tweener.Value(stream, 0.45f, t =>
                {
                    float len = t < 0.25f ? t / 0.25f : t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                    stream.localScale = new Vector3(1f, 0.32f * len, 1f);
                }, () => stream.localScale = new Vector3(1f, 0f, 1f));
        }
    }
}
