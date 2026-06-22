using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueTurnResolution
    {
        public string NpcId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Exploration;
        public InterpretedDialogueAction InterpretedAction { get; set; } = new InterpretedDialogueAction();
        public DialogueResolutionResult ResolutionResult { get; set; } = new DialogueResolutionResult();
    }
}
