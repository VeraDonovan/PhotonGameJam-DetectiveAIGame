using DetectiveGame.Core;
using UnityEngine;

namespace DetectiveGame.UI
{
    public sealed class UnderPanelController : MonoBehaviour
    {
        public void OnOpenInventory()
        {
            var appRoot = AppRoot.Instance;
            if (appRoot != null && appRoot.UIManager != null)
            {
                appRoot.UIManager.SetInventoryOpen(true);
            }
        }
    }
}
