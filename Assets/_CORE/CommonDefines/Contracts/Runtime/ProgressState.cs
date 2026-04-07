using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class ProgressState
    {
        public Dictionary<string, bool> EvidenceCollectedById { get; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> FactUnlockedById { get; } = new Dictionary<string, bool>();
        public HashSet<string> CollectedEvidenceIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedFactIds { get; } = new HashSet<string>();
        public HashSet<string> KnownSuspectIds { get; } = new HashSet<string>();
        public HashSet<string> SelectedSuspectIds { get; } = new HashSet<string>();

        public string AccusationTargetId { get; set; } = string.Empty;
    }
}
