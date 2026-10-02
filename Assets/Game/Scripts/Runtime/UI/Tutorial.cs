using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Guides a new player through the core loop with an objective banner, a bouncing arrow over the target and a
    /// ground pointer next to the player. Afterwards it keeps naming the next goal: the next unlock (and the money still
    /// needed), upgrades that are ready, and finally "max every upgrade" until the world is complete.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        /// <summary>First step of free play (the scripted lessons 0-5 are done).</summary>
        public const int FreePlayStep = 6;

        public Transform arrow;
        public Transform pointer;
        [Tooltip("UI arrow clamped to the screen edge while the target is off-screen.")]
        public RectTransform edgeArrow;
        public RectTransform edgeArea;
        public FruitField firstField;
        public Juicer firstJuicer;
        public Counter counter;
        public CashPile cash;
        [Tooltip("Upgrade shop pad (pointed at when an upgrade is affordable).")]
        public UpgradeZone upgradeZone;
        [Header("Berry Cake Shop (first cake guide)")]
        public CakeMixer firstCakeMixer;
        public Oven firstOven;
        public Counter cakeCounter;

        Carrier _player;
        Vector3 _arrowBase;
        float _arrowSpin;
        Vector3 _pointerScale = Vector3.one;
        float _ringT;
        Vector3? _lastTarget;
        int _lastStep = -1;
        long _stepEarned;
        // The objective text is rebuilt a few times a second, not every frame (string building allocates).
        float _thinkT;
        Vector3? _target;

        static readonly string[] Plural =
            { "oranges", "watermelons", "pineapples", "coconuts", "mangoes", "bananas", "papayas", "strawberries", "raspberries", "blueberries", "cranberries" };
        string MachineWord => Economy.World == 0 ? "juicer" : Economy.World == 1 ? "mixer" : "berry press";

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
            if (GameManager.I.TutorialStep == 5)
            {
                GameManager.I.TutorialStep = FreePlayStep;
                Analytics.Log(Analytics.TutorialComplete);
            }
            _thinkT = 0f;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (ExpansionIntro.Playing)
            {
                if (HUD.I != null) HUD.I.SetObjective(null);
                UpdateMarkers(null);
                return;
            }
            int step = gm.TutorialStep;
            if (step != _lastStep)
            {
                _lastStep = step;
                // Earnings, not the balance: paying for a pad meanwhile must not hold the step back.
                _stepEarned = gm.data.stats.earned;
                _thinkT = 0f;
            }
            _thinkT -= Time.deltaTime;
            if (_thinkT > 0f)
            {
                UpdateMarkers(CameraFollow.Busy ? null : _target);
                return;
            }
            _thinkT = 0.2f;
            Vector3? target = null;
            string text = null;
            string key = null;
            string fruits = Plural[(int)firstField.kind];

            switch (step)
            {
                case 0:
                    text = "Cut the " + fruits + " with your chainsaw!";
                    var f = firstField.NearestReady(_player.transform.position);
                    // Aim at the fruit itself (tropical fruit hangs high in the tree).
                    target = f != null ? f.visual.position : firstField.Center;
                    if (_player.CountOf(t => t.IsSlice()) >= 4 || _player.IsFull) Advance();
                    break;
                case 1:
                    text = "Drop the " + fruits + " into the " + MachineWord;
                    target = firstJuicer.inputZone.transform.position;
                    // Enough for a batch (or one already running) - a single slice would leave step 2 waiting forever.
                    if (firstJuicer.Working || firstJuicer.outputPile.Count > 0 ||
                        firstJuicer.inputPile.Count >= Balance.SlicesPerJuice[(int)firstJuicer.kind]) Advance();
                    else if (_player.Count == 0) gm.TutorialStep = 0;
                    break;
                case 2:
                    if (!firstJuicer.Working && firstJuicer.outputPile.Count == 0 && !_player.Contains(IsJuice) &&
                        firstJuicer.inputPile.Count < Balance.SlicesPerJuice[(int)firstJuicer.kind])
                    {
                        // Not enough in the machine for a cup: send the player back for more.
                        text = _player.Contains(IsSlice) ? "Drop more " + fruits + " into the " + MachineWord : "Cut more " + fruits + "!";
                        var nf = firstField.NearestReady(_player.transform.position);
                        target = _player.Contains(IsSlice) ? firstJuicer.inputZone.transform.position : nf != null ? nf.visual.position : firstField.Center;
                        break;
                    }
                    text = firstJuicer.outputPile.Count == 0 && firstJuicer.Working ? "Your juice is being made..." : "Grab the fresh juice!";
                    target = firstJuicer.outputZone.transform.position;
                    if (_player.Contains(IsJuice)) Advance();
                    break;
                case 3:
                    text = "Put the juice on the counter";
                    target = counter.dropZone.transform.position;
                    if (counter.display.Count > 0) Advance();
                    break;
                case 4:
                    text = "Customers pay here - collect the cash!";
                    target = cash.zone.transform.position;
                    // Money you already had (a new world starts with some) does not count: collect a payment first.
                    if (gm.data.stats.earned > _stepEarned) Advance();
                    break;
                case 5:
                    text = "Spend cash to unlock new stuff!";
                    var z = UnlockManager.FirstAvailable();
                    if (z != null) target = z.transform.position;
                    break;
                default:
                    FreePlay(gm, ref text, ref target, ref key);
                    break;
            }

            // First delivery: walk the player through loading a truck.
            var dm = DeliveryManager.Focus();
            if (step >= 5 && dm != null && dm.Loading && gm.data.stats.deliveries == 0 && dm.bay != null && dm.bay.loadZone != null)
            {
                var want = dm.Order.Item;
                string goods = dm.Order.Name;
                bool box = dm.bay.box != null;
                if (_player.Contains(t => t == want))
                {
                    text = "Load the " + goods + (box ? " into the box!" : " into the truck!");
                    target = dm.bay.loadZone.transform.position;
                }
                else
                {
                    text = "A truck wants " + dm.Order.qty + " " + goods + " - grab some!";
                    var j = FindProducer(want);
                    target = j != null ? j.OutputZone.transform.position : dm.bay.loadZone.transform.position;
                }
            }

            if (step >= 6) CakeGuide(gm, ref text, ref target);

            // Carrying slices nobody can juice yet: point at the trash bin so the player is never stuck.
            var bin = TrashBin.I;
            if (bin != null && bin.isActiveAndEnabled)
            {
                for (int i = 0; i < _player.items.Count; i++)
                {
                    var t = _player.items[i].type;
                    if (!t.IsSlice() || gm.HasJuicer(t.Fruit()) || gm.HasCakeMixer(t.Fruit())) continue;
                    text = "No " + Balance.FruitNames[(int)t.Fruit()] + " " + MachineWord + " yet! Toss them in the trash";
                    target = bin.zone != null ? bin.zone.transform.position : bin.transform.position;
                    break;
                }
            }

            _target = target;
            if (CameraFollow.Busy) target = null;
            if (HUD.I != null) HUD.I.SetObjective(text, key);
            UpdateMarkers(target);
        }

        static readonly System.Predicate<ItemType> IsJuice = t => t.IsJuice();
        static readonly System.Predicate<ItemType> IsSlice = t => t.IsSlice();

        /// <summary>After the tutorial: always show the next goal on the way to completing this world.</summary>
        void FreePlay(GameManager gm, ref string text, ref Vector3? target, ref string key)
        {
            // Standing in the shop: the panel does the talking.
            if (UpgradePanel.I != null && UpgradePanel.I.IsOpen) return;
            long money = gm.Money;
            var next = UnlockManager.FirstAvailable();
            var up = CheapestUpgrade(out int upDone, out int upTotal);
            bool shopOpen = upgradeZone != null && upgradeZone.isActiveAndEnabled;
            bool canUpgrade = shopOpen && up != null && money >= up.Cost();

            // A fox wrecked a farm: point at its restore pad (an affordable unlock still comes first).
            var raid = FoxRaid.I;
            var hurt = raid != null && !raid.Raiding ? raid.FirstDamaged() : null;
            if (hurt != null && (next == null || money < next.Remaining))
            {
                var farm = raid.FarmOf(hurt);
                text = "Fox damage! Restore the " + (string.IsNullOrEmpty(hurt.displayName) ? "farm" : hurt.displayName) + " (" + FoxRaid.Clock(raid.TimeLeft(hurt)) + ")";
                key = "fox:" + hurt.kind;
                if (farm != null && farm.restorePad != null) target = farm.restorePad.transform.position;
                return;
            }

            if (next != null)
            {
                long need = next.Remaining;
                if (money >= need)
                {
                    text = "Unlock the " + next.title + "!";
                    key = "unlock:" + next.id;
                    target = next.transform.position;
                }
                else if (canUpgrade)
                {
                    text = "Upgrade ready: " + up.name + "!";
                    key = "upgrade:" + up.id;
                    target = upgradeZone.transform.position;
                }
                else
                {
                    text = "Next: " + next.title + " - earn $" + Economy.Money(need - money) + " more";
                    key = "next:" + next.id;
                }
                return;
            }

            // Every pad is open: the last stretch is maxing the upgrades.
            if (up == null) return;
            var em = ExpansionManager.I;
            string goal = em != null && em.nextExpansion == 1 ? "to open the Tropical Farm"
                : em != null && em.nextExpansion == 2 ? "to open the Berry Blast"
                : em != null && em.nextExpansion > 2 ? "to open the next world"
                : Economy.World >= 2 ? "to become the Berry Cake Empire" : "to master the island";
            text = "Max every upgrade " + goal + "! " + upDone + "/" + upTotal;
            key = "max";
            if (canUpgrade) target = upgradeZone.transform.position;
        }

        /// <summary>Cheapest upgrade that is not maxed yet, plus levels bought / levels in total.</summary>
        static UpgradeDef CheapestUpgrade(out int done, out int total)
        {
            done = 0;
            total = 0;
            UpgradeDef best = null;
            int bestCost = int.MaxValue;
            foreach (var d in Upgrades.ForWorld(Economy.World))
            {
                done += Mathf.Min(d.Level(), d.maxLevel);
                total += d.maxLevel;
                if (d.Maxed) continue;
                int c = d.Cost();
                if (c >= 0 && c < bestCost)
                {
                    bestCost = c;
                    best = d;
                }
            }
            return best;
        }

        IProducer _cachedProducer;

        /// <summary>The open machine that makes <paramref name="item"/> (juicer, press or oven).</summary>
        IProducer FindProducer(ItemType item)
        {
            if (_cachedProducer != null && _cachedProducer.OutputType == item && _cachedProducer.IsActive) return _cachedProducer;
            foreach (var j in FindObjectsByType<Juicer>(FindObjectsSortMode.None))
                if (j.OutputType == item && j.isActiveAndEnabled) return _cachedProducer = j;
            foreach (var o in FindObjectsByType<Oven>(FindObjectsSortMode.None))
                if (o.OutputType == item && o.IsActive) return _cachedProducer = o;
            return null;
        }

        /// <summary>Until the first cake is sold: berries into the cake mixer, the oven bakes, cake into the pastry case.</summary>
        void CakeGuide(GameManager gm, ref string text, ref Vector3? target)
        {
            if (firstCakeMixer == null || firstOven == null || cakeCounter == null) return;
            if (!firstCakeMixer.isActiveAndEnabled || !firstOven.isActiveAndEnabled || gm.data.cakesSold > 0) return;
            // A fox-raided berry patch cannot feed the mixer: the restore hint matters more than the cake lesson.
            if (FoxRaid.I != null && FoxRaid.I.DamagedCount > 0 && !gm.HasField(firstCakeMixer.kind)) return;
            if (UnlockManager.FirstAvailable() is UnlockZone z && gm.Money >= z.Remaining) return;
            var cake = firstOven.OutputType;
            var berry = firstCakeMixer.InputType;
            string berries = Plural[(int)firstCakeMixer.kind];
            if (_player.Contains(t => t == cake))
            {
                text = "Put the cake in the pastry case!";
                target = cakeCounter.dropZone.transform.position;
            }
            else if (firstOven.Available > 0)
            {
                text = "Your first cake is ready - grab it!";
                target = firstOven.outputZone.transform.position;
            }
            else if (firstOven.Working || firstCakeMixer.Working || firstCakeMixer.outputPile.Count > 0)
            {
                text = "The oven is baking your cake...";
                target = firstOven.outputZone.transform.position;
            }
            else if (_player.Contains(t => t == berry))
            {
                text = "Drop the " + berries + " into the cake mixer";
                target = firstCakeMixer.inputZone.transform.position;
            }
            else
            {
                text = "Bake a cake! Bring " + berries + " to the cake mixer";
                target = firstCakeMixer.inputZone.transform.position;
            }
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
                    // Spin, leaning back towards the camera so the arrow keeps its silhouette where the camera looks
                    // steeply down on it (targets near the bottom of the screen read as a blob otherwise).
                    _arrowSpin += 90f * Time.deltaTime;
                    Vector3 down = Vector3.down;
                    var cam = GameRefs.I != null ? GameRefs.I.mainCamera : null;
                    if (cam != null)
                    {
                        Vector3 view = (p - cam.transform.position).normalized;
                        Vector3 side = Vector3.down - Vector3.Dot(Vector3.down, view) * view;
                        if (side.sqrMagnitude > 0.0001f) down = Vector3.Slerp(Vector3.down, side.normalized, 0.6f);
                    }
                    arrow.rotation = Quaternion.AngleAxis(_arrowSpin, down) * Quaternion.FromToRotation(Vector3.down, down);
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
