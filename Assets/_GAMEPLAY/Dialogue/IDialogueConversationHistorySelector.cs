using System.Collections.Generic;

namespace DetectiveGame.Gameplay.Dialogue
{
    public interface IDialogueConversationHistorySelector
    {
        IReadOnlyList<DialogueConversationExchange> SelectForApi(
            DialogueConversationSession session,
            DialoguePromptMode mode);
    }
}
