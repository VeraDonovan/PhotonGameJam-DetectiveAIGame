using System.Collections.Generic;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueApiPromptContext
    {
        public string NpcId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Exploration;

        public RawDialogueInput RawInput { get; set; } = new RawDialogueInput();
        public DialogueCandidateTopicSet CandidateTopics { get; set; } = new DialogueCandidateTopicSet();
        public NpcData NpcPublicProfile { get; set; }
        public string NpcAiProfileRawJson { get; set; } = string.Empty;

        public int Annoyance { get; set; }
        public int Pressure { get; set; }
        public int CurrentInterrogationLevel { get; set; }
        public string CurrentInterrogationLayerId { get; set; } = string.Empty;

        public List<string> RelevantUnlockedFactIds { get; } = new List<string>();
        public List<string> RelevantUnlockedStatementIds { get; } = new List<string>();
        public List<string> RelevantUnlockedLayerIds { get; } = new List<string>();
        public List<string> RelevantVisitedBeatIds { get; } = new List<string>();
        public List<string> RelevantCaughtLieIds { get; } = new List<string>();
        public List<DialogueStatementEntryContext> RelevantUnlockedStatements { get; } =
            new List<DialogueStatementEntryContext>();
        public List<TruthInterrogationLayerData> AllowedInterrogationLayers { get; } =
            new List<TruthInterrogationLayerData>();

        public List<string> AllowedRevealIds { get; } = new List<string>();
        public List<string> MustWithholdIds { get; } = new List<string>();

        public List<DialogueConversationExchange> RecentConversation { get; } =
            new List<DialogueConversationExchange>();
    }
}
