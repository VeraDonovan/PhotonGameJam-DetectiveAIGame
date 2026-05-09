using System.Collections.Generic;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueCandidateTopic
    {
        public string TopicId { get; set; } = string.Empty;
        public string NpcId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsSynthetic { get; set; }
        public bool IsSafeRoleplayTopic { get; set; }
        public bool IsSearchPhaseTopic { get; set; }
        public bool IsInterrogationPhaseTopic { get; set; }
        public bool HasUnlockedStatementVersion { get; set; }
        public DialogueTopicAvailability Availability { get; set; } = DialogueTopicAvailability.Unknown;
        public List<string> MatchHints { get; } = new List<string>();
        public List<string> RelatedStatementIds { get; } = new List<string>();
        public List<DialogueStatementEntryContext> RelatedStatements { get; } =
            new List<DialogueStatementEntryContext>();
        public List<string> RelatedInterrogationLayerIds { get; } = new List<string>();
        public List<string> RequiredEvidenceIds { get; } = new List<string>();
        public List<string> RequiredFactIds { get; } = new List<string>();
        public List<string> RequiredStatementIds { get; } = new List<string>();
        public List<string> RequiredInterrogationLayerIds { get; } = new List<string>();
        public List<string> RequiredTokenIds { get; } = new List<string>();
        public List<string> MissingRequirementIds { get; } = new List<string>();
    }

    public sealed class DialogueStatementEntryContext
    {
        public string StatementId { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string AiUsage { get; set; } = string.Empty;
        public string ResponseIntent { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }
        public bool IsUnlockable { get; set; }
        public List<string> UnlockRequirements { get; } = new List<string>();
        public List<string> DialogueSamples { get; } = new List<string>();
        public List<string> AvoidSaying { get; } = new List<string>();
    }
}
