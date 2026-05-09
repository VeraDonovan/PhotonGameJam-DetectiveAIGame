using System.Collections;
using System.Collections.Generic;
using DetectiveGame.Core;
using DetectiveGame.Gameplay.Dialogue;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance;
    public TMP_Text dialogueText;
    public GameObject dialoguePanel;

    [Header("AI Prompt Sections")]
    [SerializeField] private TextAsset dialogueBasePrompt;
    [SerializeField] private TextAsset npcContextRulesPrompt;
    [SerializeField] private TextAsset revealLogicRulesPrompt;

    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float punctuationDelay = 0.2f;
    [SerializeField] private bool enablePunctuationDelay = true;

    private TMPTypeWriter typeWriter;
    private readonly SortedDictionary<int, string> orderedDialogueQueue = new SortedDictionary<int, string>();
    private readonly Dictionary<string, DialogueConversationSession> conversationSessionByNpcId =
        new Dictionary<string, DialogueConversationSession>();
    private readonly DialogueTurnResolver turnResolver = new DialogueTurnResolver();
    private readonly DialoguePromptBuilder promptBuilder = new DialoguePromptBuilder();
    private int nextOrderedDialogueId;
    private bool hasOrderedDialogue;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }

        if (dialoguePanel != null) {
            dialoguePanel.SetActive(false);
        }

        if (dialogueText != null) {
            typeWriter = new TMPTypeWriter(dialogueText, typingSpeed, punctuationDelay, enablePunctuationDelay);
            typeWriter.SetCompleteFunc(ShowNextOrderedDialogue);
        }
    }

    public void ShowDialogue(string text) {
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(true);
        }

        if (typeWriter != null) {
            typeWriter.StartTyping(text);
        } else if (dialogueText != null) {
            dialogueText.text = text;
        }
    }

    public void SubmitAiDialogueTurn(string npcId, GamePhase phase, string playerText, string presentedEvidenceId) {
        StartCoroutine(RunAiDialogueTurn(npcId, phase, playerText, presentedEvidenceId));
    }

    private IEnumerator RunAiDialogueTurn(string npcId, GamePhase phase, string playerText, string presentedEvidenceId) {
        AppRoot appRoot = AppRoot.Instance;
        if (appRoot == null) {
            Debug.LogError("[DialogueManager] AI dialogue requires AppRoot.Instance.", this);
            yield break;
        }

        DeepSeekDialogueClient dialogueClient = DeepSeekDialogueClient.Instance;
        if (dialogueClient == null) {
            Debug.LogError("[DialogueManager] AI dialogue requires DeepSeekDialogueClient.Instance.", this);
            yield break;
        }

        if (!TryBuildPromptSections(out DialoguePromptSections promptSections)) {
            yield break;
        }

        RawDialogueInput rawInput = new RawDialogueInput {
            NpcId = npcId,
            Phase = phase,
            RawPlayerText = playerText,
            PresentedEvidenceId = presentedEvidenceId,
        };

        DialogueConversationSession conversationSession = GetOrCreateConversationSession(npcId);
        DialogueTurnContext promptContext = turnResolver.BuildPromptContext(
            rawInput,
            appRoot.DatabaseManager,
            appRoot.ProgressManager,
            appRoot.NpcRuntimeManager,
            conversationSession);

        DialoguePromptMessages promptMessages = promptBuilder.Build(promptContext, promptSections);
        DeepSeekDialogueTurnResponse aiResponse = null;
        string aiError = string.Empty;

        yield return dialogueClient.SendStructuredDialogueRequest(
            promptMessages.SystemMessage,
            promptMessages.UserMessage,
            response => aiResponse = response,
            error => aiError = error);

        if (!string.IsNullOrWhiteSpace(aiError)) {
            Debug.LogError($"[DialogueManager] AI dialogue request failed: {aiError}", this);
            ShowDialogue("Dialogue request failed.");
            yield break;
        }

        InterpretedDialogueAction interpretedAction = CreateInterpretedAction(rawInput, aiResponse);
        DialogueTurnContext resolvedContext = turnResolver.Resolve(
            rawInput,
            interpretedAction,
            appRoot.DatabaseManager,
            appRoot.ProgressManager,
            appRoot.NpcRuntimeManager,
            conversationSession);

        string npcText = resolvedContext.ResolutionResult.AcceptAiResponse
            ? aiResponse.response.prose
            : CreateRejectedResponse(resolvedContext.ResolutionResult.ResponseRejectReason);

        conversationSession.AddExchange(playerText, npcText);
        ShowDialogue(npcText);
    }

    private bool TryBuildPromptSections(out DialoguePromptSections promptSections) {
        promptSections = null;

        if (dialogueBasePrompt == null) {
            Debug.LogError("[DialogueManager] Missing dialogue base prompt TextAsset.", this);
            return false;
        }

        if (npcContextRulesPrompt == null) {
            Debug.LogError("[DialogueManager] Missing NPC context rules prompt TextAsset.", this);
            return false;
        }

        if (revealLogicRulesPrompt == null) {
            Debug.LogError("[DialogueManager] Missing reveal logic rules prompt TextAsset.", this);
            return false;
        }

        promptSections = new DialoguePromptSections {
            DialogueBasePrompt = dialogueBasePrompt.text,
            NpcContextRulesPrompt = npcContextRulesPrompt.text,
            RevealLogicRulesPrompt = revealLogicRulesPrompt.text,
        };

        return true;
    }

    private DialogueConversationSession GetOrCreateConversationSession(string npcId) {
        if (!conversationSessionByNpcId.TryGetValue(npcId, out DialogueConversationSession session)) {
            session = new DialogueConversationSession(npcId);
            conversationSessionByNpcId.Add(npcId, session);
        }

        return session;
    }

    private static InterpretedDialogueAction CreateInterpretedAction(
        RawDialogueInput rawInput,
        DeepSeekDialogueTurnResponse aiResponse) {
        string topicId = aiResponse.interpretation.topicId ?? string.Empty;
        bool isIrrelevant = aiResponse.interpretation.isIrrelevant ||
                            string.Equals(topicId, "irrelevant", System.StringComparison.OrdinalIgnoreCase);

        return new InterpretedDialogueAction {
            NpcId = rawInput.NpcId,
            Phase = rawInput.Phase,
            MatchedTopicId = isIrrelevant ? string.Empty : topicId,
            ActionType = DialogueActionType.Unknown,
            PresentedEvidenceId = rawInput.PresentedEvidenceId,
            Confidence = aiResponse.interpretation.confidence,
            IsIrrelevant = isIrrelevant,
            UsedStatementId = aiResponse.response.usedStatementId ?? string.Empty,
            UsedRevealIds = aiResponse.response.usedRevealIds ?? System.Array.Empty<string>(),
        };
    }

    private static string CreateRejectedResponse(string rejectReason) {
        if (string.Equals(rejectReason, "resolved_topic_repeated", System.StringComparison.Ordinal)) {
            return "I already answered that.";
        }

        return "I do not want to talk about that right now.";
    }

    public void ReserveOrderedDialogue(int dialogueId) {
        if (!hasOrderedDialogue || dialogueId < nextOrderedDialogueId) {
            nextOrderedDialogueId = dialogueId;
            hasOrderedDialogue = true;
        }
    }

    public void ShowDialogueInOrder(int dialogueId, string text) {
        orderedDialogueQueue[dialogueId] = text;
        ShowNextOrderedDialogue();
    }

    public void HideDialogue() {
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(false);
        }
    }

    private void ShowNextOrderedDialogue() {
        if (typeWriter == null || typeWriter.IsTyping() || !hasOrderedDialogue) {
            return;
        }

        if (!orderedDialogueQueue.TryGetValue(nextOrderedDialogueId, out string text)) {
            return;
        }

        orderedDialogueQueue.Remove(nextOrderedDialogueId);
        nextOrderedDialogueId++;
        ShowDialogue(text);
    }

    private void OnDestroy() {
        typeWriter?.OnDestroy();
    }
}
