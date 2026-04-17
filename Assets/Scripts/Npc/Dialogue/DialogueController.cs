using TMPro;
using UnityEngine;

public class DialogueController : MonoBehaviour {
    public static DialogueController Instance;
    public NPCData currentNPC;
    public TMP_InputField playerInputField;
    [SerializeField] private DeepSeekDialogueClient dialogueClient;
    [SerializeField] private KeyCode submitKey = KeyCode.Return;
    private int nextDialogueRequestId;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }

        if (dialogueClient == null) {
            dialogueClient = DeepSeekDialogueClient.Instance;
        }
    }

    public void OnSubmitButtonClick() {
        string input = playerInputField.text;
        ProcessPlayerInput(input);
        playerInputField.text = string.Empty;
        playerInputField.ActivateInputField();
    }

    private void Update() {
        if (Input.GetKeyDown(submitKey)) {
            OnSubmitButtonClick();
        }
    }

    public void ProcessPlayerInput(string playerInput) {
        if (currentNPC == null) {
            Debug.LogError("Current NPC is not set. Dialogue cannot continue.");
            return;
        }

        if (dialogueClient == null) {
            dialogueClient = DeepSeekDialogueClient.Instance;
        }

        if (dialogueClient == null) {
            Debug.LogError("DeepSeekDialogueClient is not set. Add it to the scene under Gameplay/AI.");
            return;
        }

        string systemPrompt = $"You are NPC {currentNPC.displayName} in a detective game.\n" +
                              $"NPC backstory: {currentNPC.backstory}\n" +
                              $"NPC initial statement: {currentNPC.initialStatement}\n" +
                              "Answer in character. Keep the reply short and conversational.";

        int dialogueRequestId = nextDialogueRequestId++;
        DialogueManager.Instance.ReserveOrderedDialogue(dialogueRequestId);
        StartCoroutine(dialogueClient.SendDialogueRequest(
            systemPrompt,
            playerInput,
            aiResponse => DialogueManager.Instance.ShowDialogueInOrder(dialogueRequestId, aiResponse),
            error => {
                Debug.LogError("DeepSeek dialogue request failed: " + error);
                DialogueManager.Instance.ShowDialogueInOrder(dialogueRequestId, "AI request failed: " + error);
            }));
    }

    public void SetCurrentNPC(NPCData npc) {
        currentNPC = npc;
        Debug.Log("Current dialogue NPC set to: " + npc.displayName);
    }
}
