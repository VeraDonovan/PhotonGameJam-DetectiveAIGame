using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class InterpretedDialogueAction
    {
        public string NpcId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Exploration;
        public string MatchedTopicId { get; set; } = string.Empty;
        public DialogueActionType ActionType { get; set; } = DialogueActionType.Unknown;
        public string PresentedEvidenceId { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public bool IsIrrelevant { get; set; }
        public string UsedBeatId { get; set; } = string.Empty;
        public string UsedStatementId { get; set; } = string.Empty;
        public string[] UsedRevealIds { get; set; } = System.Array.Empty<string>();
    }
}
