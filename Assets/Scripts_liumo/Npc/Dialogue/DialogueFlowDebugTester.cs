using TMPro;
using UnityEngine;

public class DialogueFlowDebugTester : MonoBehaviour {
    [SerializeField] private DialogueController dialogueController;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private TMP_InputField playerInputField;

    [Header("Debug NPC")]
    [SerializeField] private string npcId = "debug_npc";
    [SerializeField] private string npcDisplayName = "Debug NPC";
    [SerializeField] private string npcBackstory = "A temporary NPC used only for dialogue flow testing.";
    [SerializeField] private string npcInitialStatement = "I am ready. Ask me anything.";

    [Header("Opening Lines")]
    [SerializeField] private string[] openingLines = {
        "Debug dialogue started. Type a question and press Enter.",
        "Dialogue flow test is active. Ask the NPC something.",
        "Test NPC is listening. Enter any text to continue."
    };

    private void Awake() {
        if (dialogueController == null) {
            dialogueController = DialogueController.Instance;
        }

        if (dialogueManager == null) {
            dialogueManager = DialogueManager.Instance;
        }

        if (playerInputField == null && dialogueController != null) {
            playerInputField = dialogueController.playerInputField;
        }
    }

    private void Start() {
        if (dialogueController == null || dialogueManager == null || playerInputField == null) {
            Debug.LogError("DialogueFlowDebugTester needs DialogueController, DialogueManager, and TMP_InputField references.");
            return;
        }

        dialogueController.playerInputField = playerInputField;
        dialogueController.SetCurrentNPC(CreateDebugNpc());
        ShowRandomOpeningLine();
    }

    public void SubmitCurrentInput() {
        if (dialogueController == null || playerInputField == null) {
            return;
        }

        dialogueController.ProcessPlayerInput(playerInputField.text);
        playerInputField.text = string.Empty;
        playerInputField.ActivateInputField();
    }

    private void ShowRandomOpeningLine() {
        int index = Random.Range(0, openingLines.Length);
        dialogueManager.ShowDialogue(openingLines[index]);
    }

    private NPCData CreateDebugNpc() {
        return new NPCData {
            npcId = npcId,
            displayName = npcDisplayName,
            backstory = npcBackstory,
            initialStatement = npcInitialStatement
        };
    }
}
