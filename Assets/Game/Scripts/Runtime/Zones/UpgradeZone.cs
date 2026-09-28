using UnityEngine;

namespace JuiceKing
{
    /// <summary>Opens the upgrade shop while the player stands on it.</summary>
    public class UpgradeZone : Zone
    {
        protected override void Awake()
        {
            base.Awake();
            playerOnly = true;
        }

        protected override float TickCarrier(Carrier c, float timer) => timer;

        protected override void OnPlayerEnter()
        {
            if (UpgradePanel.I != null) UpgradePanel.I.Show();
        }

        protected override void OnPlayerExit()
        {
            if (UpgradePanel.I != null) UpgradePanel.I.Hide();
        }

        void OnDisable()
        {
            if (PlayerInside && UpgradePanel.I != null) UpgradePanel.I.Hide();
        }
    }
}
