using UnityEngine;

namespace JuiceKing
{
    /// <summary>Keeps world-space UI facing the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        Transform _cam;

        void LateUpdate()
        {
            if (_cam == null)
            {
                var c = GameRefs.I != null && GameRefs.I.mainCamera != null ? GameRefs.I.mainCamera : Camera.main;
                if (c == null) return;
                _cam = c.transform;
            }
            transform.rotation = _cam.rotation;
        }
    }
}
