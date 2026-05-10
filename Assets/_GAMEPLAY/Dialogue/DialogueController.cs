using DetectiveGame.Core;
using TMPro;
using UnityEngine;

public class DialogueController : MonoBehaviour {
    public static DialogueController Instance;
    public NPCData currentNPC;
    public TMP_InputField playerInputField;
    [SerializeField] private TMP_Text presentedEvidenceNameText;
    [SerializeField] private KeyCode submitKey = KeyCode.Return;
    private string currentNpcId = string.Empty;
    private string pendingPresentedEvidenceId = string.Empty;
    private string pendingPresentedEvidenceName = string.Empty;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }

        RefreshPresentedEvidenceDisplay();
    }

    public void OnSubmitButtonClick() {
        string input = playerInputField.text;
        SubmitPlayerInput(input, pendingPresentedEvidenceId);
        ClearPendingPresentedEvidence();
        playerInputField.text = string.Empty;
        playerInputField.ActivateInputField();
    }

    public void OnPresentEvidenceButtonClick() {
        var uiManager = AppRoot.Instance != null ? AppRoot.Instance.UIManager : null;
        if (uiManager == null) {
            Debug.LogError("UIManager is not available. Evidence selection cannot open.");
            return;
        }

        uiManager.OpenEvidenceSelectionForDialogue(HandleEvidenceSelectedForDialogue);
    }

    private void Update() {
        if (Input.GetKeyDown(submitKey)) {
            OnSubmitButtonClick();
        }
    }

    public void ProcessPlayerInput(string playerInput) {
        SubmitPlayerInput(playerInput, string.Empty);
    }

    public void SubmitPlayerInput(string playerInput, string presentedEvidenceId) {
        if (string.IsNullOrWhiteSpace(currentNpcId)) {
            Debug.LogError("Current NPC is not set. Dialogue cannot continue.");
            return;
        }

        if (DialogueManager.Instance == null) {
            Debug.LogError("DialogueManager is not available. Dialogue cannot continue.");
            return;
        }

        DialogueManager.Instance.SubmitAiDialogueTurn(
            currentNpcId,
            GetCurrentPhase(),
            playerInput,
            presentedEvidenceId ?? string.Empty);
    }

    public void StartNpcOpeningDialogue() {
        if (string.IsNullOrWhiteSpace(currentNpcId)) {
            Debug.LogError("Current NPC is not set. Opening dialogue cannot start.");
            return;
        }

        if (DialogueManager.Instance == null) {
            Debug.LogError("DialogueManager is not available. Opening dialogue cannot continue.");
            return;
        }

        DialogueManager.Instance.RequestAiOpeningDialogue(currentNpcId, GetCurrentPhase());
    }

    public void SetCurrentNPC(NPCData npc) {
        currentNPC = npc;
        currentNpcId = npc != null ? npc.npcId : string.Empty;
        if (DialogueManager.Instance != null) {
            DialogueManager.Instance.SetSpeakerName(npc != null ? npc.displayName : string.Empty);
        }
        Debug.Log("Current dialogue NPC set to: " + (npc != null ? npc.displayName : string.Empty));
    }

    public void SetCurrentNpcId(string npcId) {
        currentNpcId = npcId ?? string.Empty;
        currentNPC = null;
        if (DialogueManager.Instance != null) {
            DialogueManager.Instance.SetSpeakerNameByNpcId(currentNpcId);
        }
        Debug.Log("Current dialogue NPC id set to: " + currentNpcId);
    }

    private void HandleEvidenceSelectedForDialogue(string evidenceId, string displayName) {
        pendingPresentedEvidenceId = evidenceId ?? string.Empty;
        pendingPresentedEvidenceName = displayName ?? string.Empty;
        RefreshPresentedEvidenceDisplay();
        playerInputField?.ActivateInputField();
    }

    private void ClearPendingPresentedEvidence() {
        pendingPresentedEvidenceId = string.Empty;
        pendingPresentedEvidenceName = string.Empty;
        RefreshPresentedEvidenceDisplay();
    }

    private void RefreshPresentedEvidenceDisplay() {
        if (presentedEvidenceNameText != null) {
            presentedEvidenceNameText.text = pendingPresentedEvidenceName;
        }
    }

    private static GamePhase GetCurrentPhase() {
        var appRoot = AppRoot.Instance;
        if (appRoot == null || appRoot.GameStateManager == null) {
            return GamePhase.Exploration;
        }

        return appRoot.GameStateManager.CurrentPhase;
    }
}
