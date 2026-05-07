using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class NpcDialogueRuntimeState
    {
        public const int MaxAnnoyance = 100;
        public const int MinAnnoyance = 0;
        public const int MaxPressure = 100;
        public const int MinPressure = 0;

        public NpcDialogueRuntimeState(string npcId)
        {
            NpcId = npcId;
        }

        public string NpcId { get; }
        public int Annoyance { get; private set; }
        public int Pressure { get; private set; }
        public string LastMatchedTopicId { get; private set; } = string.Empty;
        public string CurrentInterrogationLayerId { get; private set; } = string.Empty;
        public HashSet<string> DiscussedTopicIds { get; } = new HashSet<string>();
        public HashSet<string> ResolvedTopicIds { get; } = new HashSet<string>();

        public void SetAnnoyance(int value)
        {
            Annoyance = Clamp(value, MinAnnoyance, MaxAnnoyance);
        }

        public void AddAnnoyance(int delta)
        {
            SetAnnoyance(Annoyance + delta);
        }

        public void ResetAnnoyance()
        {
            Annoyance = MinAnnoyance;
        }

        public void SetPressure(int value)
        {
            Pressure = Clamp(value, MinPressure, MaxPressure);
        }

        public void AddPressure(int delta)
        {
            SetPressure(Pressure + delta);
        }

        public void ResetPressure()
        {
            Pressure = MinPressure;
            CurrentInterrogationLayerId = string.Empty;
        }

        public void MarkTopicDiscussed(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
            {
                return;
            }

            LastMatchedTopicId = topicId;
            DiscussedTopicIds.Add(topicId);
        }

        public void MarkTopicResolved(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
            {
                return;
            }

            MarkTopicDiscussed(topicId);
            ResolvedTopicIds.Add(topicId);
        }

        public void SetCurrentInterrogationLayer(string layerId)
        {
            CurrentInterrogationLayerId = layerId ?? string.Empty;
        }

        public void ResetDialogueProgress()
        {
            ResetAnnoyance();
            ResetPressure();
            LastMatchedTopicId = string.Empty;
            DiscussedTopicIds.Clear();
            ResolvedTopicIds.Clear();
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
