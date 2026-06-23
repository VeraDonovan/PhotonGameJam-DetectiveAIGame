using System;
using System.Collections.Generic;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueConversationHistorySelector : IDialogueConversationHistorySelector
    {
        public IReadOnlyList<DialogueConversationExchange> SelectForApi(
            DialogueConversationSession session,
            DialoguePromptMode mode)
        {
            if (session == null)
            {
                return Array.Empty<DialogueConversationExchange>();
            }

            var exchanges = session.Exchanges;
            if (exchanges == null || exchanges.Count == 0)
            {
                return Array.Empty<DialogueConversationExchange>();
            }

            int maxCount = mode switch
            {
                DialoguePromptMode.Opening => DialogueConversationConfig.OpeningVerbatimExchangeCount,
                DialoguePromptMode.Turn => DialogueConversationConfig.RecentVerbatimExchangeCount,
                _ => DialogueConversationConfig.RecentVerbatimExchangeCount,
            };

            if (maxCount <= 0)
            {
                return Array.Empty<DialogueConversationExchange>();
            }

            int startIndex = exchanges.Count > maxCount
                ? exchanges.Count - maxCount
                : 0;

            var selected = new List<DialogueConversationExchange>();
            for (int i = startIndex; i < exchanges.Count; i++)
            {
                var exchange = exchanges[i];
                if (exchange == null)
                {
                    continue;
                }

                selected.Add(new DialogueConversationExchange
                {
                    PlayerText = exchange.PlayerText,
                    NpcText = exchange.NpcText,
                });
            }

            return selected;
        }
    }
}
