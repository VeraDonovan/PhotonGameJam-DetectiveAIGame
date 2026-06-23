namespace DetectiveGame.Gameplay.Dialogue
{
    public interface IDialoguePromptBuilder
    {
        DialoguePromptMessages Build(DialogueApiPromptContext context, DialoguePromptSections promptSections);

        DialoguePromptMessages BuildOpening(DialogueApiPromptContext context);
    }
}
