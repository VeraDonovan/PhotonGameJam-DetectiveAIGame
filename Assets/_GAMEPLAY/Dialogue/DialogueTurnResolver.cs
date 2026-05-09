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

        public DialogueTurnContext BuildPromptContext(
            RawDialogueInput rawInput,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcRuntimeManager npcRuntimeManager,
            DialogueConversationSession conversationSession = null)
        {
            ValidatePromptInputs(rawInput, databaseManager, progressManager, npcRuntimeManager);

            var candidateTopics = candidateTopicResolver.Resolve(
                rawInput.NpcId,
                rawInput.Phase,
                databaseManager,
                progressManager);

            var npcState = npcRuntimeManager.GetOrCreateDialogueState(rawInput.NpcId);
            var context = CreateContext(
                rawInput,
                new InterpretedDialogueAction
                {
                    NpcId = rawInput.NpcId,
                    Phase = rawInput.Phase,
                    PresentedEvidenceId = rawInput.PresentedEvidenceId,
                },
                candidateTopics,
                new DialogueResolutionResult
                {
                    NewAnnoyance = npcState.Annoyance,
                    NewPressure = npcState.Pressure,
                },
                npcState,
                databaseManager,
                progressManager,
                conversationSession);

            return context;
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
                rawInput,
                interpretedAction,
                candidateTopics,
                databaseManager,
                progressManager,
                npcState,
                npcRuntimeManager);

            return CreateContext(
                rawInput,
                interpretedAction,
                candidateTopics,
                result,
                npcState,
                databaseManager,
                progressManager,
                conversationSession);
        }

        private static DialogueTurnContext CreateContext(
            RawDialogueInput rawInput,
            InterpretedDialogueAction interpretedAction,
            DialogueCandidateTopicSet candidateTopics,
            DialogueResolutionResult result,
            NpcDialogueRuntimeState npcState,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            DialogueConversationSession conversationSession)
        {
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

            PopulateDatabaseContext(context, databaseManager, progressManager);

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

        private static void PopulateDatabaseContext(
            DialogueTurnContext context,
            DatabaseManager databaseManager,
            ProgressManager progressManager)
        {
            if (databaseManager.NpcDatabase.TryGetNpc(context.NpcId, out var npcProfile))
            {
                context.NpcPublicProfile = npcProfile;
            }

            if (databaseManager.NpcAiProfileDatabase.TryGetProfile(context.NpcId, out var aiProfile) &&
                aiProfile != null)
            {
                context.NpcAiProfileRawJson = aiProfile.rawJson ?? string.Empty;
            }

            PopulateRelevantFacts(context, databaseManager, progressManager);
            PopulateRelevantStatements(context, databaseManager, progressManager);
            PopulateAllowedInterrogationLayers(context, databaseManager, progressManager);
            PopulateMatchedTopicWithholdContext(context);
        }

        private static void PopulateRelevantFacts(
            DialogueTurnContext context,
            DatabaseManager databaseManager,
            ProgressManager progressManager)
        {
            foreach (var factId in databaseManager.FactDatabase.GetFactIdsByNpc(context.NpcId))
            {
                if (progressManager.IsFactUnlocked(factId))
                {
                    AddUnique(context.RelevantUnlockedFactIds, factId);
                }
            }
        }

        private static void PopulateRelevantStatements(
            DialogueTurnContext context,
            DatabaseManager databaseManager,
            ProgressManager progressManager)
        {
            foreach (var statementId in databaseManager.StatementDatabase.GetStatementIdsByNpc(context.NpcId))
            {
                if (!progressManager.IsStatementUnlocked(statementId))
                {
                    continue;
                }

                AddUnique(context.RelevantUnlockedStatementIds, statementId);
                if (databaseManager.StatementDatabase.TryGetStatement(statementId, out var statement) &&
                    statement != null)
                {
                    context.RelevantUnlockedStatements.Add(CreateStatementContext(statement, isUnlockable: true));
                }
            }
        }

        private static void PopulateAllowedInterrogationLayers(
            DialogueTurnContext context,
            DatabaseManager databaseManager,
            ProgressManager progressManager)
        {
            foreach (var layer in databaseManager.TruthDatabase.GetInterrogationLayersByNpc(context.NpcId))
            {
                if (layer == null || !progressManager.IsInterrogationLayerUnlocked(layer.layerId))
                {
                    continue;
                }

                AddUnique(context.RelevantUnlockedLayerIds, layer.layerId);
                AddUnique(context.AllowedRevealIds, layer.layerId);
                context.AllowedInterrogationLayers.Add(layer);
            }
        }

        private static void PopulateMatchedTopicWithholdContext(DialogueTurnContext context)
        {
            var matchedTopic = FindTopic(context.CandidateTopics, context.InterpretedAction.MatchedTopicId);
            if (matchedTopic == null)
            {
                return;
            }

            foreach (var missingRequirementId in matchedTopic.MissingRequirementIds)
            {
                AddUnique(context.MustWithholdIds, missingRequirementId);
            }
        }

        private static DialogueResolutionResult ResolveGameplayShell(
            RawDialogueInput rawInput,
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
                ResolveExplorationProgress(
                    rawInput,
                    interpretedAction,
                    matchedTopic,
                    databaseManager,
                    progressManager,
                    result);
                return result;
            }

            result.ResolutionType = DialogueResolutionType.NoProgress;
            ValidateAiResponseUsage(interpretedAction, matchedTopic, progressManager, result);
            return result;
        }

        private static void ResolveExplorationProgress(
            RawDialogueInput rawInput,
            InterpretedDialogueAction interpretedAction,
            DialogueCandidateTopic matchedTopic,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            DialogueResolutionResult result)
        {
            var statement = FindNextUnlockableExplorationStatement(
                interpretedAction.MatchedTopicId,
                databaseManager.StatementDatabase,
                progressManager,
                rawInput.PresentedEvidenceId);

            if (statement == null)
            {
                result.ResolutionType = DialogueResolutionType.NoProgress;
                ValidateAiResponseUsage(interpretedAction, matchedTopic, progressManager, result);
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

            ValidateAiResponseUsage(interpretedAction, matchedTopic, progressManager, result);
        }

        private static StatementEntryData FindNextUnlockableExplorationStatement(
            string topicId,
            StatementDatabase statementDatabase,
            ProgressManager progressManager,
            string presentedEvidenceId)
        {
            foreach (var statement in statementDatabase.GetStatementsByTopic(topicId))
            {
                if (statement == null ||
                    !string.Equals(statement.phase, "exploration", StringComparison.OrdinalIgnoreCase) ||
                    progressManager.IsStatementUnlocked(statement.statementId))
                {
                    continue;
                }

                if (AreStatementRequirementsSatisfied(
                        statement.unlockRequirements,
                        progressManager,
                        presentedEvidenceId))
                {
                    return statement;
                }
            }

            return null;
        }

        private static bool AreStatementRequirementsSatisfied(
            IReadOnlyList<string> requirementIds,
            ProgressManager progressManager,
            string presentedEvidenceId)
        {
            var hasEvidenceRequirement = false;
            var presentedRequiredEvidence = false;

            foreach (var requirementId in requirementIds ?? Array.Empty<string>())
            {
                if (progressManager.EvidenceCollectedById.ContainsKey(requirementId))
                {
                    hasEvidenceRequirement = true;
                    if (!progressManager.IsEvidenceCollected(requirementId))
                    {
                        return false;
                    }

                    if (string.Equals(requirementId, presentedEvidenceId, StringComparison.Ordinal))
                    {
                        presentedRequiredEvidence = true;
                    }

                    continue;
                }

                if (!IsRequirementSatisfied(requirementId, progressManager))
                {
                    return false;
                }
            }

            return !hasEvidenceRequirement || presentedRequiredEvidence;
        }

        private static void ValidateAiResponseUsage(
            InterpretedDialogueAction interpretedAction,
            DialogueCandidateTopic matchedTopic,
            ProgressManager progressManager,
            DialogueResolutionResult result)
        {
            if (!IsUsedStatementAllowed(interpretedAction.UsedStatementId, matchedTopic, result))
            {
                RejectAiResponse(result, "used_statement_not_allowed");
                return;
            }

            if (!AreUsedRevealsAllowed(interpretedAction.UsedRevealIds, progressManager, result))
            {
                RejectAiResponse(result, "used_reveal_not_allowed");
            }
        }

        private static bool IsUsedStatementAllowed(
            string usedStatementId,
            DialogueCandidateTopic matchedTopic,
            DialogueResolutionResult result)
        {
            if (string.IsNullOrWhiteSpace(usedStatementId))
            {
                return true;
            }

            if (!matchedTopic.RelatedStatementIds.Contains(usedStatementId))
            {
                return false;
            }

            foreach (var statement in matchedTopic.RelatedStatements)
            {
                if (!string.Equals(statement.StatementId, usedStatementId, StringComparison.Ordinal))
                {
                    continue;
                }

                return statement.IsUnlocked || result.UnlockedStatementIds.Contains(usedStatementId);
            }

            return false;
        }

        private static bool AreUsedRevealsAllowed(
            IReadOnlyList<string> usedRevealIds,
            ProgressManager progressManager,
            DialogueResolutionResult result)
        {
            foreach (var revealId in usedRevealIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(revealId))
                {
                    continue;
                }

                if (!progressManager.IsInterrogationLayerUnlocked(revealId) &&
                    !result.UnlockedLayerIds.Contains(revealId))
                {
                    return false;
                }
            }

            return true;
        }

        private static void RejectAiResponse(DialogueResolutionResult result, string reason)
        {
            result.AcceptAiResponse = false;
            result.ResponseRejectReason = reason;
        }

        private static bool IsRequirementSatisfied(string requirementId, ProgressManager progressManager)
        {
            return progressManager.IsEvidenceCollected(requirementId) ||
                   progressManager.IsFactUnlocked(requirementId) ||
                   progressManager.IsStatementUnlocked(requirementId) ||
                   progressManager.IsInterrogationLayerUnlocked(requirementId) ||
                   progressManager.IsProgressTokenUnlocked(requirementId);
        }

        private static DialogueStatementEntryContext CreateStatementContext(
            StatementEntryData statement,
            bool isUnlockable)
        {
            var context = new DialogueStatementEntryContext
            {
                StatementId = statement.statementId ?? string.Empty,
                Phase = statement.phase ?? string.Empty,
                Text = statement.text ?? string.Empty,
                AiUsage = statement.aiUsage ?? string.Empty,
                ResponseIntent = statement.responseIntent ?? string.Empty,
                IsUnlocked = true,
                IsUnlockable = isUnlockable,
            };

            AddRange(context.UnlockRequirements, statement.unlockRequirements);
            AddRange(context.DialogueSamples, statement.dialogueSamples);
            AddRange(context.AvoidSaying, statement.avoidSaying);
            return context;
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

        private static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || values.Contains(value))
            {
                return;
            }

            values.Add(value);
        }

        private static void AddRange(List<string> destination, IReadOnlyList<string> source)
        {
            foreach (var value in source ?? Array.Empty<string>())
            {
                AddUnique(destination, value);
            }
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
            result.AcceptAiResponse = false;
            result.ResponseRejectReason = punishReason;
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

        private static void ValidatePromptInputs(
            RawDialogueInput rawInput,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcRuntimeManager npcRuntimeManager)
        {
            if (rawInput == null)
            {
                throw new ArgumentNullException(nameof(rawInput));
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
        }
    }
}
