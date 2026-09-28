using Unity.AI.Navigation;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Bakes the NavMesh at runtime (and after unlocks change the layout) for helper workers.</summary>
    public class NavBaker : MonoBehaviour
    {
        static NavBaker _i;
        public NavMeshSurface surface;

        void Awake() => _i = this;

        public static void Rebuild()
        {
            if (_i != null && _i.surface != null) _i.surface.BuildNavMesh();
        }
    }
}
