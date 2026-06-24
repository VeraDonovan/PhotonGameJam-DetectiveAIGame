using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public interface IDialogueApiContextAssembler
    {
        DialogueApiPromptContext Assemble(
            RawDialogueInput rawInput,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcRuntimeManager npcRuntimeManager,
            DialogueConversationSession conversationSession,
            DialoguePromptMode mode);
    }
}
