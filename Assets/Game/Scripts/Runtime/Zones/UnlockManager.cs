using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Restores unlock state on load and chains reveal of next pads.</summary>
    public class UnlockManager : MonoBehaviour
    {
        static UnlockManager _i;
        public UnlockZone[] zones;
        public float revealDelay = 0.6f;

        readonly Dictionary<UnlockZone, List<UnlockZone>> _prereq = new Dictionary<UnlockZone, List<UnlockZone>>();

        public static event System.Action<UnlockZone> ZoneUnlocked;

        void Awake() => _i = this;

        void Start()
        {
            var gm = GameManager.I;
            foreach (var z in zones)
                foreach (var n in z.next)
                {
                    if (n == null) continue;
                    if (!_prereq.TryGetValue(n, out var list)) _prereq[n] = list = new List<UnlockZone>();
                    list.Add(z);
                }

            foreach (var z in zones)
            {
                bool unlocked = gm.IsUnlocked(z.id);
                bool visible = z.startVisible;
                if (_prereq.TryGetValue(z, out var pre))
                    foreach (var p in pre)
                        if (gm.IsUnlocked(p.id)) visible = true;
                z.InitState(unlocked, visible);
            }
            NavBaker.Rebuild();
        }

        public static void OnZoneUnlocked(UnlockZone z)
        {
            ZoneUnlocked?.Invoke(z);
            if (_i == null) return;
            Tweener.Delay(0.3f, NavBaker.Rebuild);
            float d = _i.revealDelay;
            foreach (var n in z.next)
            {
                if (n == null) continue;
                var nn = n;
                Tweener.Delay(d, () => nn.Show(true));
                d += 0.25f;
            }
        }

        public static UnlockZone FirstAvailable()
        {
            if (_i == null) return null;
            UnlockZone best = null;
            foreach (var z in _i.zones)
            {
                if (z == null || z.IsUnlocked || !z.gameObject.activeInHierarchy) continue;
                if (best == null || z.price < best.price) best = z;
            }
            return best;
        }
    }
}
