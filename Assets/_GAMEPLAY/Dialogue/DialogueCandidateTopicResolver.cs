using System;
using System.Collections.Generic;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueCandidateTopicResolver
    {
        public DialogueCandidateTopicSet Resolve(
            string npcId,
            GamePhase phase,
            DatabaseManager databaseManager,
            ProgressManager progressManager)
        {
            ValidateInputs(npcId, databaseManager, progressManager);

            var topicSet = new DialogueCandidateTopicSet
            {
                NpcId = npcId,
                Phase = phase,
            };

            var topicsById = new Dictionary<string, DialogueCandidateTopic>(StringComparer.Ordinal);

            BuildStatementTopics(npcId, phase, databaseManager.StatementDatabase, progressManager, topicsById);
            EnrichWithInterrogationLayers(npcId, phase, databaseManager.TruthDatabase, progressManager, topicsById);

            foreach (var topic in SortTopics(topicsById.Values))
            {
                topicSet.Topics.Add(topic);
            }

            return topicSet;
        }

        private static void BuildStatementTopics(
            string npcId,
            GamePhase phase,
            StatementDatabase statementDatabase,
            ProgressManager progressManager,
            Dictionary<string, DialogueCandidateTopic> topicsById)
        {
            foreach (var topicData in statementDatabase.GetTopicsByNpc(npcId))
            {
                if (topicData == null || string.IsNullOrWhiteSpace(topicData.topicId))
                {
                    continue;
                }

                var candidateTopic = GetOrCreateTopic(
                    topicsById,
                    topicData.topicId,
                    npcId,
                    topicData.displayName,
                    topicData.sortOrder,
                    isSynthetic: false);

                foreach (var entry in topicData.entries ?? new List<StatementEntryData>())
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.statementId))
                    {
                        continue;
                    }

                    if (!IsStatementEntryInPhase(entry.phase, phase))
                    {
                        continue;
                    }

                    AddUnique(candidateTopic.RelatedStatementIds, entry.statementId);

                    if (string.Equals(entry.phase, "exploration", StringComparison.OrdinalIgnoreCase))
                    {
                        candidateTopic.IsSearchPhaseTopic = true;
                    }

                    if (string.Equals(entry.phase, "interrogation", StringComparison.OrdinalIgnoreCase))
                    {
                        candidateTopic.IsInterrogationPhaseTopic = true;
                    }

                    if (progressManager.IsStatementUnlocked(entry.statementId))
                    {
                        candidateTopic.HasUnlockedStatementVersion = true;
                    }

                    AddRequirements(
                        entry.unlockRequirements,
                        progressManager,
                        candidateTopic.RequiredEvidenceIds,
                        candidateTopic.RequiredFactIds,
                        candidateTopic.RequiredStatementIds,
                        candidateTopic.RequiredInterrogationLayerIds,
                        candidateTopic.RequiredTokenIds,
                        candidateTopic.MissingRequirementIds);
                }

                FinalizeAvailability(candidateTopic, phase);
            }
        }

        private static void EnrichWithInterrogationLayers(
            string npcId,
            GamePhase phase,
            TruthDatabase truthDatabase,
            ProgressManager progressManager,
            Dictionary<string, DialogueCandidateTopic> topicsById)
        {
            if (phase != GamePhase.Interrogation)
            {
                return;
            }

            foreach (var layer in truthDatabase.GetInterrogationLayersByNpc(npcId))
            {
                if (layer == null || string.IsNullOrWhiteSpace(layer.layerId))
                {
                    continue;
                }

                if (layer.relatedStatementTopicIds == null || layer.relatedStatementTopicIds.Count == 0)
                {
                    var syntheticTopic = GetOrCreateTopic(
                        topicsById,
                        layer.layerId,
                        npcId,
                        layer.topic,
                        sortOrder: int.MaxValue,
                        isSynthetic: true);

                    ApplyLayerToTopic(syntheticTopic, layer, progressManager);
                    FinalizeAvailability(syntheticTopic, phase);
                    continue;
                }

                foreach (var topicId in layer.relatedStatementTopicIds)
                {
                    if (string.IsNullOrWhiteSpace(topicId))
                    {
                        continue;
                    }

                    var layerTopic = GetOrCreateTopic(
                        topicsById,
                        topicId,
                        npcId,
                        layer.topic,
                        sortOrder: int.MaxValue,
                        isSynthetic: false);

                    ApplyLayerToTopic(layerTopic, layer, progressManager);
                    FinalizeAvailability(layerTopic, phase);
                }
            }
        }

        private static void ApplyLayerToTopic(
            DialogueCandidateTopic topic,
            TruthInterrogationLayerData layer,
            ProgressManager progressManager)
        {
            topic.IsInterrogationPhaseTopic = true;
            AddUnique(topic.RelatedInterrogationLayerIds, layer.layerId);

            AddRequirements(
                layer.requiredEvidenceIds,
                progressManager,
                topic.RequiredEvidenceIds,
                topic.RequiredFactIds,
                topic.RequiredStatementIds,
                topic.RequiredInterrogationLayerIds,
                topic.RequiredTokenIds,
                topic.MissingRequirementIds);
        }

        private static void FinalizeAvailability(DialogueCandidateTopic topic, GamePhase phase)
        {
            var phaseSupported = phase switch
            {
                GamePhase.Exploration => topic.IsSearchPhaseTopic,
                GamePhase.Interrogation => topic.IsInterrogationPhaseTopic || topic.IsSearchPhaseTopic,
                _ => false,
            };

            if (!phaseSupported)
            {
                topic.Availability = DialogueTopicAvailability.Unavailable;
                return;
            }

            topic.Availability = DialogueTopicAvailability.Available;
        }

        private static DialogueCandidateTopic GetOrCreateTopic(
            Dictionary<string, DialogueCandidateTopic> topicsById,
            string topicId,
            string npcId,
            string displayName,
            int sortOrder,
            bool isSynthetic)
        {
            if (!topicsById.TryGetValue(topicId, out var topic))
            {
                topic = new DialogueCandidateTopic
                {
                    TopicId = topicId,
                    NpcId = npcId,
                    DisplayName = displayName ?? string.Empty,
                    SortOrder = sortOrder,
                    IsSynthetic = isSynthetic,
                };

                topicsById[topicId] = topic;
                return topic;
            }

            if (string.IsNullOrWhiteSpace(topic.DisplayName) && !string.IsNullOrWhiteSpace(displayName))
            {
                topic.DisplayName = displayName;
            }

            if (topic.SortOrder == 0 && sortOrder != 0)
            {
                topic.SortOrder = sortOrder;
            }

            topic.IsSynthetic = topic.IsSynthetic && isSynthetic;
            return topic;
        }

        private static void AddRequirements(
            IReadOnlyList<string> requirementIds,
            ProgressManager progressManager,
            List<string> requiredEvidenceIds,
            List<string> requiredFactIds,
            List<string> requiredStatementIds,
            List<string> requiredInterrogationLayerIds,
            List<string> requiredTokenIds,
            List<string> missingRequirementIds)
        {
            foreach (var requirementId in requirementIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(requirementId))
                {
                    continue;
                }

                var isSatisfied = false;
                if (progressManager.EvidenceCollectedById.ContainsKey(requirementId))
                {
                    AddUnique(requiredEvidenceIds, requirementId);
                    isSatisfied = progressManager.IsEvidenceCollected(requirementId);
                }
                else if (progressManager.FactUnlockedById.ContainsKey(requirementId))
                {
                    AddUnique(requiredFactIds, requirementId);
                    isSatisfied = progressManager.IsFactUnlocked(requirementId);
                }
                else if (progressManager.StatementUnlockedById.ContainsKey(requirementId))
                {
                    AddUnique(requiredStatementIds, requirementId);
                    isSatisfied = progressManager.IsStatementUnlocked(requirementId);
                }
                else if (progressManager.IsProgressTokenUnlocked(requirementId) || progressManager.RuntimeState.ProgressTokenById.ContainsKey(requirementId))
                {
                    AddUnique(requiredTokenIds, requirementId);
                    isSatisfied = progressManager.IsProgressTokenUnlocked(requirementId);
                }
                else if (progressManager.IsInterrogationLayerUnlocked(requirementId) ||
                         progressManager.InterrogationLayerUnlockedById.ContainsKey(requirementId))
                {
                    AddUnique(requiredInterrogationLayerIds, requirementId);
                    isSatisfied = progressManager.IsInterrogationLayerUnlocked(requirementId);
                }
                else
                {
                    AddUnique(requiredTokenIds, requirementId);
                }

                if (!isSatisfied)
                {
                    AddUnique(missingRequirementIds, requirementId);
                }
            }
        }

        private static bool IsStatementEntryInPhase(string entryPhase, GamePhase phase)
        {
            if (string.IsNullOrWhiteSpace(entryPhase))
            {
                return false;
            }

            if (phase == GamePhase.Exploration)
            {
                return string.Equals(entryPhase, "exploration", StringComparison.OrdinalIgnoreCase);
            }

            if (phase == GamePhase.Interrogation)
            {
                return string.Equals(entryPhase, "exploration", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(entryPhase, "interrogation", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static IEnumerable<DialogueCandidateTopic> SortTopics(IEnumerable<DialogueCandidateTopic> topics)
        {
            var topicList = new List<DialogueCandidateTopic>(topics);
            topicList.Sort(CompareTopics);
            return topicList;
        }

        private static int CompareTopics(DialogueCandidateTopic left, DialogueCandidateTopic right)
        {
            var sortOrderComparison = left.SortOrder.CompareTo(right.SortOrder);
            if (sortOrderComparison != 0)
            {
                return sortOrderComparison;
            }

            return string.Compare(left.TopicId, right.TopicId, StringComparison.Ordinal);
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || values.Contains(value))
            {
                return;
            }

            values.Add(value);
        }

        private static void ValidateInputs(
            string npcId,
            DatabaseManager databaseManager,
            ProgressManager progressManager)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                throw new ArgumentException("DialogueCandidateTopicResolver requires a non-empty npcId.", nameof(npcId));
            }

            if (databaseManager == null)
            {
                throw new ArgumentNullException(nameof(databaseManager));
            }

            if (progressManager == null)
            {
                throw new ArgumentNullException(nameof(progressManager));
            }

            if (databaseManager.StatementDatabase == null)
            {
                throw new InvalidOperationException("DialogueCandidateTopicResolver requires StatementDatabase.");
            }

            if (databaseManager.TruthDatabase == null)
            {
                throw new InvalidOperationException("DialogueCandidateTopicResolver requires TruthDatabase.");
            }
        }
    }
}
