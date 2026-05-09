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

    private void ShowWaitingDialoguePanel() {
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(true);
        }

        if (dialogueText != null) {
            dialogueText.text = "……";
        }
    }

    public void SubmitAiDialogueTurn(string npcId, GamePhase phase, string playerText, string presentedEvidenceId) {
        ShowWaitingDialoguePanel();
        StartCoroutine(RunAiDialogueTurn(npcId, phase, playerText, presentedEvidenceId));
    }

    public void RequestAiOpeningDialogue(string npcId, GamePhase phase) {
        ShowWaitingDialoguePanel();
        StartCoroutine(RunAiOpeningDialogue(npcId, phase));
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
        if (ShouldRefuseNonChineseInput(playerText)) {
            const string refusalText = "我听不懂你在说什么。";
            conversationSession.AddExchange(playerText, refusalText);
            ShowDialogue(refusalText);
            yield break;
        }

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
            ShowDialogue("对话请求失败。");
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

    private IEnumerator RunAiOpeningDialogue(string npcId, GamePhase phase) {
        AppRoot appRoot = AppRoot.Instance;
        if (appRoot == null) {
            Debug.LogError("[DialogueManager] AI opening dialogue requires AppRoot.Instance.", this);
            yield break;
        }

        DeepSeekDialogueClient dialogueClient = DeepSeekDialogueClient.Instance;
        if (dialogueClient == null) {
            Debug.LogError("[DialogueManager] AI opening dialogue requires DeepSeekDialogueClient.Instance.", this);
            yield break;
        }

        if (!TryBuildPromptSections(out DialoguePromptSections promptSections)) {
            yield break;
        }

        RawDialogueInput rawInput = new RawDialogueInput {
            NpcId = npcId,
            Phase = phase,
            RawPlayerText = "当前是这次对话的开场。玩家刚刚开始接触你，还没有输入具体问题。请根据当前阶段、已知状态和最近对话，自然地先说一句中文开场白。",
            PresentedEvidenceId = string.Empty,
        };

        DialogueConversationSession conversationSession = GetOrCreateConversationSession(npcId);
        DialogueTurnContext promptContext = turnResolver.BuildPromptContext(
            rawInput,
            appRoot.DatabaseManager,
            appRoot.ProgressManager,
            appRoot.NpcRuntimeManager,
            conversationSession);

        DialoguePromptMessages promptMessages = promptBuilder.Build(promptContext, promptSections);
        string openingUserPrompt =
            promptMessages.UserMessage +
            "\n\n开场模式：\n" +
            "这是NPC主动开口的第一句，玩家这一回合还没有输入具体问题。\n" +
            "请根据当前阶段、当前可谈话题、已知状态和最近对话，用中文自然地先说一句开场白。\n" +
            "不要输出JSON，不要解释规则，不要使用旁白。\n" +
            "如果当前状态适合寒暄或试探，就先用符合角色的简短开场。";

        string aiOpeningText = string.Empty;
        string aiError = string.Empty;

        yield return dialogueClient.SendDialogueRequest(
            promptMessages.SystemMessage,
            openingUserPrompt,
            response => aiOpeningText = response,
            error => aiError = error);

        if (!string.IsNullOrWhiteSpace(aiError)) {
            Debug.LogError($"[DialogueManager] AI opening dialogue request failed: {aiError}", this);
            ShowDialogue(CreateOpeningFallbackText(appRoot, npcId));
            yield break;
        }

        string npcText = !string.IsNullOrWhiteSpace(aiOpeningText)
            ? aiOpeningText
            : CreateOpeningFallbackText(appRoot, npcId);

        conversationSession.AddExchange(string.Empty, npcText);
        ShowDialogue(npcText);
    }

    private static bool ShouldRefuseNonChineseInput(string playerText) {
        if (string.IsNullOrWhiteSpace(playerText)) {
            return false;
        }

        var hasChineseCharacter = false;
        var hasLatinLetter = false;
        foreach (char character in playerText) {
            if (IsChineseCharacter(character)) {
                hasChineseCharacter = true;
                continue;
            }

            if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z')) {
                hasLatinLetter = true;
            }
        }

        return hasLatinLetter && !hasChineseCharacter;
    }

    private static bool IsChineseCharacter(char character) {
        return (character >= '\u3400' && character <= '\u4DBF') ||
               (character >= '\u4E00' && character <= '\u9FFF') ||
               (character >= '\uF900' && character <= '\uFAFF');
    }

    private static string CreateOpeningFallbackText(AppRoot appRoot, string npcId) {
        if (appRoot != null &&
            appRoot.DatabaseManager != null &&
            appRoot.DatabaseManager.NpcDatabase.TryGetNpc(npcId, out var npcProfile) &&
            npcProfile != null &&
            !string.IsNullOrWhiteSpace(npcProfile.profileText)) {
            return npcProfile.profileText;
        }

        return "你想问什么？";
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
            return "我已经回答过这个问题了。";
        }

        return "我现在不想谈这个。";
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
