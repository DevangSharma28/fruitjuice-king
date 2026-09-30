using UnityEngine;

namespace JuiceKing
{
    /// <summary>Keeps an ambient effect (floating pollen, drifting leaves) centred on the player on the ground plane.</summary>
    public class FollowTarget : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;

        void LateUpdate()
        {
            if (target == null)
            {
                if (GameRefs.I != null && GameRefs.I.player != null) target = GameRefs.I.player.transform;
                else return;
            }
            var p = target.position + offset;
            p.y = offset.y;
            transform.position = p;
        }
    }
}
