using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using DetectiveGame.Core;

namespace DetectiveGame.UI
{
    public sealed class InventoryPanelManager : MonoBehaviour
    {
        public enum InventoryTab
        {
            Case = 0,
            Evidence = 1,
            Suspect = 2,
            Arrest = 3   // 👉 新增：Arrest 选项
        }

        [SerializeField] private InventoryTab defaultTab = InventoryTab.Case;
        [SerializeField] private Button caseButton;
        [SerializeField] private Button evidenceButton;
        [SerializeField] private Button suspectButton;
        [SerializeField] private Button arrestButton;   // 👉 新增：Arrest 按钮

        [SerializeField] private GameObject casePanel;
        [SerializeField] private GameObject evidencePanel;
        [SerializeField] private GameObject suspectPanel;
        [SerializeField] private GameObject arrestPanel;   // 👉 新增：Arrest 面板

        private readonly Dictionary<GameObject, bool> preEvidenceSelectionStates = new Dictionary<GameObject, bool>();
        private bool isEvidenceSelectionViewActive;

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

        public void ShowArrestTab()   // 👉 新增：打开 Arrest 面板的方法
        {
            SetActiveTab(InventoryTab.Arrest);
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
            SetPanelState(arrestPanel, ActiveTab == InventoryTab.Arrest);   // 👉 新增：刷新 Arrest 面板
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
            SetPanelState(arrestPanel, false);   // 👉 新增：关闭 Arrest 面板
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
            RegisterButton(arrestButton, ShowArrestTab);   // 👉 新增：绑定 Arrest 按钮
        }

        private void UnbindButtonEvents()
        {
            UnregisterButton(caseButton, ShowCaseTab);
            UnregisterButton(evidenceButton, ShowEvidenceTab);
            UnregisterButton(suspectButton, ShowSuspectTab);
            UnregisterButton(arrestButton, ShowArrestTab);   // 👉 新增：解绑 Arrest 按钮
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

        public void OnCloseCasePanel()
        {
            SetPanelState(casePanel, false);
        }

        public void OnCloseEvidencePanel()
        {
            SetPanelState(evidencePanel, false);
        }

        public void OnCloseSuspectPanel()
        {
            SetPanelState(suspectPanel, false);
        }

        public void OnCloseArrestPanel()   // 👉 新增：关闭 Arrest 面板的方法
        {
            SetPanelState(arrestPanel, false);
        }

        public void OnCloseInventory()
        {
            var appRoot = AppRoot.Instance;
            if (appRoot != null && appRoot.UIManager != null)
            {
                appRoot.UIManager.SetInventoryOpen(false);
            }
        }
    }
}
