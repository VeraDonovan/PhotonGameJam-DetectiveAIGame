using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class ProgressState
    {
        public Dictionary<string, bool> EvidenceCollectedById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> FactUnlockedById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> StatementUnlockedById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> InterrogationLayerUnlockedById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> DialogueBeatVisitedById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> CaughtLieById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> ProgressTokenById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, string> LatestStatementIdByTopic { get; } = new Dictionary<string, string>();
        public HashSet<string> CollectedEvidenceIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedFactIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedStatementIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedInterrogationLayerIds { get; } = new HashSet<string>();
        public HashSet<string> VisitedDialogueBeatIds { get; } = new HashSet<string>();
        public HashSet<string> CaughtLieIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedProgressTokens { get; } = new HashSet<string>();
        public HashSet<string> KnownSuspectIds { get; } = new HashSet<string>();
        public HashSet<string> SelectedSuspectIds { get; } = new HashSet<string>();

        public string CurrentInterrogationTargetId { get; set; } = string.Empty;
        public string AccusationTargetId { get; set; } = string.Empty;
    }
}
