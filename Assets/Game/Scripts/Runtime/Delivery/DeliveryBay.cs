using UnityEngine;

namespace JuiceKing
{
    /// <summary>Scene layout for deliveries: the road in, the parking spot, the road out, the loading pad and the trucks.</summary>
    public class DeliveryBay : MonoBehaviour
    {
        public Transform[] arrivePath;
        public Transform park;
        public Transform[] departPath;
        public DeliveryZone loadZone;
        [Tooltip("Order sign standing beside the truck stop.")]
        public TruckBoard board;
        [Tooltip("One reusable truck per TruckKind (index = TruckKind).")]
        public DeliveryTruck[] trucks;
        [Tooltip("Optional: goods go into this box, which is loaded aboard when full (Berry Blast).")]
        public DeliveryBox box;
        [Tooltip("Desk that runs this bay (empty = the scene's first desk).")]
        public DeliveryManager manager;

        void OnEnable()
        {
            var m = manager != null ? manager : DeliveryManager.I;
            if (m != null) m.OnBayOpened(this);
        }
    }
}
