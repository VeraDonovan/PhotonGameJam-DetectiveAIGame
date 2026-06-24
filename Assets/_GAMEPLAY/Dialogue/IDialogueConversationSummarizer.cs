using System.Collections;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public interface IDialogueConversationSummarizer
    {
        IEnumerator MaybeSummarizeTurnOverflow(DialogueConversationSession session);

        IEnumerator MaybeUpdateOpeningSummary(
            DialogueConversationSession session,
            DialogueConversationExchange latestExchange,
            GamePhase phase,
            int annoyance);
    }
}
