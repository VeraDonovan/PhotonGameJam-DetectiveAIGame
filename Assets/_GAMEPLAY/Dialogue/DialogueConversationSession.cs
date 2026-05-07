using System;
using System.Collections.Generic;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueConversationSession
    {
        public const int MaxExchangeCount = 6;

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

        public void AddExchange(string playerText, string npcText)
        {
            exchanges.Add(new DialogueConversationExchange
            {
                PlayerText = playerText ?? string.Empty,
                NpcText = npcText ?? string.Empty,
            });

            while (exchanges.Count > MaxExchangeCount)
            {
                exchanges.RemoveAt(0);
            }
        }

        public void Clear()
        {
            exchanges.Clear();
        }
    }

    public sealed class DialogueConversationExchange
    {
        public string PlayerText { get; set; } = string.Empty;
        public string NpcText { get; set; } = string.Empty;
    }
}
