using DetectiveGame.UI;
using System;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private KeyCode inventoryToggleKey = KeyCode.Tab;
        [SerializeField] private GameObject inventoryRoot;
        [SerializeField] private InventoryPanelManager inventoryPanelManager;
        [SerializeField] private bool inventoryOpenOnStart;

        public bool IsInventoryOpen => inventoryRoot.activeSelf;

        public void Initialize()
        {
            ValidateConfiguration();

            inventoryRoot.SetActive(inventoryOpenOnStart);

            if (inventoryOpenOnStart)
            {
                inventoryPanelManager.RefreshActiveTab();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(inventoryToggleKey))
            {
                SetInventoryOpen(!IsInventoryOpen);
            }
        }

        public void SetInventoryOpen(bool isOpen)
        {
            inventoryRoot.SetActive(isOpen);

            if (isOpen)
            {
                inventoryPanelManager.RefreshActiveTab();
            }
        }

        private void ValidateConfiguration()
        {
            if (inventoryRoot == null)
            {
                throw new InvalidOperationException("UIManager requires inventoryRoot to be assigned.");
            }

            if (inventoryPanelManager == null)
            {
                throw new InvalidOperationException("UIManager requires inventoryPanelManager to be assigned.");
            }
        }
    }
}
