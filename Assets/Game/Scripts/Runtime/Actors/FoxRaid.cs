using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Berry Blast event: now and then a fox sneaks out of the woods, raids a berry farm and runs off with the berries.
    /// The farm stops producing and its bushes wilt until they regrow (~5 minutes) or the player restores them with Golden
    /// Apples or a rewarded video. The very first raid plays as a short cutscene that teaches all of this.
    /// Damaged farms and the time to the next raid are saved, and regrowth keeps ticking while the game is closed.
    /// </summary>
    public class FoxRaid : MonoBehaviour
    {
        public static FoxRaid I { get; private set; }

        [System.Serializable]
        public class Farm
        {
            public FruitField field;
            [Tooltip("Where the fox slips into the farm.")]
            public Transform entry;
            [Tooltip("Pad shown in front of a raided farm: stand on it to restore the farm.")]
            public FoxRestoreZone restorePad;
        }

        public FoxActor fox;
        [Tooltip("Den in the woods the fox comes from and escapes to.")]
        public Transform den;
        public Farm[] farms;
        public ExpansionIntro cutscene;
        [Tooltip("Seconds of play after the tutorial before the first (scripted) raid.")]
        public float firstDelay = 50f;

        public const string AdPlacement = "fox_restore";

        readonly Dictionary<FruitField, float> _timers = new Dictionary<FruitField, float>();
        bool _raiding;
        float _firstT;
        float _saveT;
        Coroutine _routine;
        Farm _current;

        public bool Raiding => _raiding;

        void Awake() => I = this;

        void Start()
        {
            if (fox != null) fox.gameObject.SetActive(false);
            foreach (var f in farms)
                if (f.restorePad != null) f.restorePad.gameObject.SetActive(false);

            // Farms still recovering from a raid (time away counts towards regrowth).
            var d = GameManager.I.data;
            float away = (float)System.Math.Min(GameManager.I.AwaySeconds, 3600.0);
            for (int i = 0; i < d.foxKinds.Count && i < d.foxTimers.Count; i++)
            {
                var farm = FarmOf((FruitKind)d.foxKinds[i]);
                if (farm == null) continue;
                float left = d.foxTimers[i] - away;
                if (left > 1f) MarkDamaged(farm, left, false);
            }
            SyncSave();
            if (d.foxNext < 0f && d.foxIntroSeen) d.foxNext = Economy.FoxInterval;
        }

        Farm FarmOf(FruitKind k)
        {
            foreach (var f in farms)
                if (f.field != null && f.field.kind == k) return f;
            return null;
        }

        public Farm FarmOf(FruitField field)
        {
            foreach (var f in farms)
                if (f.field == field) return f;
            return null;
        }

        /// <summary>Seconds until a damaged farm regrows on its own (0 if healthy).</summary>
        public float TimeLeft(FruitField field) => field != null && _timers.TryGetValue(field, out var t) ? Mathf.Max(0f, t) : 0f;

        public FruitField FirstDamaged()
        {
            foreach (var kv in _timers) return kv.Key;
            return null;
        }

        public int DamagedCount => _timers.Count;

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            float dt = Time.deltaTime;

            // Regrowth timers.
            if (_timers.Count > 0)
            {
                _keys.Clear();
                _keys.AddRange(_timers.Keys);
                foreach (var f in _keys)
                {
                    float t = _timers[f] - dt;
                    _timers[f] = t;
                    if (t <= 0f) Restore(f, false);
                }
            }

            _saveT += dt;
            if (_saveT > 1f)
            {
                _saveT = 0f;
                SyncSave();
            }

            if (_raiding || ExpansionIntro.Playing || CameraFollow.Busy || LoadingScreen.Busy) return;
            if (gm.TutorialStep < 6) return;

            var d = gm.data;
            if (!d.foxIntroSeen)
            {
                if (PopupOpen()) return;
                _firstT += dt;
                if (_firstT >= firstDelay)
                {
                    var farm = PickFarm();
                    if (farm != null) PlayFirstRaid(farm);
                }
                return;
            }

            // Regular raids: one farm at a time, never while one is still damaged.
            if (_timers.Count > 0) return;
            d.foxNext -= dt;
            if (d.foxNext <= 0f)
            {
                var farm = PickFarm();
                d.foxNext = farm != null ? Economy.FoxInterval : 30f;
                if (farm != null) StartRaid(farm, false);
            }
        }

        static readonly List<FruitField> _keys = new List<FruitField>();

        static bool PopupOpen() =>
            (OfferPopup.I != null && OfferPopup.I.IsOpen) || (PremiumPopup.I != null && PremiumPopup.I.IsOpen) ||
            (UpgradePanel.I != null && UpgradePanel.I.IsOpen) || (DeliveryPopup.I != null && DeliveryPopup.I.IsOpen) ||
            (ShopPopup.I != null && ShopPopup.I.IsOpen);

        /// <summary>A random open, healthy farm (the fox prefers the one the player is not standing in).</summary>
        Farm PickFarm()
        {
            var options = new List<Farm>();
            foreach (var f in farms)
                if (f.field != null && f.field.isActiveAndEnabled && !f.field.Damaged) options.Add(f);
            if (options.Count == 0) return null;
            return options[Random.Range(0, options.Count)];
        }

        // ---------------------------------------------------------------- raids

        void StartRaid(Farm farm, bool cinematic)
        {
            _raiding = true;
            _current = farm;
            GameManager.I.data.stats.foxRaids++;
            if (!cinematic && HUD.I != null)
                HUD.I.Toast("A fox is raiding the " + Name(farm) + "!", GameRefs.I != null ? GameRefs.I.foxIcon : null, 3f);
            _routine = StartCoroutine(RaidRoutine(farm, cinematic));
        }

        IEnumerator RaidRoutine(Farm farm, bool cinematic)
        {
            var field = farm.field;
            var nodes = new List<FruitNode>();
            field.Nodes(nodes);
            // The fox goes for up to three bushes nearest its way in.
            Vector3 entry = farm.entry != null ? farm.entry.position : field.transform.position;
            nodes.Sort((a, b) => (a.transform.position - entry).sqrMagnitude.CompareTo((b.transform.position - entry).sqrMagnitude));
            if (nodes.Count > 3) nodes.RemoveRange(3, nodes.Count - 3);

            Vector3 home = den != null ? den.position : entry + Vector3.forward * 6f;
            fox.Appear(home, entry - home);
            yield return new WaitForSeconds(0.35f);
            if (cinematic && cutscene != null) cutscene.Caption("Uh oh... a sneaky fox!");
            yield return fox.RunTo(entry, 5.5f);
            yield return fox.Sniff(0.7f);
            if (cinematic && cutscene != null) cutscene.Caption("It's stealing your berries!");

            foreach (var n in nodes)
            {
                if (n == null) continue;
                Vector3 p = n.transform.position;
                Vector3 side = (entry - p);
                side.y = 0f;
                side = side.sqrMagnitude > 0.01f ? side.normalized : Vector3.back;
                yield return fox.RunTo(p + side * 0.95f, 6f);
                var node = n;
                yield return fox.Pounce(p, () =>
                {
                    var col = Balance.FruitColors[(int)field.kind];
                    Fx.Chips(node.transform.position + Vector3.up * 0.6f, col, 10);
                    Fx.Leaves(node.transform.position + Vector3.up * 0.8f, 5);
                    Fx.Drops(node.transform.position + Vector3.up * 0.5f, Balance.JuiceColors[(int)field.kind], 4);
                    node.Damage();
                    if (NearPlayer(node.transform.position) || cinematic) CameraFollow.Shake(0.05f, 0.12f);
                });
                fox.SetLoot(true);
            }

            MarkDamaged(farm, Economy.FoxRegrowTime, true);
            if (cinematic && cutscene != null) cutscene.Caption("Your " + Name(farm) + " is damaged!");
            Sfx.Play(SfxId.Yip, 0.5f, 1.2f);
            if (cinematic)
            {
                // Hold on the wrecked patch for a beat, then hand back while the fox scampers off.
                StartCoroutine(fox.RunTo(home, 7.5f));
                yield return new WaitForSeconds(1.6f);
                FinishCutscene(farm);
                while (fox.gameObject.activeSelf && (fox.transform.position - home).sqrMagnitude > 0.2f) yield return null;
            }
            else yield return fox.RunTo(home, 7.5f);
            fox.Vanish();
            yield return new WaitForSeconds(0.5f);
            _raiding = false;
            _routine = null;
            if (!cinematic && HUD.I != null)
                HUD.I.Toast("The fox ruined the " + Name(farm) + "! Restore it at the farm", GameRefs.I != null ? GameRefs.I.foxIcon : null, 3.2f);
        }

        bool NearPlayer(Vector3 p)
        {
            var pl = GameRefs.I != null ? GameRefs.I.player : null;
            return pl != null && (pl.transform.position - p).sqrMagnitude < 150f;
        }

        void PlayFirstRaid(Farm farm)
        {
            if (cutscene == null)
            {
                GameManager.I.data.foxIntroSeen = true;
                StartRaid(farm, false);
                return;
            }
            cutscene.BeginCutscene(() => SkipCutscene(farm));
            Vector3 look = farm.field.transform.position;
            if (den != null) look = Vector3.Lerp(look, den.position, 0.3f);
            // Hold on the farm while the fox does its thing (the routine ends the shot).
            CameraFollow.Peek(look, 30f, 1.05f, () => StartRaid(farm, true), null, 1f, 0.8f);
        }

        void SkipCutscene(Farm farm)
        {
            StopAllCoroutines();
            _routine = null;
            _raiding = false;
            if (fox != null) fox.gameObject.SetActive(false);
            if (!farm.field.Damaged) MarkDamaged(farm, Economy.FoxRegrowTime, true);
            FinishCutscene(farm);
        }

        void FinishCutscene(Farm farm)
        {
            var d = GameManager.I.data;
            d.foxIntroSeen = true;
            d.foxNext = Economy.FoxInterval;
            GameManager.I.Save();
            if (cutscene != null) cutscene.EndCutscene();
            CameraFollow.CancelPeeks();
            StartCoroutine(OfferWhenClear(farm));
        }

        /// <summary>Once the bars are gone and the camera is back on the player: explain the two ways to fix it.</summary>
        IEnumerator OfferWhenClear(Farm farm)
        {
            yield return new WaitForSeconds(0.3f);
            while (ExpansionIntro.Playing || CameraFollow.Busy) yield return null;
            yield return new WaitForSeconds(0.25f);
            OfferRestore(farm.field, true);
        }

        // ---------------------------------------------------------------- damage & restore

        void MarkDamaged(Farm farm, float seconds, bool fresh)
        {
            var field = farm.field;
            field.SetDamaged(true);
            _timers[field] = seconds;
            if (farm.restorePad != null)
            {
                farm.restorePad.field = field;
                farm.restorePad.gameObject.SetActive(true);
                if (fresh) Tweener.Scale(farm.restorePad.transform, Vector3.zero, Vector3.one, 0.5f, Ease.OutBack, null, 0.3f);
            }
            SyncSave();
        }

        /// <summary>Open the recovery offer for a raided farm (Golden Apples or a rewarded video).</summary>
        public void OfferRestore(FruitField field, bool firstTime = false)
        {
            if (field == null || !field.Damaged || PremiumPopup.I == null) return;
            var farm = FarmOf(field);
            string body = firstTime
                ? "Foxes sneak in from the woods and wreck your bushes. It regrows by itself - or fix it right now!"
                : "It regrows by itself - or fix it right now!";
            // The fox already peeks over the popup's header, so the card shows the berry that was raided.
            var refs = GameRefs.I;
            Sprite icon = refs == null ? null
                : refs.fruitIcons != null && (int)field.kind < refs.fruitIcons.Length && refs.fruitIcons[(int)field.kind] != null ? refs.fruitIcons[(int)field.kind]
                : refs.foxIcon;
            PremiumPopup.I.Show("FOX ATTACK!", Name(farm) + " is damaged. " + body, icon,
                Economy.ApplesRestoreFarm, () => "Regrows in " + Clock(TimeLeft(field)),
                () => Restore(field, true), AdPlacement, () => Restore(field, true));
        }

        public void Restore(FruitField field, bool instant)
        {
            if (field == null || !_timers.ContainsKey(field)) return;
            _timers.Remove(field);
            field.SetDamaged(false, instant ? 0.12f : 0.3f);
            var farm = FarmOf(field);
            if (farm != null && farm.restorePad != null)
                Tweener.Scale(farm.restorePad.transform, farm.restorePad.transform.localScale, Vector3.zero, 0.3f, Ease.InQuad,
                    () => { if (farm.restorePad != null) farm.restorePad.gameObject.SetActive(false); });
            Vector3 c = field.Center + Vector3.up;
            Fx.Confetti(c, instant ? 50 : 20);
            Fx.Stars(c, 12, new Color(0.6f, 1f, 0.5f));
            Fx.Ring(new Vector3(c.x, 0.05f, c.z), new Color(0.6f, 1f, 0.5f, 0.9f), 7f);
            Sfx.Play(SfxId.Unlock, 0.6f, 1.15f);
            FloatingText.Show(instant ? "RESTORED!" : "REGROWN!", c + Vector3.up * 1.2f, new Color(0.6f, 1f, 0.5f), 1.3f, 1.3f, 1.3f);
            if (!instant && HUD.I != null) HUD.I.Toast("The " + Name(farm) + " grew back!", GameRefs.I != null ? GameRefs.I.FruitIcon(field.kind) : null);
            SyncSave();
            GameManager.I.Save();
        }

        void SyncSave()
        {
            var d = GameManager.I.data;
            d.foxKinds.Clear();
            d.foxTimers.Clear();
            foreach (var kv in _timers)
            {
                d.foxKinds.Add((int)kv.Key.kind);
                d.foxTimers.Add(kv.Value);
            }
        }

        static string Name(Farm farm) =>
            farm != null && farm.field != null && !string.IsNullOrEmpty(farm.field.displayName) ? farm.field.displayName
            : farm != null && farm.field != null ? Balance.FruitNames[(int)farm.field.kind] + " farm" : "farm";

        public static string Clock(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
