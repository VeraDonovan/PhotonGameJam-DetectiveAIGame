using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class DialogueBeatDatabase
    {
        private static readonly IReadOnlyList<DialogueBeatTopicData> EmptyTopics = Array.Empty<DialogueBeatTopicData>();
        private static readonly IReadOnlyList<DialogueBeatNodeData> EmptyNodes = Array.Empty<DialogueBeatNodeData>();

        private readonly DialogueBeatSetData beatSetData;
        private readonly Dictionary<string, DialogueBeatTopicData> topicById;
        private readonly Dictionary<string, DialogueBeatNodeData> nodeById;
        private readonly Dictionary<string, List<DialogueBeatTopicData>> topicsByNpcId;
        private readonly Dictionary<string, List<DialogueBeatNodeData>> nodesByNpcId;
        private readonly Dictionary<string, List<DialogueBeatNodeData>> nodesByTopicId;

        internal DialogueBeatDatabase(
            DialogueBeatSetData beatSetData,
            Dictionary<string, DialogueBeatTopicData> topicById,
            Dictionary<string, DialogueBeatNodeData> nodeById,
            Dictionary<string, List<DialogueBeatTopicData>> topicsByNpcId,
            Dictionary<string, List<DialogueBeatNodeData>> nodesByNpcId,
            Dictionary<string, List<DialogueBeatNodeData>> nodesByTopicId)
        {
            this.beatSetData = beatSetData;
            this.topicById = topicById ?? new Dictionary<string, DialogueBeatTopicData>(StringComparer.Ordinal);
            this.nodeById = nodeById ?? new Dictionary<string, DialogueBeatNodeData>(StringComparer.Ordinal);
            this.topicsByNpcId = topicsByNpcId ?? new Dictionary<string, List<DialogueBeatTopicData>>(StringComparer.Ordinal);
            this.nodesByNpcId = nodesByNpcId ?? new Dictionary<string, List<DialogueBeatNodeData>>(StringComparer.Ordinal);
            this.nodesByTopicId = nodesByTopicId ?? new Dictionary<string, List<DialogueBeatNodeData>>(StringComparer.Ordinal);
        }

        public DialogueBeatSetData BeatSetData => beatSetData;
        public IReadOnlyDictionary<string, DialogueBeatTopicData> TopicById => topicById;
        public IReadOnlyDictionary<string, DialogueBeatNodeData> NodeById => nodeById;

        public bool TryGetTopic(string topicId, out DialogueBeatTopicData topic)
        {
            return topicById.TryGetValue(topicId, out topic);
        }

        public bool TryGetNode(string nodeId, out DialogueBeatNodeData node)
        {
            return nodeById.TryGetValue(nodeId, out node);
        }

        public IReadOnlyList<DialogueBeatTopicData> GetTopicsByNpc(string npcId)
        {
            return TryGetList(topicsByNpcId, npcId, EmptyTopics);
        }

        public IReadOnlyList<DialogueBeatNodeData> GetNodesByNpc(string npcId)
        {
            return TryGetList(nodesByNpcId, npcId, EmptyNodes);
        }

        public IReadOnlyList<DialogueBeatNodeData> GetNodesByTopic(string topicId)
        {
            return TryGetList(nodesByTopicId, topicId, EmptyNodes);
        }

        private static IReadOnlyList<T> TryGetList<T>(
            IReadOnlyDictionary<string, List<T>> source,
            string key,
            IReadOnlyList<T> emptyValue)
        {
            if (string.IsNullOrWhiteSpace(key) || !source.TryGetValue(key, out var values))
            {
                return emptyValue;
            }

            return values;
        }
    }
}
