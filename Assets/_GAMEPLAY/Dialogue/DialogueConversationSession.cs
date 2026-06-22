using System;
using System.Collections.Generic;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueConversationSession
    {
        private readonly List<DialogueConversationExchange> exchanges = new List<DialogueConversationExchange>();

        public DialogueConversationSession(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                throw new ArgumentException("DialogueConversationSession requires a non-empty npcId.", nameof(npcId));
            }

            NpcId = npcId;
        }

        public string NpcId { get; }
        public IReadOnlyList<DialogueConversationExchange> Exchanges => exchanges;

        public string ActiveTurnSummary { get; private set; } = string.Empty;
        public string PendingTurnSummary { get; private set; } = string.Empty;
        public string ActiveOpeningSummary { get; private set; } = string.Empty;
        public string PendingOpeningSummary { get; private set; } = string.Empty;
        public int SummarizedExchangeCount { get; private set; }

        public void AddExchange(string playerText, string npcText)
        {
            exchanges.Add(new DialogueConversationExchange
            {
                PlayerText = playerText ?? string.Empty,
                NpcText = npcText ?? string.Empty,
            });
        }

        public void PromotePendingTurnSummaryIfAny()
        {
            if (string.IsNullOrWhiteSpace(PendingTurnSummary))
            {
                return;
            }

            ActiveTurnSummary = PendingTurnSummary;
            PendingTurnSummary = string.Empty;
        }

        public void PromotePendingOpeningSummaryIfAny()
        {
            if (string.IsNullOrWhiteSpace(PendingOpeningSummary))
            {
                return;
            }

            ActiveOpeningSummary = PendingOpeningSummary;
            PendingOpeningSummary = string.Empty;
        }

        public void SetPendingTurnSummary(string summary)
        {
            PendingTurnSummary = summary ?? string.Empty;
        }

        public void SetPendingOpeningSummary(string summary)
        {
            PendingOpeningSummary = summary ?? string.Empty;
        }

        public void AdvanceSummarizedExchangeCount(int batchSize)
        {
            if (batchSize <= 0)
            {
                return;
            }

            SummarizedExchangeCount += batchSize;
        }

        public void Clear()
        {
            exchanges.Clear();
            ActiveTurnSummary = string.Empty;
            PendingTurnSummary = string.Empty;
            ActiveOpeningSummary = string.Empty;
            PendingOpeningSummary = string.Empty;
            SummarizedExchangeCount = 0;
        }
    }

    public sealed class DialogueConversationExchange
    {
        public string PlayerText { get; set; } = string.Empty;
        public string NpcText { get; set; } = string.Empty;
    }
}
