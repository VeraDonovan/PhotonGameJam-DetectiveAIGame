using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class RawDialogueInput
    {
        public string NpcId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Exploration;
        public string RawPlayerText { get; set; } = string.Empty;
        public string PresentedEvidenceId { get; set; } = string.Empty;
    }
}
