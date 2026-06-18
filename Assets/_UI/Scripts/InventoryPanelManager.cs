using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

namespace DetectiveGame.UI
{
    public sealed class InventoryPanelManager : MonoBehaviour
    {
        public enum InventoryTab
        {
            Case = 0,
            Evidence = 1,
            Suspect = 2,
        }

        [SerializeField] private InventoryTab defaultTab = InventoryTab.Case;
        [SerializeField] private Button caseButton;
        [SerializeField] private Button evidenceButton;
        [SerializeField] private Button suspectButton;
        [SerializeField] private GameObject casePanel;
        [SerializeField] private GameObject evidencePanel;
        [SerializeField] private GameObject suspectPanel;
        [SerializeField] private GameObject underPanel;

        private readonly Dictionary<GameObject, bool> preEvidenceSelectionStates = new Dictionary<GameObject, bool>();
        private bool isEvidenceSelectionViewActive;

        public InventoryTab ActiveTab { get; private set; }

        private void Awake()
        {
            ResolveOptionalPanelReferences();
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

        public void SetActiveTab(InventoryTab tab)
        {
            RestoreEvidenceSelectionView();
            ActiveTab = tab;
            RefreshActiveTab();
        }

        public void RefreshActiveTab()
        {
            RestoreEvidenceSelectionView();
            SetPanelState(casePanel, ActiveTab == InventoryTab.Case);
            SetPanelState(evidencePanel, ActiveTab == InventoryTab.Evidence);
            SetPanelState(suspectPanel, ActiveTab == InventoryTab.Suspect);
            SetPanelState(underPanel, true);
        }

        public void ShowEvidenceSelectionOnly()
        {
            ActiveTab = InventoryTab.Evidence;
            CaptureEvidenceSelectionViewState();

            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                SetPanelState(child, child == evidencePanel);
            }

            SetPanelState(casePanel, false);
            SetPanelState(evidencePanel, true);
            SetPanelState(suspectPanel, false);
            SetPanelState(underPanel, false);
        }

        public void RestoreEvidenceSelectionView()
        {
            if (!isEvidenceSelectionViewActive)
            {
                return;
            }

            foreach (var panelState in preEvidenceSelectionStates)
            {
                SetPanelState(panelState.Key, panelState.Value);
            }

            preEvidenceSelectionStates.Clear();
            isEvidenceSelectionViewActive = false;
        }

        private void CaptureEvidenceSelectionViewState()
        {
            if (isEvidenceSelectionViewActive)
            {
                return;
            }

            preEvidenceSelectionStates.Clear();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                preEvidenceSelectionStates[child] = child.activeSelf;
            }

            isEvidenceSelectionViewActive = true;
        }

        private void ResolveOptionalPanelReferences()
        {
            if (underPanel != null)
            {
                return;
            }

            var underPanelTransform = transform.Find("underPanel");
            if (underPanelTransform != null)
            {
                underPanel = underPanelTransform.gameObject;
            }
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
        }

        private void UnbindButtonEvents()
        {
            UnregisterButton(caseButton, ShowCaseTab);
            UnregisterButton(evidenceButton, ShowEvidenceTab);
            UnregisterButton(suspectButton, ShowSuspectTab);
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
