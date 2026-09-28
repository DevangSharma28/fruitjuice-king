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
        public FruitField firstField;
        public Juicer firstJuicer;
        public Counter counter;
        public CashPile cash;

        Carrier _player;
        Vector3 _arrowBase;

        void Start()
        {
            _player = GameRefs.I.player;
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
            if (arrow != null)
            {
                if (arrow.gameObject.activeSelf != has) arrow.gameObject.SetActive(has);
                if (has)
                {
                    Vector3 p = target.Value;
                    p.y = Mathf.Max(p.y, 0f) + 2.4f + Mathf.Sin(Time.time * 5f) * 0.25f;
                    arrow.position = p;
                    arrow.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
                }
            }

            if (pointer != null)
            {
                bool show = false;
                if (has)
                {
                    Vector3 d = target.Value - _player.transform.position;
                    d.y = 0f;
                    show = d.magnitude > 3.5f;
                    if (show)
                    {
                        pointer.position = _player.transform.position + d.normalized * 1.4f + Vector3.up * 0.06f;
                        pointer.rotation = Quaternion.LookRotation(d.normalized);
                    }
                }
                if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
            }
        }
    }
}
