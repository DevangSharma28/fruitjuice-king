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

        void OnEnable()
        {
            if (DeliveryManager.I != null) DeliveryManager.I.OnBayOpened(this);
        }
    }
}
