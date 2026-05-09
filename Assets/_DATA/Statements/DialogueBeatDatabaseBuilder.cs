using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class DialogueBeatDatabaseBuilder
    {
        public static DialogueBeatDatabase Build(DialogueBeatSetData beatSetData)
        {
            if (beatSetData == null)
            {
                throw new ArgumentNullException(nameof(beatSetData));
            }

            var topicById = new Dictionary<string, DialogueBeatTopicData>(StringComparer.Ordinal);
            var nodeById = new Dictionary<string, DialogueBeatNodeData>(StringComparer.Ordinal);
            var topicsByNpcId = new Dictionary<string, List<DialogueBeatTopicData>>(StringComparer.Ordinal);
            var nodesByNpcId = new Dictionary<string, List<DialogueBeatNodeData>>(StringComparer.Ordinal);
            var nodesByTopicId = new Dictionary<string, List<DialogueBeatNodeData>>(StringComparer.Ordinal);

            foreach (var topic in beatSetData.topics ?? new List<DialogueBeatTopicData>())
            {
                if (topic == null || string.IsNullOrWhiteSpace(topic.topicId))
                {
                    throw new InvalidOperationException("Dialogue beat topic is missing a topicId.");
                }

                if (!topicById.TryAdd(topic.topicId, topic))
                {
                    throw new InvalidOperationException($"Duplicate dialogue beat topic id '{topic.topicId}'.");
                }

                if (!string.IsNullOrWhiteSpace(topic.npcId))
                {
                    AddValue(topicsByNpcId, topic.npcId, topic);
                }

                var topicNodes = new List<DialogueBeatNodeData>();
                foreach (var node in topic.nodes ?? new List<DialogueBeatNodeData>())
                {
                    if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                    {
                        throw new InvalidOperationException(
                            $"Dialogue beat topic '{topic.topicId}' contains a node without a nodeId.");
                    }

                    if (!nodeById.TryAdd(node.nodeId, node))
                    {
                        throw new InvalidOperationException($"Duplicate dialogue beat node id '{node.nodeId}'.");
                    }

                    topicNodes.Add(node);
                    if (!string.IsNullOrWhiteSpace(topic.npcId))
                    {
                        AddValue(nodesByNpcId, topic.npcId, node);
                    }
                }

                nodesByTopicId[topic.topicId] = topicNodes;
            }

            return new DialogueBeatDatabase(
                beatSetData,
                topicById,
                nodeById,
                topicsByNpcId,
                nodesByNpcId,
                nodesByTopicId);
        }

        private static void AddValue<T>(Dictionary<string, List<T>> source, string key, T value)
        {
            if (!source.TryGetValue(key, out var values))
            {
                values = new List<T>();
                source[key] = values;
            }

            values.Add(value);
        }
    }
}
