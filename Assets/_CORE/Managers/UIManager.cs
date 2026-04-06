using DetectiveGame.UI;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private KeyCode inventoryToggleKey = KeyCode.Tab;
        [SerializeField] private GameObject inventoryRoot;
        [SerializeField] private InventoryPanelManager inventoryPanelManager;
        [SerializeField] private bool inventoryOpenOnStart;

        public bool IsInventoryOpen => inventoryRoot != null && inventoryRoot.activeSelf;

        public void Initialize()
        {
            if (inventoryRoot == null || inventoryPanelManager == null)
            {
                return;
            }

            inventoryRoot.SetActive(inventoryOpenOnStart);

            if (inventoryOpenOnStart)
            {
                inventoryPanelManager?.RefreshActiveTab();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(inventoryToggleKey) && inventoryRoot != null)
            {
                SetInventoryOpen(!IsInventoryOpen);
            }
        }

        public void SetInventoryOpen(bool isOpen)
        {
            if (inventoryRoot == null || inventoryPanelManager == null)
            {
                return;
            }

            inventoryRoot.SetActive(isOpen);

            if (isOpen)
            {
                inventoryPanelManager?.RefreshActiveTab();
            }
        }
    }
}
