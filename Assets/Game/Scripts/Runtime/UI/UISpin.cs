using UnityEngine;

namespace JuiceKing
{
    /// <summary>Constant spin on unscaled time (sunburst rays behind reward icons).</summary>
    public class UISpin : MonoBehaviour
    {
        public float speed = 20f;

        void Update() => transform.Rotate(0f, 0f, -speed * Time.unscaledDeltaTime);
    }
}
