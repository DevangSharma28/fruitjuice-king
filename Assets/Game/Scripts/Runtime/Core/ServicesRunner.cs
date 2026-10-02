using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Hidden object that lives for the whole app session and drives <see cref="AppServices.Tick"/> (consent timeout and
    /// retry, ad loading). Created by <see cref="AppServices"/> before the first scene loads; survives scene switches.
    /// </summary>
    public class ServicesRunner : MonoBehaviour
    {
        static ServicesRunner _instance;

        internal static void Install()
        {
            if (_instance != null) return;
            var go = new GameObject("[Services]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ServicesRunner>();
        }

        void Update() => AppServices.Tick();
    }
}
