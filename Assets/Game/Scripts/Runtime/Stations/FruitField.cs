using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// A plot of fruit nodes of one kind. Used by farmer helpers to find work. In the Berry Blast a fox can raid it: while
    /// <see cref="Damaged"/> nothing grows and customers stop ordering its berry.
    /// </summary>
    public class FruitField : MonoBehaviour
    {
        public FruitKind kind;
        public Transform idlePoint;
        [Tooltip("Name shown in raid messages (\"Strawberry Patch\").")]
        public string displayName;

        public bool Damaged { get; private set; }

        void OnEnable()
        {
            if (GameManager.I != null && !Damaged) GameManager.I.RegisterField(kind);
        }

        void OnDisable()
        {
            if (GameManager.I != null) GameManager.I.UnregisterField(kind);
        }

        /// <summary>Every node of this field (including the expansion plots).</summary>
        public void Nodes(List<FruitNode> into)
        {
            into.Clear();
            var all = FruitNode.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].field == this) into.Add(all[i]);
        }

        /// <summary>Raided: bushes go bare and wilt, production of this berry stops.</summary>
        public void SetDamaged(bool damaged, float regrowStagger = 0.25f)
        {
            if (Damaged == damaged) return;
            Damaged = damaged;
            var nodes = new List<FruitNode>();
            Nodes(nodes);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (damaged) nodes[i].Damage();
                else nodes[i].Restore(0.2f + i * regrowStagger);
            }
            if (GameManager.I == null) return;
            if (damaged) GameManager.I.UnregisterField(kind);
            else if (isActiveAndEnabled) GameManager.I.RegisterField(kind);
        }

        public FruitNode NearestReady(Vector3 from)
        {
            FruitNode best = null;
            float bd = float.MaxValue;
            var all = FruitNode.All;
            for (int i = 0; i < all.Count; i++)
            {
                var n = all[i];
                if (n.field != this || !n.IsReady) continue;
                float d = (n.transform.position - from).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = n;
                }
            }
            return best;
        }

        public Vector3 Center => idlePoint != null ? idlePoint.position : transform.position;
    }
}
