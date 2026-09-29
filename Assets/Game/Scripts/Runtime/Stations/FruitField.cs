using UnityEngine;

namespace JuiceKing
{
    /// <summary>A plot of fruit nodes of one kind. Used by farmer helpers to find work.</summary>
    public class FruitField : MonoBehaviour
    {
        public FruitKind kind;
        public Transform idlePoint;

        void OnEnable()
        {
            if (GameManager.I != null) GameManager.I.RegisterField(kind);
        }

        void OnDisable()
        {
            if (GameManager.I != null) GameManager.I.UnregisterField(kind);
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
