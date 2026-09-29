using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Guides a new player through the core loop with an objective banner, a bouncing arrow
    /// over the target and a ground pointer next to the player. Afterwards it hints affordable unlocks.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public Transform arrow;
        public Transform pointer;
        [Tooltip("UI arrow clamped to the screen edge while the target is off-screen.")]
        public RectTransform edgeArrow;
        public RectTransform edgeArea;
        public FruitField firstField;
        public Juicer firstJuicer;
        public Counter counter;
        public CashPile cash;

        Carrier _player;
        Vector3 _arrowBase;
        Vector3 _pointerScale = Vector3.one;
        float _ringT;
        Vector3? _lastTarget;

        void Start()
        {
            _player = GameRefs.I.player;
            if (pointer != null) _pointerScale = pointer.localScale;
            if (arrow != null) _arrowScale = arrow.localScale;
            if (edgeArrow != null) edgeArrow.gameObject.SetActive(false);
            UnlockManager.ZoneUnlocked += OnUnlocked;
        }

        void OnDestroy() => UnlockManager.ZoneUnlocked -= OnUnlocked;

        void OnUnlocked(UnlockZone z)
        {
            if (GameManager.I.TutorialStep == 5) GameManager.I.TutorialStep = 6;
        }

        void Update()
        {
            var gm = GameManager.I;
            int step = gm.TutorialStep;
            Vector3? target = null;
            string text = null;

            switch (step)
            {
                case 0:
                    text = "Cut the oranges with your chainsaw!";
                    var f = firstField.NearestReady(_player.transform.position);
                    target = f != null ? f.transform.position + Vector3.up * 1.2f : firstField.Center;
                    if (_player.CountOf(t => t.IsSlice()) >= 4 || _player.IsFull) Advance();
                    break;
                case 1:
                    text = "Drop the oranges into the juicer";
                    target = firstJuicer.inputZone.transform.position;
                    if (firstJuicer.inputPile.Count > 0 || firstJuicer.outputPile.Count > 0 || _player.Count == 0) Advance();
                    break;
                case 2:
                    text = "Grab the fresh juice!";
                    target = firstJuicer.outputZone.transform.position;
                    if (_player.Contains(t => t.IsJuice())) Advance();
                    break;
                case 3:
                    text = "Put the juice on the counter";
                    target = counter.dropZone.transform.position;
                    if (counter.display.Count > 0) Advance();
                    break;
                case 4:
                    text = "Customers pay here - collect the cash!";
                    target = cash.zone.transform.position;
                    if (gm.Money > 0) Advance();
                    break;
                case 5:
                    text = "Spend cash to unlock new stuff!";
                    var z = UnlockManager.FirstAvailable();
                    if (z != null) target = z.transform.position;
                    break;
                default:
                    // Free play: point at the cheapest pad the player can afford.
                    var next = UnlockManager.FirstAvailable();
                    if (next != null && gm.Money >= next.price - next.Paid && gm.Money > 0) target = next.transform.position;
                    break;
            }

            // Carrying slices nobody can juice yet: point at the trash bin so the player is never stuck.
            var bin = TrashBin.I;
            if (bin != null && bin.isActiveAndEnabled)
            {
                for (int i = 0; i < _player.items.Count; i++)
                {
                    var t = _player.items[i].type;
                    if (!t.IsSlice() || gm.HasJuicer(t.Fruit())) continue;
                    text = "No " + Balance.FruitNames[(int)t.Fruit()] + " juicer yet! Toss them in the trash";
                    target = bin.zone != null ? bin.zone.transform.position : bin.transform.position;
                    break;
                }
            }

            if (HUD.I != null) HUD.I.SetObjective(text);
            UpdateMarkers(target);
        }

        void Advance()
        {
            GameManager.I.TutorialStep++;
            Sfx.Play(SfxId.Click, 0.5f, 1.4f);
        }

        void UpdateMarkers(Vector3? target)
        {
            bool has = target.HasValue;
            float time = Time.time;
            if (arrow != null)
            {
                if (arrow.gameObject.activeSelf != has) arrow.gameObject.SetActive(has);
                if (has)
                {
                    Vector3 p = target.Value;
                    float bounce = Mathf.Abs(Mathf.Sin(time * 4f));
                    p.y = Mathf.Max(p.y, 0f) + 2.3f + bounce * 0.55f;
                    arrow.position = p;
                    arrow.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
                    // Squash when it "lands".
                    float sq = 1f + (1f - bounce) * 0.12f;
                    arrow.localScale = new Vector3(_arrowScale.x * sq, _arrowScale.y / sq, _arrowScale.z * sq);
                }
            }

            // Pulse a ring on the ground under the target so the spot itself reads, not just the arrow.
            if (has)
            {
                if (!_lastTarget.HasValue || (_lastTarget.Value - target.Value).sqrMagnitude > 0.5f) _ringT = 0f;
                _ringT -= Time.deltaTime;
                if (_ringT <= 0f)
                {
                    _ringT = 1.1f;
                    var g = target.Value;
                    g.y = 0f;
                    Fx.Ring(g, new Color(1f, 0.9f, 0.25f, 0.9f), 3.2f);
                }
            }
            _lastTarget = target;

            if (pointer != null)
            {
                bool show = false;
                if (has)
                {
                    Vector3 d = target.Value - _player.transform.position;
                    d.y = 0f;
                    show = d.magnitude > 3f;
                    if (show)
                    {
                        // Chevron slides out ahead of the player and pulses, like a "go this way" hint.
                        float slide = Mathf.Repeat(time * 1.4f, 1f);
                        pointer.position = _player.transform.position + d.normalized * (1.5f + slide * 0.7f) + Vector3.up * 0.09f;
                        pointer.rotation = Quaternion.LookRotation(d.normalized);
                        float pulse = 1f + Mathf.Sin(time * 8f) * 0.06f;
                        pointer.localScale = _pointerScale * pulse;
                    }
                }
                if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
            }

            UpdateEdgeArrow(target);
        }

        Vector3 _arrowScale = Vector3.one;

        /// <summary>Screen-edge arrow for targets outside the view.</summary>
        void UpdateEdgeArrow(Vector3? target)
        {
            if (edgeArrow == null || edgeArea == null) return;
            var cam = GameRefs.I != null ? GameRefs.I.mainCamera : Camera.main;
            bool show = false;
            if (target.HasValue && cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(target.Value + Vector3.up * 1f);
                bool behind = vp.z < 0f;
                if (behind) vp = new Vector3(1f - vp.x, 1f - vp.y, 0f);
                const float m = 0.08f;
                bool off = behind || vp.x < m || vp.x > 1f - m || vp.y < m + 0.05f || vp.y > 1f - m - 0.12f;
                if (off)
                {
                    show = true;
                    Vector2 c = new Vector2(0.5f, 0.5f);
                    Vector2 dir = new Vector2(vp.x, vp.y) - c;
                    if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
                    // Push the point onto the inset screen rectangle along the direction from the centre.
                    float kx = (0.5f - m) / Mathf.Max(0.0001f, Mathf.Abs(dir.x));
                    float ky = (0.5f - m - 0.08f) / Mathf.Max(0.0001f, Mathf.Abs(dir.y));
                    Vector2 edge = c + dir * Mathf.Min(kx, ky);
                    var r = edgeArea.rect;
                    edgeArrow.anchoredPosition = new Vector2((edge.x - 0.5f) * r.width, (edge.y - 0.5f) * r.height);
                    float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    edgeArrow.localRotation = Quaternion.Euler(0f, 0f, ang);
                    float s = 1f + Mathf.Sin(Time.time * 7f) * 0.1f;
                    edgeArrow.localScale = new Vector3(s, s, 1f);
                }
            }
            if (edgeArrow.gameObject.activeSelf != show) edgeArrow.gameObject.SetActive(show);
        }
    }
}
