using System.Collections;
using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;
using DetectiveGame.Gameplay.Dialogue;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance;
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
        isDialoguePanelActive = false;
        hasWarnedExternalPanelToggle = false;

        if (dialogueText != null) {
            typeWriter = new TMPTypeWriter(dialogueText, typingSpeed, punctuationDelay, enablePunctuationDelay);
            typeWriter.SetCompleteFunc(ShowNextOrderedDialogue);
        }
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
            string fallbackText = CreateAiFailureResponse();
            conversationSession.AddExchange(playerText, fallbackText);
            ShowDialogue(fallbackText);
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

        if (TryPlayAuthoredOpeningBeat(appRoot, npcId, phase, out var openingBeatText))
        {
            GetOrCreateConversationSession(npcId).AddExchange(string.Empty, openingBeatText);
            ShowDialogue(openingBeatText);
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
        DialogueTurnContext promptContext = turnResolver.BuildPromptContext(
            rawInput,
            appRoot.DatabaseManager,
            appRoot.ProgressManager,
            appRoot.NpcRuntimeManager,
            conversationSession);

        string openingSystemPrompt = BuildOpeningSystemPrompt();
        string openingUserPrompt = BuildOpeningUserPrompt(promptContext);

        string aiOpeningText = string.Empty;
        string aiError = string.Empty;

        yield return dialogueClient.SendDialogueRequest(
            openingSystemPrompt,
            openingUserPrompt,
            response => aiOpeningText = response,
            error => aiError = error,
            maxTokensOverride: 300);

        if (!string.IsNullOrWhiteSpace(aiError)) {
            Debug.LogError($"[DialogueManager] AI opening dialogue request failed: {aiError}", this);
            ShowDialogue(CreateOpeningFallbackText(appRoot, npcId));
            yield break;
        }

        string npcText = !string.IsNullOrWhiteSpace(aiOpeningText)
            ? NormalizeOpeningResponseText(aiOpeningText)
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

    private static string BuildOpeningSystemPrompt() {
        return
            "你在生成一款中文侦探游戏里的NPC开场白。\n" +
            "玩家是来调查案件的警察，你是在对警察开口说第一句话。\n" +
            "只返回一句简短的中文台词。\n" +
            "不要输出JSON。\n" +
            "不要解释规则。\n" +
            "不要复述提示词。\n" +
            "不要输出旁白、括号说明、系统信息或分析。\n" +
            "只根据给定的公开资料、当前阶段和最近对话，用符合角色的方式先开口。";
    }

    private static string BuildOpeningUserPrompt(DialogueTurnContext context) {
        var builder = new StringBuilder();
        builder.AppendLine("当前是NPC主动开口的开场。");
        builder.Append("阶段: ");
        builder.AppendLine(context.Phase.ToString());
        builder.AppendLine("玩家身份: 警察");
        builder.AppendLine("场景: 你正在接受警方关于案件的问话。");

        if (context.NpcPublicProfile != null) {
            builder.Append("姓名: ");
            builder.AppendLine(context.NpcPublicProfile.displayName ?? string.Empty);
            builder.Append("身份: ");
            builder.AppendLine(context.NpcPublicProfile.occupation ?? string.Empty);
            builder.Append("与死者关系: ");
            builder.AppendLine(context.NpcPublicProfile.relationshipToVictim ?? string.Empty);
            builder.Append("公开简介: ");
            builder.AppendLine(context.NpcPublicProfile.profileText ?? string.Empty);
        }

        builder.AppendLine("最近对话:");
        if (context.RecentConversation == null || context.RecentConversation.Count == 0) {
            builder.AppendLine("无");
        } else {
            int startIndex = context.RecentConversation.Count > 2
                ? context.RecentConversation.Count - 2
                : 0;
            for (int i = startIndex; i < context.RecentConversation.Count; i++) {
                var exchange = context.RecentConversation[i];
                builder.Append("玩家: ");
                builder.AppendLine(exchange.PlayerText ?? string.Empty);
                builder.Append("NPC: ");
                builder.AppendLine(exchange.NpcText ?? string.Empty);
            }
        }

        builder.AppendLine("要求:");
        builder.AppendLine("玩家刚刚开始接触你，这一回合还没有输入具体问题。");
        builder.AppendLine("你要意识到对方是警察，因此开场语气要符合被警方询问时的反应。");
        builder.AppendLine("请直接说一句自然的中文开场白。");
        return builder.ToString();
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
                    !string.Equals(node.phase, phase.ToString(), System.StringComparison.OrdinalIgnoreCase) &&
                    !(phase == GamePhase.Intro && string.Equals(node.phase, "exploration", System.StringComparison.OrdinalIgnoreCase)) ||
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
        HideDialogue();
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
