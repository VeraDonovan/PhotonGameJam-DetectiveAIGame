using System;
using System.Collections.Generic;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueTurnResolver
    {
        public const int DefaultAnnoyanceGain = 20;

        private readonly DialogueCandidateTopicResolver candidateTopicResolver;

        public DialogueTurnResolver()
            : this(new DialogueCandidateTopicResolver())
        {
        }

        public DialogueTurnResolver(DialogueCandidateTopicResolver candidateTopicResolver)
        {
            this.candidateTopicResolver = candidateTopicResolver ??
                                          throw new ArgumentNullException(nameof(candidateTopicResolver));
        }

        public DialogueTurnContext Resolve(
            RawDialogueInput rawInput,
            InterpretedDialogueAction interpretedAction,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcRuntimeManager npcRuntimeManager,
            DialogueConversationSession conversationSession = null)
        {
            ValidateInputs(rawInput, interpretedAction, databaseManager, progressManager, npcRuntimeManager);

            var candidateTopics = candidateTopicResolver.Resolve(
                rawInput.NpcId,
                rawInput.Phase,
                databaseManager,
                progressManager);

            var npcState = npcRuntimeManager.GetOrCreateDialogueState(rawInput.NpcId);
            var result = ResolveGameplayShell(
                interpretedAction,
                candidateTopics,
                databaseManager,
                progressManager,
                npcState,
                npcRuntimeManager);

            var context = new DialogueTurnContext
            {
                NpcId = rawInput.NpcId,
                Phase = rawInput.Phase,
                RawInput = rawInput,
                CandidateTopics = candidateTopics,
                InterpretedAction = interpretedAction,
                ResolutionResult = result,
                Annoyance = npcState.Annoyance,
                Pressure = rawInput.Phase == GamePhase.Interrogation ? npcState.Pressure : 0,
                CurrentInterrogationLayerId = rawInput.Phase == GamePhase.Interrogation
                    ? npcState.CurrentInterrogationLayerId
                    : string.Empty,
            };

            if (conversationSession != null && string.Equals(conversationSession.NpcId, rawInput.NpcId, StringComparison.Ordinal))
            {
                foreach (var exchange in conversationSession.Exchanges)
                {
                    context.RecentConversation.Add(new DialogueConversationExchange
                    {
                        PlayerText = exchange.PlayerText,
                        NpcText = exchange.NpcText,
                    });
                }
            }

            return context;
        }

        private static DialogueResolutionResult ResolveGameplayShell(
            InterpretedDialogueAction interpretedAction,
            DialogueCandidateTopicSet candidateTopics,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcDialogueRuntimeState npcState,
            NpcRuntimeManager npcRuntimeManager)
        {
            var result = new DialogueResolutionResult
            {
                NewAnnoyance = npcState.Annoyance,
                NewPressure = npcState.Pressure,
            };

            if (interpretedAction.IsIrrelevant || string.IsNullOrWhiteSpace(interpretedAction.MatchedTopicId))
            {
                ApplyAnnoyance(result, npcState, npcRuntimeManager, interpretedAction.NpcId, "irrelevant_input");
                return result;
            }

            var matchedTopic = FindTopic(candidateTopics, interpretedAction.MatchedTopicId);
            if (matchedTopic == null)
            {
                ApplyAnnoyance(result, npcState, npcRuntimeManager, interpretedAction.NpcId, "topic_outside_candidate_set");
                return result;
            }

            if (matchedTopic.Availability != DialogueTopicAvailability.Available)
            {
                ApplyAnnoyance(result, npcState, npcRuntimeManager, interpretedAction.NpcId, "topic_not_available");
                return result;
            }

            if (npcState.ResolvedTopicIds.Contains(interpretedAction.MatchedTopicId))
            {
                ApplyAnnoyance(result, npcState, npcRuntimeManager, interpretedAction.NpcId, "resolved_topic_repeated");
                return result;
            }

            npcRuntimeManager.MarkTopicDiscussed(interpretedAction.NpcId, interpretedAction.MatchedTopicId);

            if (interpretedAction.Phase == GamePhase.Exploration)
            {
                ResolveExplorationProgress(interpretedAction, databaseManager, progressManager, result);
                return result;
            }

            result.ResolutionType = DialogueResolutionType.NoProgress;
            return result;
        }

        private static void ResolveExplorationProgress(
            InterpretedDialogueAction interpretedAction,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            DialogueResolutionResult result)
        {
            var statement = FindNextUnlockableExplorationStatement(
                interpretedAction.MatchedTopicId,
                databaseManager.StatementDatabase,
                progressManager);

            if (statement == null)
            {
                result.ResolutionType = DialogueResolutionType.NoProgress;
                return;
            }

            var unlockedFactsBefore = new HashSet<string>(progressManager.UnlockedFactIds);
            var unlockedStatementsBefore = new HashSet<string>(progressManager.UnlockedStatementIds);

            if (!progressManager.UnlockStatement(statement.statementId))
            {
                result.ResolutionType = DialogueResolutionType.NoProgress;
                return;
            }

            AddNewUnlockedIds(unlockedStatementsBefore, progressManager.UnlockedStatementIds, result.UnlockedStatementIds);
            AddNewUnlockedIds(unlockedFactsBefore, progressManager.UnlockedFactIds, result.UnlockedFactIds);
            result.ResolutionType = result.UnlockedFactIds.Count > 0
                ? DialogueResolutionType.CompositeProgress
                : DialogueResolutionType.StatementUnlocked;
        }

        private static StatementEntryData FindNextUnlockableExplorationStatement(
            string topicId,
            StatementDatabase statementDatabase,
            ProgressManager progressManager)
        {
            foreach (var statement in statementDatabase.GetStatementsByTopic(topicId))
            {
                if (statement == null ||
                    !string.Equals(statement.phase, "exploration", StringComparison.OrdinalIgnoreCase) ||
                    progressManager.IsStatementUnlocked(statement.statementId))
                {
                    continue;
                }

                if (AreRequirementsSatisfied(statement.unlockRequirements, progressManager))
                {
                    return statement;
                }
            }

            return null;
        }

        private static bool AreRequirementsSatisfied(
            IReadOnlyList<string> requirementIds,
            ProgressManager progressManager)
        {
            foreach (var requirementId in requirementIds ?? Array.Empty<string>())
            {
                if (!IsRequirementSatisfied(requirementId, progressManager))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsRequirementSatisfied(string requirementId, ProgressManager progressManager)
        {
            return progressManager.IsEvidenceCollected(requirementId) ||
                   progressManager.IsFactUnlocked(requirementId) ||
                   progressManager.IsStatementUnlocked(requirementId) ||
                   progressManager.IsInterrogationLayerUnlocked(requirementId) ||
                   progressManager.IsProgressTokenUnlocked(requirementId);
        }

        private static void AddNewUnlockedIds(
            HashSet<string> before,
            IEnumerable<string> after,
            List<string> destination)
        {
            foreach (var id in after)
            {
                if (!before.Contains(id))
                {
                    destination.Add(id);
                }
            }
        }

        private static DialogueCandidateTopic FindTopic(
            DialogueCandidateTopicSet candidateTopics,
            string topicId)
        {
            foreach (var topic in candidateTopics.Topics)
            {
                if (string.Equals(topic.TopicId, topicId, StringComparison.Ordinal))
                {
                    return topic;
                }
            }

            return null;
        }

        private static void ApplyAnnoyance(
            DialogueResolutionResult result,
            NpcDialogueRuntimeState npcState,
            NpcRuntimeManager npcRuntimeManager,
            string npcId,
            string punishReason)
        {
            var oldAnnoyance = npcState.Annoyance;
            var newAnnoyance = npcRuntimeManager.AddAnnoyance(npcId, DefaultAnnoyanceGain);

            result.ResolutionType = DialogueResolutionType.Punished;
            result.AnnoyanceDelta = newAnnoyance - oldAnnoyance;
            result.NewAnnoyance = newAnnoyance;
            result.NewPressure = npcState.Pressure;
            result.PunishReason = punishReason;
        }

        private static void ValidateInputs(
            RawDialogueInput rawInput,
            InterpretedDialogueAction interpretedAction,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcRuntimeManager npcRuntimeManager)
        {
            if (rawInput == null)
            {
                throw new ArgumentNullException(nameof(rawInput));
            }

            if (interpretedAction == null)
            {
                throw new ArgumentNullException(nameof(interpretedAction));
            }

            if (databaseManager == null)
            {
                throw new ArgumentNullException(nameof(databaseManager));
            }

            if (progressManager == null)
            {
                throw new ArgumentNullException(nameof(progressManager));
            }

            if (npcRuntimeManager == null)
            {
                throw new ArgumentNullException(nameof(npcRuntimeManager));
            }

            if (string.IsNullOrWhiteSpace(rawInput.NpcId))
            {
                throw new ArgumentException("DialogueTurnResolver requires RawDialogueInput.NpcId.", nameof(rawInput));
            }

            if (!string.Equals(rawInput.NpcId, interpretedAction.NpcId, StringComparison.Ordinal))
            {
                throw new ArgumentException("DialogueTurnResolver requires raw input and interpreted action to use the same npcId.");
            }

            if (rawInput.Phase != interpretedAction.Phase)
            {
                throw new ArgumentException("DialogueTurnResolver requires raw input and interpreted action to use the same phase.");
            }
        }
    }
}
