using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class CaseRuntimeState
    {
        public HashSet<string> CollectedEvidenceIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedFactIds { get; } = new HashSet<string>();
        public HashSet<string> KnownSuspectIds { get; } = new HashSet<string>();
        public HashSet<string> SelectedSuspectIds { get; } = new HashSet<string>();

        public string AccusationTargetId { get; set; } = string.Empty;
    }
}
