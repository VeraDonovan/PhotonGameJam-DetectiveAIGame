using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class StatementDatabaseBuilder
    {
        public static StatementDatabase Build(StatementSetData statementSetData)
        {
            if (statementSetData == null)
            {
                throw new ArgumentNullException(nameof(statementSetData));
            }

            var topicById = new Dictionary<string, StatementTopicData>(StringComparer.Ordinal);
            var statementById = new Dictionary<string, StatementEntryData>(StringComparer.Ordinal);
            var topicIdsByNpcId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var topicsByNpcId = new Dictionary<string, List<StatementTopicData>>(StringComparer.Ordinal);
            var statementIdsByNpcId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var statementsByTopicId = new Dictionary<string, List<StatementEntryData>>(StringComparer.Ordinal);
            var unlockRequirementsByStatementId = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var topic in statementSetData.statementTopics ?? new List<StatementTopicData>())
            {
                if (topic == null || string.IsNullOrWhiteSpace(topic.topicId))
                {
                    throw new InvalidOperationException("Statement topic is missing a topicId.");
                }

                if (!topicById.TryAdd(topic.topicId, topic))
                {
                    throw new InvalidOperationException($"Duplicate statement topic id '{topic.topicId}'.");
                }

                if (!string.IsNullOrWhiteSpace(topic.npcId))
                {
                    AddValue(topicIdsByNpcId, topic.npcId, topic.topicId);
                    AddValue(topicsByNpcId, topic.npcId, topic);
                }

                var statements = new List<StatementEntryData>();
                foreach (var entry in topic.entries ?? new List<StatementEntryData>())
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.statementId))
                    {
                        throw new InvalidOperationException(
                            $"Statement topic '{topic.topicId}' contains an entry without a statementId.");
                    }

                    if (!statementById.TryAdd(entry.statementId, entry))
                    {
                        throw new InvalidOperationException($"Duplicate statement id '{entry.statementId}'.");
                    }

                    statements.Add(entry);
                    unlockRequirementsByStatementId[entry.statementId] =
                        new List<string>(entry.unlockRequirements ?? new List<string>());

                    if (!string.IsNullOrWhiteSpace(topic.npcId))
                    {
                        AddValue(statementIdsByNpcId, topic.npcId, entry.statementId);
                    }
                }

                statementsByTopicId[topic.topicId] = statements;
            }

            return new StatementDatabase(
                statementSetData,
                topicById,
                statementById,
                topicIdsByNpcId,
                topicsByNpcId,
                statementIdsByNpcId,
                statementsByTopicId,
                unlockRequirementsByStatementId);
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
