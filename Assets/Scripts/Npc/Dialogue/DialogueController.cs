using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;
using TMPro;
using UnityEngine;

public class DialogueController : MonoBehaviour {
    public static DialogueController Instance;
    public NPCData currentNPC;
    public TMP_InputField playerInputField;
    [SerializeField] private DeepSeekDialogueClient dialogueClient;
    [SerializeField] private TextAsset basePrompt;
    [SerializeField] private TextAsset npcContextRulesPrompt;
    [SerializeField] private TextAsset revealLogicRulesPrompt;
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
        SendDialogueRequest(playerInput);
    }

    public void StartNpcOpeningDialogue() {
        SendDialogueRequest(
            "The player has approached you at the crime scene. You are already a suspect in this case. Speak first with a short in-character opening line based on your public profile, private context, and current game phase. Do not reveal hidden truth unless the allowed reveal context permits it.");
    }

    private void SendDialogueRequest(string playerInput) {
        if (currentNPC == null) {
            Debug.LogError("Current NPC is not set. Dialogue cannot continue.");
            return;
        }

        if (dialogueClient == null) {
            dialogueClient = DeepSeekDialogueClient.Instance;
        }

        if (dialogueClient == null) {
            Debug.LogError("DeepSeekDialogueClient is not set. Add it to the scene under _CORE/AI.");
            return;
        }

        if (basePrompt == null) {
            Debug.LogError("DialogueController is missing a base prompt TextAsset.");
            return;
        }

        if (npcContextRulesPrompt == null) {
            Debug.LogError("DialogueController is missing an NPC context rules prompt TextAsset.");
            return;
        }

        if (revealLogicRulesPrompt == null) {
            Debug.LogError("DialogueController is missing a reveal logic rules prompt TextAsset.");
            return;
        }

        string systemPrompt = BuildSystemPrompt();

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

    private string BuildSystemPrompt() {
        var prompt = new StringBuilder();
        AppendPromptSection(prompt, basePrompt.text);
        AppendPromptSection(prompt, npcContextRulesPrompt.text);
        AppendPromptSection(prompt, revealLogicRulesPrompt.text);
        AppendNpcContext(prompt);
        AppendRuntimeContext(prompt);
        return prompt.ToString();
    }

    private static void AppendPromptSection(StringBuilder prompt, string sectionText) {
        if (prompt.Length > 0) {
            prompt.AppendLine();
        }

        prompt.AppendLine(sectionText);
    }

    private void AppendNpcContext(StringBuilder prompt) {
        prompt.AppendLine();
        prompt.AppendLine("Current NPC context:");
        prompt.AppendLine($"- Name: {currentNPC.displayName}");
        prompt.AppendLine($"- Backstory: {currentNPC.backstory}");
        prompt.AppendLine($"- Initial statement: {currentNPC.initialStatement}");
        prompt.AppendLine($"- Composure: {currentNPC.stats?.composure ?? 0}");
        prompt.AppendLine($"- Lie: {currentNPC.stats?.lie ?? 0}");
        prompt.AppendLine($"- Aggression: {currentNPC.stats?.aggression ?? 0}");
        prompt.AppendLine($"- Cooperation: {currentNPC.stats?.cooperation ?? 0}");
        prompt.AppendLine($"- Guilt: {currentNPC.stats?.guilt ?? 0}");
        prompt.AppendLine($"- Trauma: {currentNPC.stats?.trauma ?? 0}");
    }

    private void AppendRuntimeContext(StringBuilder prompt) {
        var appRoot = AppRoot.Instance;
        if (appRoot == null || appRoot.DatabaseManager == null || appRoot.ProgressManager == null) {
            prompt.AppendLine();
            prompt.AppendLine("Current game context:");
            prompt.AppendLine("- Runtime context unavailable. Use only the NPC context above.");
            return;
        }

        var databaseManager = appRoot.DatabaseManager;
        var progressManager = appRoot.ProgressManager;
        var currentPhase = appRoot.GameStateManager?.CurrentPhase;

        prompt.AppendLine();
        prompt.AppendLine("Current game context:");
        prompt.AppendLine($"- Phase: {currentPhase?.ToString() ?? "Unknown"}");
        prompt.AppendLine("- Scene premise: The current NPC is already a suspect at the crime scene. They know a death occurred and police are investigating.");

        AppendKnownEvidence(prompt, databaseManager, progressManager);
        AppendKnownFacts(prompt, databaseManager, progressManager);
        AppendNpcTruthContext(prompt, databaseManager, progressManager, currentPhase == GamePhase.Interrogation);
    }

    private static void AppendKnownEvidence(
        StringBuilder prompt,
        DatabaseManager databaseManager,
        ProgressManager progressManager) {
        prompt.AppendLine();
        prompt.AppendLine("Player-known evidence:");
        if (progressManager.CollectedEvidenceIds.Count == 0) {
            prompt.AppendLine("- None");
            return;
        }

        foreach (var evidenceId in progressManager.CollectedEvidenceIds) {
            if (databaseManager.EvidenceDatabase != null &&
                databaseManager.EvidenceDatabase.TryGetEvidence(evidenceId, out var evidence)) {
                prompt.AppendLine($"- {evidenceId}: {evidence.displayName} - {evidence.summary}");
            } else {
                prompt.AppendLine($"- {evidenceId}");
            }
        }
    }

    private static void AppendKnownFacts(
        StringBuilder prompt,
        DatabaseManager databaseManager,
        ProgressManager progressManager) {
        prompt.AppendLine();
        prompt.AppendLine("Player-known facts:");
        if (progressManager.UnlockedFactIds.Count == 0) {
            prompt.AppendLine("- None");
            return;
        }

        foreach (var factId in progressManager.UnlockedFactIds) {
            if (databaseManager.FactDatabase != null &&
                databaseManager.FactDatabase.TryGetFact(factId, out var fact)) {
                prompt.AppendLine($"- {factId}: {fact.displayName} - {fact.summary}");
            } else {
                prompt.AppendLine($"- {factId}");
            }
        }
    }

    private void AppendNpcTruthContext(
        StringBuilder prompt,
        DatabaseManager databaseManager,
        ProgressManager progressManager,
        bool includeInterrogationContext) {
        if (databaseManager.TruthDatabase == null ||
            !databaseManager.TruthDatabase.TryGetNpcTruth(currentNPC.npcId, out var npcTruth)) {
            return;
        }

        prompt.AppendLine();
        prompt.AppendLine("NPC private acting context:");
        prompt.AppendLine($"- Actual relationship to victim: {npcTruth.actualRelationshipToVictim}");
        prompt.AppendLine($"- Is killer: {npcTruth.isKiller}");
        prompt.AppendLine($"- Backstory: {npcTruth.backstory}");
        prompt.AppendLine($"- Real motive: {npcTruth.realMotive}");
        prompt.AppendLine($"- Hidden truth: {npcTruth.hiddenTruth}");
        AppendList(prompt, "NPC knowledge:", npcTruth.knowledge);
        AppendPersonalTimeline(prompt, npcTruth.personalTimeline);

        AppendRevealContext(prompt, npcTruth.dialogueTriggers, npcTruth.interrogationLayers, progressManager, includeInterrogationContext);
    }

    private static void AppendPersonalTimeline(
        StringBuilder prompt,
        IReadOnlyList<NpcPersonalTimelineEntryData> personalTimeline) {
        prompt.AppendLine();
        prompt.AppendLine("NPC personal timeline:");
        if (personalTimeline == null || personalTimeline.Count == 0) {
            prompt.AppendLine("- None");
            return;
        }

        foreach (var timelineEntry in personalTimeline) {
            if (timelineEntry == null) {
                continue;
            }

            prompt.AppendLine($"- {timelineEntry.time} @ {timelineEntry.locationId}: {timelineEntry.@event}");
            prompt.AppendLine($"  Knows: {timelineEntry.whatThisNpcKnows}");
            prompt.AppendLine($"  Claims: {timelineEntry.whatThisNpcClaims}");
            prompt.AppendLine($"  Hides: {timelineEntry.whatThisNpcHides}");
        }
    }

    private static void AppendRevealContext(
        StringBuilder prompt,
        IReadOnlyList<DialogueTriggerData> dialogueTriggers,
        IReadOnlyList<TruthInterrogationLayerData> interrogationLayers,
        ProgressManager progressManager,
        bool includeInterrogationContext) {
        prompt.AppendLine();
        prompt.AppendLine("Allowed Reveal Context:");
        var addedAllowedReveal = false;
        foreach (var trigger in dialogueTriggers ?? new List<DialogueTriggerData>()) {
            if (trigger == null) {
                continue;
            }

            if (!RequirementsSatisfied(trigger.unlockRequirements, progressManager)) {
                continue;
            }

            addedAllowedReveal = true;
            AppendTrigger(prompt, trigger);
        }

        if (!addedAllowedReveal) {
            prompt.AppendLine("- None");
        }

        prompt.AppendLine();
        prompt.AppendLine("Guess-sensitive reveal candidates:");
        var addedGuessReveal = false;
        foreach (var trigger in dialogueTriggers ?? new List<DialogueTriggerData>()) {
            if (trigger == null) {
                continue;
            }

            if (RequirementsSatisfied(trigger.unlockRequirements, progressManager)) {
                continue;
            }

            addedGuessReveal = true;
            AppendTrigger(prompt, trigger);
        }

        if (!addedGuessReveal) {
            prompt.AppendLine("- None");
        }

        prompt.AppendLine();
        prompt.AppendLine("Current NPC interrogation guidance:");
        if (!includeInterrogationContext) {
            prompt.AppendLine("- None. Current phase is not interrogation.");
            return;
        }

        var addedInterrogationGuidance = false;
        foreach (var layer in interrogationLayers ?? new List<TruthInterrogationLayerData>()) {
            if (layer == null) {
                continue;
            }

            if (!RequirementsSatisfied(layer.requiredEvidenceIds, progressManager)) {
                continue;
            }

            addedInterrogationGuidance = true;
            AppendInterrogationLayer(prompt, layer);
        }

        if (!addedInterrogationGuidance) {
            prompt.AppendLine("- None");
        }
    }

    private static void AppendTrigger(StringBuilder prompt, DialogueTriggerData trigger) {
        prompt.AppendLine($"- Trigger: {trigger.triggerId}");
        prompt.AppendLine($"  Topic: {trigger.topic}");
        prompt.AppendLine($"  Reveal goal: {trigger.revealGoal}");
        prompt.AppendLine($"  AI guidance: {trigger.aiGuidance}");
        AppendList(prompt, "  Must withhold:", trigger.withhold);
        AppendList(prompt, "  Example phrasing:", trigger.examplePhrasings);
    }

    private static void AppendInterrogationLayer(StringBuilder prompt, TruthInterrogationLayerData layer) {
        prompt.AppendLine($"- Layer: {layer.layerId}");
        prompt.AppendLine($"  Round type: {layer.roundType}");
        prompt.AppendLine($"  Topic: {layer.topic}");
        prompt.AppendLine($"  Reveal goal: {layer.revealGoal}");
        prompt.AppendLine($"  AI guidance: {layer.aiGuidance}");
        AppendList(prompt, "  Required evidence:", layer.requiredEvidenceIds);
        AppendList(prompt, "  Reveal facts:", layer.revealFactIds);
        AppendList(prompt, "  Example phrasing:", layer.examplePhrasings);
    }

    private static void AppendList(StringBuilder prompt, string heading, IReadOnlyList<string> values) {
        prompt.AppendLine(heading);
        if (values == null || values.Count == 0) {
            prompt.AppendLine("- None");
            return;
        }

        foreach (var value in values) {
            prompt.AppendLine($"- {value}");
        }
    }

    private static bool RequirementsSatisfied(IReadOnlyList<string> requirements, ProgressManager progressManager) {
        if (requirements == null || requirements.Count == 0) {
            return true;
        }

        foreach (var requirement in requirements) {
            if (!progressManager.IsEvidenceCollected(requirement) && !progressManager.IsFactUnlocked(requirement)) {
                return false;
            }
        }

        return true;
    }
}
