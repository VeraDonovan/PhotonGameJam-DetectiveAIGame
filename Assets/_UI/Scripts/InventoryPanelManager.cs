using UnityEngine.UI;
using UnityEngine;

namespace DetectiveGame.UI
{
    public sealed class InventoryPanelManager : MonoBehaviour
    {
        public enum InventoryTab
        {
            Case = 0,
            Evidence = 1,
            Suspect = 2,
            Investigation = 3
        }

        [SerializeField] private InventoryTab defaultTab = InventoryTab.Case;
        [SerializeField] private Button caseButton;
        [SerializeField] private Button evidenceButton;
        [SerializeField] private Button suspectButton;
        [SerializeField] private Button investigationButton;
        [SerializeField] private GameObject casePanel;
        [SerializeField] private GameObject evidencePanel;
        [SerializeField] private GameObject suspectPanel;
        [SerializeField] private GameObject investigationPanel;

        public InventoryTab ActiveTab { get; private set; }

        private void Awake()
        {
            BindButtonEvents();
            ActiveTab = defaultTab;
            RefreshActiveTab();
        }

        private void OnDestroy()
        {
            UnbindButtonEvents();
        }

        public void ShowCaseTab()
        {
            SetActiveTab(InventoryTab.Case);
        }

        public void ShowEvidenceTab()
        {
            SetActiveTab(InventoryTab.Evidence);
        }

        public void ShowSuspectTab()
        {
            SetActiveTab(InventoryTab.Suspect);
        }

        public void ShowInvestigationTab()
        {
            SetActiveTab(InventoryTab.Investigation);
        }

        public void SetActiveTab(InventoryTab tab)
        {
            ActiveTab = tab;
            RefreshActiveTab();
        }

        public void RefreshActiveTab()
        {
            SetPanelState(casePanel, ActiveTab == InventoryTab.Case);
            SetPanelState(evidencePanel, ActiveTab == InventoryTab.Evidence);
            SetPanelState(suspectPanel, ActiveTab == InventoryTab.Suspect);
            SetPanelState(investigationPanel, ActiveTab == InventoryTab.Investigation);
        }

        private static void SetPanelState(GameObject targetPanel, bool isActive)
        {
            if (targetPanel == null)
            {
                return;
            }

            targetPanel.SetActive(isActive);
        }

        private void BindButtonEvents()
        {
            RegisterButton(caseButton, ShowCaseTab);
            RegisterButton(evidenceButton, ShowEvidenceTab);
            RegisterButton(suspectButton, ShowSuspectTab);
            RegisterButton(investigationButton, ShowInvestigationTab);
        }

        private void UnbindButtonEvents()
        {
            UnregisterButton(caseButton, ShowCaseTab);
            UnregisterButton(evidenceButton, ShowEvidenceTab);
            UnregisterButton(suspectButton, ShowSuspectTab);
            UnregisterButton(investigationButton, ShowInvestigationTab);
        }

        private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void UnregisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
        }
    }
}
