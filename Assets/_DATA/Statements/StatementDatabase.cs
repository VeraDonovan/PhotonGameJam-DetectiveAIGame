using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class StatementDatabase
    {
        private static readonly IReadOnlyList<string> EmptyIds = Array.Empty<string>();
        private static readonly IReadOnlyList<StatementTopicData> EmptyTopics = Array.Empty<StatementTopicData>();
        private static readonly IReadOnlyList<StatementEntryData> EmptyStatements = Array.Empty<StatementEntryData>();

        private readonly StatementSetData statementSetData;
        private readonly Dictionary<string, StatementTopicData> topicById;
        private readonly Dictionary<string, StatementEntryData> statementById;
        private readonly Dictionary<string, List<string>> topicIdsByNpcId;
        private readonly Dictionary<string, List<StatementTopicData>> topicsByNpcId;
        private readonly Dictionary<string, List<string>> statementIdsByNpcId;
        private readonly Dictionary<string, List<StatementEntryData>> statementsByTopicId;
        private readonly Dictionary<string, List<string>> unlockRequirementsByStatementId;

        internal StatementDatabase(
            StatementSetData statementSetData,
            Dictionary<string, StatementTopicData> topicById,
            Dictionary<string, StatementEntryData> statementById,
            Dictionary<string, List<string>> topicIdsByNpcId,
            Dictionary<string, List<StatementTopicData>> topicsByNpcId,
            Dictionary<string, List<string>> statementIdsByNpcId,
            Dictionary<string, List<StatementEntryData>> statementsByTopicId,
            Dictionary<string, List<string>> unlockRequirementsByStatementId)
        {
            this.statementSetData = statementSetData;
            this.topicById = topicById ?? new Dictionary<string, StatementTopicData>(StringComparer.Ordinal);
            this.statementById = statementById ?? new Dictionary<string, StatementEntryData>(StringComparer.Ordinal);
            this.topicIdsByNpcId = topicIdsByNpcId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.topicsByNpcId = topicsByNpcId ?? new Dictionary<string, List<StatementTopicData>>(StringComparer.Ordinal);
            this.statementIdsByNpcId = statementIdsByNpcId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.statementsByTopicId = statementsByTopicId ?? new Dictionary<string, List<StatementEntryData>>(StringComparer.Ordinal);
            this.unlockRequirementsByStatementId = unlockRequirementsByStatementId ??
                                                  new Dictionary<string, List<string>>(StringComparer.Ordinal);
        }

        public StatementSetData StatementSetData => statementSetData;
        public IReadOnlyDictionary<string, StatementTopicData> TopicById => topicById;
        public IReadOnlyDictionary<string, StatementEntryData> StatementById => statementById;
        public IReadOnlyDictionary<string, List<string>> TopicIdsByNpcId => topicIdsByNpcId;
        public IReadOnlyDictionary<string, List<StatementTopicData>> TopicsByNpcId => topicsByNpcId;
        public IReadOnlyDictionary<string, List<string>> StatementIdsByNpcId => statementIdsByNpcId;
        public IReadOnlyDictionary<string, List<StatementEntryData>> StatementsByTopicId => statementsByTopicId;
        public IReadOnlyDictionary<string, List<string>> UnlockRequirementsByStatementId => unlockRequirementsByStatementId;

        public bool TryGetTopic(string topicId, out StatementTopicData topic)
        {
            return topicById.TryGetValue(topicId, out topic);
        }

        public bool TryGetStatement(string statementId, out StatementEntryData statement)
        {
            return statementById.TryGetValue(statementId, out statement);
        }

        public IReadOnlyList<StatementTopicData> GetTopicsByNpc(string npcId)
        {
            return TryGetList(topicsByNpcId, npcId, EmptyTopics);
        }

        public IReadOnlyList<string> GetTopicIdsByNpc(string npcId)
        {
            return TryGetList(topicIdsByNpcId, npcId, EmptyIds);
        }

        public IReadOnlyList<string> GetStatementIdsByNpc(string npcId)
        {
            return TryGetList(statementIdsByNpcId, npcId, EmptyIds);
        }

        public IReadOnlyList<StatementEntryData> GetStatementsByTopic(string topicId)
        {
            return TryGetList(statementsByTopicId, topicId, EmptyStatements);
        }

        public IReadOnlyList<string> GetUnlockRequirements(string statementId)
        {
            return TryGetList(unlockRequirementsByStatementId, statementId, EmptyIds);
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
