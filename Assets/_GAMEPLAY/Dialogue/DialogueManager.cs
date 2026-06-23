using System.Collections;
using System.Collections.Generic;
using DetectiveGame.Core;
using DetectiveGame.Gameplay.Dialogue;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance;
    public static System.Action<DialogueTurnDebugInfo> OnTurnDebug; // 测试沙盒用：每轮结算后触发，不影响玩家游戏
    public TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    public GameObject dialoguePanel;
    private bool isDialoguePanelActive;
    private bool hasWarnedExternalPanelToggle;
    private const string UiBlockSourceId = "dialogue_panel";

    [Header("AI Prompt Sections")]
    [SerializeField] private TextAsset dialogueBasePrompt;
    [SerializeField] private TextAsset npcContextRulesPrompt;
    [SerializeField] private TextAsset revealLogicRulesPrompt;

    [Header("Conversation History")]
    [SerializeField] private int recentVerbatimExchangeCount = 6;
    [SerializeField] private int turnSummaryBatchSize = 1;
    [SerializeField] private int openingVerbatimExchangeCount = 0;
    [SerializeField] private bool logConversationHistoryCompression = false;

    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float punctuationDelay = 0.2f;
    [SerializeField] private bool enablePunctuationDelay = true;

    private TMPTypeWriter typeWriter;
    private readonly SortedDictionary<int, string> orderedDialogueQueue = new SortedDictionary<int, string>();
    private readonly Dictionary<string, DialogueConversationSession> conversationSessionByNpcId =
        new Dictionary<string, DialogueConversationSession>();
    private readonly DialogueApiContextAssembler contextAssembler = new DialogueApiContextAssembler();
    private readonly DialogueTurnResolver turnResolver = new DialogueTurnResolver();
    private readonly DialoguePromptBuilder promptBuilder = new DialoguePromptBuilder();
    private readonly IDialogueConversationSummarizer conversationSummarizer = new DialogueConversationSummarizer();
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
        isDialoguePanelActive = false;
        hasWarnedExternalPanelToggle = false;

        if (dialogueText != null) {
            typeWriter = new TMPTypeWriter(dialogueText, typingSpeed, punctuationDelay, enablePunctuationDelay);
            typeWriter.SetCompleteFunc(ShowNextOrderedDialogue);
        }

        ApplyConversationConfig();
    }

    private void ApplyConversationConfig() {
        DialogueConversationConfig.RecentVerbatimExchangeCount = Mathf.Max(1, recentVerbatimExchangeCount);
        DialogueConversationConfig.TurnSummaryBatchSize = Mathf.Max(1, turnSummaryBatchSize);
        DialogueConversationConfig.OpeningVerbatimExchangeCount = Mathf.Max(0, openingVerbatimExchangeCount);
        DialogueHistoryCompressionLogger.SetEnabled(logConversationHistoryCompression);
        if (logConversationHistoryCompression) {
            DialogueHistoryCompressionLogger.LogConfig();
        }
    }

    private static void PromoteTurnSummaryWithLog(DialogueConversationSession session) {
        if (session == null) {
            return;
        }

        bool hadPending = !string.IsNullOrWhiteSpace(session.PendingTurnSummary);
        session.PromotePendingTurnSummaryIfAny();
        DialogueHistoryCompressionLogger.LogPromoteTurn(session, hadPending);
    }

    private static void PromoteOpeningSummaryWithLog(DialogueConversationSession session) {
        if (session == null) {
            return;
        }

        bool hadPending = !string.IsNullOrWhiteSpace(session.PendingOpeningSummary);
        session.PromotePendingOpeningSummaryIfAny();
        DialogueHistoryCompressionLogger.LogPromoteOpening(session, hadPending);
    }

    public void ShowDialogue(string text) {
        SetDialoguePanelActive(true);

        if (typeWriter != null) {
            typeWriter.StartTyping(text);
        } else if (dialogueText != null) {
            dialogueText.text = text;
        }
    }

    public void SetSpeakerName(string speakerName) {
        if (speakerNameText != null) {
            speakerNameText.text = speakerName ?? string.Empty;
        }
    }

    public void SetSpeakerNameByNpcId(string npcId) {
        if (string.IsNullOrWhiteSpace(npcId)) {
            SetSpeakerName(string.Empty);
            return;
        }

        AppRoot appRoot = AppRoot.Instance;
        if (appRoot?.DatabaseManager?.NpcDatabase != null &&
            appRoot.DatabaseManager.NpcDatabase.TryGetNpc(npcId, out var npc) &&
            npc != null) {
            SetSpeakerName(npc.displayName);
            return;
        }

        SetSpeakerName(npcId);
    }

    private void ShowWaitingDialoguePanel() {
        SetDialoguePanelActive(true);

        if (dialogueText != null) {
            dialogueText.text = "……";
        }
    }

    public void SubmitAiDialogueTurn(string npcId, GamePhase phase, string playerText, string presentedEvidenceId) {
        SetSpeakerNameByNpcId(npcId);
        ShowWaitingDialoguePanel();
        StartCoroutine(RunAiDialogueTurn(npcId, phase, playerText, presentedEvidenceId));
    }

    public void RequestAiOpeningDialogue(string npcId, GamePhase phase) {
        SetSpeakerNameByNpcId(npcId);
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
            SchedulePostTurnSummarization(conversationSession, playerText, refusalText, phase, appRoot);
            ShowDialogue(refusalText);
            OnTurnDebug?.Invoke(new DialogueTurnDebugInfo { NpcId = npcId, PlayerText = playerText, NpcText = refusalText, Accepted = false, RejectReason = "non_chinese_input" });
            yield break;
        }

        PromoteTurnSummaryWithLog(conversationSession);

        DialogueApiPromptContext promptContext = contextAssembler.Assemble(
            rawInput,
            appRoot.DatabaseManager,
            appRoot.ProgressManager,
            appRoot.NpcRuntimeManager,
            conversationSession,
            DialoguePromptMode.Turn);

        DialoguePromptMessages promptMessages = promptBuilder.Build(promptContext, promptSections);
        DialogueHistoryCompressionLogger.LogTurnPromptBuilt(conversationSession, promptContext);
        DeepSeekDialogueTurnResponse aiResponse = null;
        string aiError = string.Empty;

        yield return dialogueClient.SendStructuredDialogueRequest(
            promptMessages.SystemMessage,
            promptMessages.UserMessage,
            response => aiResponse = response,
            error => aiError = error);

        if (!string.IsNullOrWhiteSpace(aiError)) {
            Debug.LogError($"[DialogueManager] AI dialogue request failed: {aiError}", this);
            string fallbackText = CreateAiFailureResponse();
            conversationSession.AddExchange(playerText, fallbackText);
            SchedulePostTurnSummarization(conversationSession, playerText, fallbackText, phase, appRoot);
            ShowDialogue(fallbackText);
            OnTurnDebug?.Invoke(new DialogueTurnDebugInfo { NpcId = npcId, PlayerText = playerText, NpcText = fallbackText, Accepted = false, RejectReason = "ai_request_failed: " + aiError });
            yield break;
        }

        InterpretedDialogueAction interpretedAction = CreateInterpretedAction(rawInput, aiResponse);
        DialogueTurnResolution resolution = turnResolver.Resolve(
            rawInput,
            interpretedAction,
            appRoot.DatabaseManager,
            appRoot.ProgressManager,
            appRoot.NpcRuntimeManager);

        string npcText = resolution.ResolutionResult.AcceptAiResponse
            ? aiResponse.response.prose
            : CreateRejectedResponse(resolution.ResolutionResult.ResponseRejectReason);

        conversationSession.AddExchange(playerText, npcText);
        SchedulePostTurnSummarization(conversationSession, playerText, npcText, phase, appRoot);
        ShowDialogue(npcText);

        OnTurnDebug?.Invoke(new DialogueTurnDebugInfo {
            NpcId = npcId,
            PlayerText = playerText,
            NpcText = npcText,
            MatchedTopicId = interpretedAction != null ? interpretedAction.MatchedTopicId : string.Empty,
            Accepted = resolution.ResolutionResult.AcceptAiResponse,
            RejectReason = resolution.ResolutionResult.ResponseRejectReason,
            ResolutionType = resolution.ResolutionResult.ResolutionType.ToString(),
            Pressure = resolution.ResolutionResult.NewPressure,
            Annoyance = resolution.ResolutionResult.NewAnnoyance,
            UnlockedFactIds = new List<string>(resolution.ResolutionResult.UnlockedFactIds),
            UnlockedLayerIds = new List<string>(resolution.ResolutionResult.UnlockedLayerIds),
            UnlockedStatementIds = new List<string>(resolution.ResolutionResult.UnlockedStatementIds),
        });
    }

    private IEnumerator RunAiOpeningDialogue(string npcId, GamePhase phase) {
        AppRoot appRoot = AppRoot.Instance;
        if (appRoot == null) {
            Debug.LogError("[DialogueManager] AI opening dialogue requires AppRoot.Instance.", this);
            yield break;
        }

        if (TryPlayAuthoredOpeningBeat(appRoot, npcId, phase, out var openingBeatText))
        {
            GetOrCreateConversationSession(npcId).AddExchange(string.Empty, openingBeatText);
            ShowDialogue(openingBeatText);
            OnTurnDebug?.Invoke(new DialogueTurnDebugInfo { NpcId = npcId, PlayerText = "(开场白)", NpcText = openingBeatText, MatchedTopicId = "opening_beat", ResolutionType = "Opening" });
            yield break;
        }

        DeepSeekDialogueClient dialogueClient = DeepSeekDialogueClient.Instance;
        if (dialogueClient == null) {
            Debug.LogError("[DialogueManager] AI opening dialogue requires DeepSeekDialogueClient.Instance.", this);
            yield break;
        }

        RawDialogueInput rawInput = new RawDialogueInput {
            NpcId = npcId,
            Phase = phase,
            RawPlayerText = string.Empty,
            PresentedEvidenceId = string.Empty,
        };

        DialogueConversationSession conversationSession = GetOrCreateConversationSession(npcId);
        PromoteOpeningSummaryWithLog(conversationSession);

        DialogueApiPromptContext promptContext = contextAssembler.Assemble(
            rawInput,
            appRoot.DatabaseManager,
            appRoot.ProgressManager,
            appRoot.NpcRuntimeManager,
            conversationSession,
            DialoguePromptMode.Opening);

        DialoguePromptMessages openingPromptMessages = promptBuilder.BuildOpening(promptContext);
        DialogueHistoryCompressionLogger.LogOpeningPromptBuilt(conversationSession, promptContext);

        string aiOpeningText = string.Empty;
        string aiError = string.Empty;

        yield return dialogueClient.SendDialogueRequest(
            openingPromptMessages.SystemMessage,
            openingPromptMessages.UserMessage,
            response => aiOpeningText = response,
            error => aiError = error,
            maxTokensOverride: 300);

        if (!string.IsNullOrWhiteSpace(aiError)) {
            Debug.LogError($"[DialogueManager] AI opening dialogue request failed: {aiError}", this);
            string openingFallback = CreateOpeningFallbackText(appRoot, npcId);
            ShowDialogue(openingFallback);
            OnTurnDebug?.Invoke(new DialogueTurnDebugInfo { NpcId = npcId, PlayerText = "(开场白)", NpcText = openingFallback, Accepted = false, RejectReason = "ai_request_failed: " + aiError });
            yield break;
        }

        string npcText = !string.IsNullOrWhiteSpace(aiOpeningText)
            ? NormalizeOpeningResponseText(aiOpeningText)
            : CreateOpeningFallbackText(appRoot, npcId);

        conversationSession.AddExchange(string.Empty, npcText);
        ShowDialogue(npcText);
        OnTurnDebug?.Invoke(new DialogueTurnDebugInfo { NpcId = npcId, PlayerText = "(开场白)", NpcText = npcText, MatchedTopicId = "opening", ResolutionType = "Opening" });
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

    private static string NormalizeOpeningResponseText(string rawText) {
        string text = (rawText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text)) {
            return string.Empty;
        }

        if (text.StartsWith("{") && text.EndsWith("}")) {
            try {
                DeepSeekDialogueTurnResponse structuredResponse =
                    JsonUtility.FromJson<DeepSeekDialogueTurnResponse>(text);
                if (structuredResponse != null &&
                    structuredResponse.response != null &&
                    !string.IsNullOrWhiteSpace(structuredResponse.response.prose)) {
                    return structuredResponse.response.prose.Trim();
                }
            } catch {
            }
        }

        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"') {
            text = text.Substring(1, text.Length - 2)
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        return text.Trim();
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

    private static string CreateAiFailureResponse() {
        return "这件事我现在不想说。";
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

    private void SchedulePostTurnSummarization(
        DialogueConversationSession session,
        string playerText,
        string npcText,
        GamePhase phase,
        AppRoot appRoot) {
        if (session == null) {
            return;
        }

        int annoyance = 0;
        if (appRoot?.NpcRuntimeManager != null) {
            var npcState = appRoot.NpcRuntimeManager.GetOrCreateDialogueState(session.NpcId);
            annoyance = phase == GamePhase.Exploration ? npcState.Annoyance : 0;
        }

        var latestExchange = new DialogueConversationExchange {
            PlayerText = playerText ?? string.Empty,
            NpcText = npcText ?? string.Empty,
        };

        StartCoroutine(conversationSummarizer.MaybeSummarizeTurnOverflow(session));
        StartCoroutine(conversationSummarizer.MaybeUpdateOpeningSummary(
            session,
            latestExchange,
            phase,
            annoyance));
        DialogueHistoryCompressionLogger.LogTurnEndScheduled(session);
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
            UsedBeatId = aiResponse.response.usedBeatId ?? string.Empty,
            UsedStatementId = aiResponse.response.usedStatementId ?? string.Empty,
            UsedRevealIds = aiResponse.response.usedRevealIds ?? System.Array.Empty<string>(),
        };
    }

    private static bool TryPlayAuthoredOpeningBeat(AppRoot appRoot, string npcId, GamePhase phase, out string openingBeatText)
    {
        openingBeatText = string.Empty;
        if (appRoot?.DatabaseManager?.DialogueBeatDatabase == null || appRoot.ProgressManager == null)
        {
            return false;
        }

        foreach (var topic in appRoot.DatabaseManager.DialogueBeatDatabase.GetTopicsByNpc(npcId))
        {
            foreach (var node in topic.nodes ?? new List<DialogueBeatNodeData>())
            {
                if (node == null ||
                    !string.Equals(node.phase, phase.ToString(), System.StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(node.availabilityType, "auto_opening", System.StringComparison.OrdinalIgnoreCase) ||
                    appRoot.ProgressManager.IsDialogueBeatVisited(node.nodeId))
                {
                    continue;
                }

                if (!appRoot.ProgressManager.VisitDialogueBeat(node.nodeId))
                {
                    continue;
                }

                openingBeatText = node.text ?? string.Empty;
                return !string.IsNullOrWhiteSpace(openingBeatText);
            }
        }

        return false;
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
        SetDialoguePanelActive(false);
    }

    public void OnCloseButtonClick() {
        Debug.Log("该加载underpanel了");
        HideDialogue();

        var appRoot = AppRoot.Instance;
    if (appRoot != null && appRoot.UIManager != null && appRoot.UIManager.UnderPanel != null) {
        appRoot.UIManager.UnderPanel.SetActive(true);
        Debug.Log("[DialogueManager] 点击关闭按钮，重新打开 UnderPanel");
    }
    }

    private void SetDialoguePanelActive(bool isActive) {
        if (!hasWarnedExternalPanelToggle && dialoguePanel != null && dialoguePanel.activeSelf != isDialoguePanelActive) {
            hasWarnedExternalPanelToggle = true;
            Debug.LogWarning("[DialogueManager] dialoguePanel active state was changed outside DialogueManager. Use DialogueManager.ShowDialogue/HideDialogue to keep UI-block events consistent.", this);
        }

        if (dialoguePanel != null) {
            dialoguePanel.SetActive(isActive);
        }

        if (isDialoguePanelActive == isActive) {
            return;
        }

        isDialoguePanelActive = isActive;
        AppRoot appRoot = AppRoot.Instance;
        appRoot?.EventManager?.Publish(new UiBlockRequestEvent(UiBlockSourceId, isActive));
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
