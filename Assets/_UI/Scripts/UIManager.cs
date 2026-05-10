using System;
using System.Collections.Generic;
using DetectiveGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveGame.Core
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private KeyCode inventoryToggleKey = KeyCode.Tab;
        [SerializeField] private GameObject menuPanelRoot;
        [SerializeField] private GameObject inventoryRoot;
        [SerializeField] private InventoryPanelManager inventoryPanelManager;
        [SerializeField] private EvidencePanelManager evidencePanelManager;
        [SerializeField] private bool menuOpenOnStart = true;
        [SerializeField] private bool inventoryOpenOnStart;
        [SerializeField] private TransitionUI transitionUI;
        [SerializeField] private GameObject unlockPopupRoot;
        [SerializeField] private TMP_Text unlockPopupText;
        [SerializeField] private Button unlockPopupCloseButton;

        private EventManager eventManager;
        private EvidenceDatabase evidenceDatabase;
        private StatementDatabase statementDatabase;
        private GameStateManager gameStateManager;
        private ProgressManager progressManager;
        private readonly HashSet<string> externalInputBlockers = new HashSet<string>(StringComparer.Ordinal);
        private bool lastPublishedBlockState;

        public bool IsMenuOpen => menuPanelRoot != null && menuPanelRoot.activeSelf;
        public bool IsInventoryOpen => inventoryRoot.activeSelf;
        public bool IsPlayerInputBlocked => IsAnyUiBlockingPlayer();

        private void Awake()
        {
            BindPopupCloseButton();
            SetPanelActive(unlockPopupRoot, false);
            SetTransitionOpen(false);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        public void Initialize()
        {
            var appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                throw new InvalidOperationException("UIManager requires AppRoot.Instance during initialization.");
            }

            eventManager = appRoot.EventManager;
            evidenceDatabase = appRoot.DatabaseManager?.EvidenceDatabase;
            eventManager.Subscribe<EvidenceAddedEvent>(HandleEvidenceAdded);
            statementDatabase = appRoot.DatabaseManager?.StatementDatabase;
            gameStateManager = appRoot.GameStateManager;
            progressManager = appRoot.ProgressManager;

            ValidateConfiguration();
            SubscribeToEvents();

            SetMenuOpen(menuOpenOnStart);
            SetInventoryOpen(inventoryOpenOnStart);
            SetUnlockPopupOpen(false);
            SetTransitionOpen(false);
            PublishUiBlockState();
        }

        public void OpenEvidenceSelectionForDialogue(Action<string, string> onEvidenceSelected)
        {
            SetMenuOpen(false);
            SetUnlockPopupOpen(false);
            SetTransitionOpen(false);
            SetInventoryOpen(true);
            inventoryPanelManager.ShowEvidenceSelectionOnly();
            evidencePanelManager.BeginSelectionMode((evidenceId, displayName) =>
            {
                onEvidenceSelected?.Invoke(evidenceId, displayName);
                SetInventoryOpen(false);
            });
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
            if (!isOpen)
            {
                inventoryPanelManager.RestoreEvidenceSelectionView();
            }

            SetPanelActive(inventoryRoot, isOpen);

            if (isOpen)
            {
                inventoryPanelManager.RefreshActiveTab();
            }

            PublishUiBlockState();
        }

        public void SetMenuOpen(bool isOpen)
        {
            SetPanelActive(menuPanelRoot, isOpen);
            PublishUiBlockState();
        }

        public void CloseUnlockPopup()
        {
            SetUnlockPopupOpen(false);
        }

        public void OnStartGameButton()
        {
            SetMenuOpen(false);
            SetTransitionOpen(true);
            transitionUI.StartTransition(HandleTransitionFinished);
            StartGame();
        }

        public bool StartGame()
        {
            if (!gameStateManager.TryStartGame())
            {
                return false;
            }

            SetMenuOpen(false);
            return true;
        }

        public bool EnterInterrogationPhase()
        {
            return gameStateManager.TryBeginInterrogation();
        }

        public bool ConfirmSuspectForFinalPhase(string suspectId)
        {
            progressManager.SubmitAccusation(suspectId);
            return !string.IsNullOrWhiteSpace(progressManager.AccusationTargetId);
        }
        
        public bool EnterResultPhase()
        {
            return !string.IsNullOrWhiteSpace(progressManager.AccusationTargetId);
        }

        private void HandleEvidenceAdded(EvidenceAddedEvent eventData)
        {   
            Debug.Log($"弹窗跳出，证据ID: {eventData.EvidenceId}");
            if (!evidenceDatabase.TryGetEvidence(eventData.EvidenceId, out var evidenceData) || evidenceData == null)
            {
                return;
            }

            ShowUnlockPopup(evidenceData.summary);
        }

        private void HandleStatementUnlocked(StatementUnlockedEvent eventData)
        {
            if (!statementDatabase.TryGetStatement(eventData.StatementId, out var statementData) || statementData == null)
            {
                return;
            }

            ShowUnlockPopup(statementData.text);
        }

        private void ShowUnlockPopup(string message)
        {
            if (unlockPopupText != null)
            {
                unlockPopupText.text = message ?? string.Empty;
            }

            SetUnlockPopupOpen(!string.IsNullOrWhiteSpace(message));
        }

        private void SetUnlockPopupOpen(bool isOpen)
        {
            SetPanelActive(unlockPopupRoot, isOpen);
            PublishUiBlockState();
        }

        private void SetTransitionOpen(bool isOpen)
        {
            if (transitionUI != null)
            {
                transitionUI.gameObject.SetActive(isOpen);
            }

            PublishUiBlockState();
        }

        private void HandleTransitionFinished()
        {
            CloseAllGameplayBlockingPanels();
            SetTransitionOpen(false);
        }

        private void SubscribeToEvents()
        {
            eventManager.Unsubscribe<EvidenceAddedEvent>(HandleEvidenceAdded);
            eventManager.Unsubscribe<StatementUnlockedEvent>(HandleStatementUnlocked);
            eventManager.Unsubscribe<UiBlockRequestEvent>(HandleUiBlockRequest);
            eventManager.Subscribe<EvidenceAddedEvent>(HandleEvidenceAdded);
            eventManager.Subscribe<StatementUnlockedEvent>(HandleStatementUnlocked);
            eventManager.Subscribe<UiBlockRequestEvent>(HandleUiBlockRequest);
        }

        private void UnsubscribeFromEvents()
        {
            if (eventManager == null)
            {
                return;
            }

            eventManager.Unsubscribe<EvidenceAddedEvent>(HandleEvidenceAdded);
            eventManager.Unsubscribe<StatementUnlockedEvent>(HandleStatementUnlocked);
            eventManager.Unsubscribe<UiBlockRequestEvent>(HandleUiBlockRequest);
        }

        private void HandleUiBlockRequest(UiBlockRequestEvent eventData)
        {
            if (string.IsNullOrWhiteSpace(eventData.SourceId))
            {
                return;
            }

            if (eventData.IsBlocked)
            {
                externalInputBlockers.Add(eventData.SourceId);
            }
            else
            {
                externalInputBlockers.Remove(eventData.SourceId);
            }

            PublishUiBlockState();
        }

        private void BindPopupCloseButton()
        {
            if (unlockPopupCloseButton == null)
            {
                return;
            }

            unlockPopupCloseButton.onClick.RemoveListener(CloseUnlockPopup);
            unlockPopupCloseButton.onClick.AddListener(CloseUnlockPopup);
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

            if (evidencePanelManager == null && inventoryPanelManager != null)
            {
                evidencePanelManager = inventoryPanelManager.GetComponentInChildren<EvidencePanelManager>(true);
            }

            if (evidencePanelManager == null)
            {
                throw new InvalidOperationException("UIManager requires evidencePanelManager to be assigned.");
            }

            if (transitionUI == null)
            {
                throw new InvalidOperationException("UIManager requires transitionUI to be assigned.");
            }

            if (eventManager == null)
            {
                throw new InvalidOperationException("UIManager requires AppRoot.EventManager during initialization.");
            }

            if (evidenceDatabase == null)
            {
                throw new InvalidOperationException("UIManager requires AppRoot.DatabaseManager.EvidenceDatabase during initialization.");
            }

            if (statementDatabase == null)
            {
                throw new InvalidOperationException("UIManager requires AppRoot.DatabaseManager.StatementDatabase during initialization.");
            }

            if (gameStateManager == null)
            {
                throw new InvalidOperationException("UIManager requires AppRoot.GameStateManager during initialization.");
            }

            if (progressManager == null)
            {
                throw new InvalidOperationException("UIManager requires AppRoot.ProgressManager during initialization.");
            }
        }

        private void CloseAllGameplayBlockingPanels()
        {
            SetMenuOpen(false);
            SetInventoryOpen(false);
            SetUnlockPopupOpen(false);
        }

        private void PublishUiBlockState()
        {
            if (eventManager == null)
            {
                return;
            }

            var isBlocked = IsAnyUiBlockingPlayer() || externalInputBlockers.Count > 0;
            if (isBlocked == lastPublishedBlockState)
            {
                return;
            }

            lastPublishedBlockState = isBlocked;
            eventManager.Publish(new UiBlockStateChangedEvent(isBlocked));
        }

        private bool IsAnyUiBlockingPlayer()
        {
            return IsPanelOpen(menuPanelRoot) ||
                   IsPanelOpen(inventoryRoot) ||
                   IsPanelOpen(unlockPopupRoot) ||
                   (transitionUI != null && transitionUI.gameObject.activeSelf);
        }

        private static bool IsPanelOpen(GameObject panel)
        {
            return panel != null && panel.activeSelf;
        }

        private static void SetPanelActive(GameObject panel, bool isOpen)
        {
            if (panel != null)
            {
                panel.SetActive(isOpen);
            }
        }
    }
}
