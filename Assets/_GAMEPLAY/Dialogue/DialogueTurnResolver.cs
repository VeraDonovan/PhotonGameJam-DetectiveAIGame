using System;
using System.Collections.Generic;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueTurnResolver
    {
        public const int DefaultAnnoyanceGain = 20;
        public const int DefaultInterrogationPressureGain = 20;
        public const int RepeatTopicRefusalAnnoyanceThreshold = 80;

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

            var npcState = npcRuntimeManager.GetOrCreateDialogueState(rawInput.NpcId);
            var candidateTopics = candidateTopicResolver.Resolve(
                rawInput.NpcId,
                rawInput.Phase,
                databaseManager,
                progressManager,
                npcRuntimeManager);

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
                    NewAnnoyance = rawInput.Phase == GamePhase.Exploration ? npcState.Annoyance : 0,
                    NewPressure = rawInput.Phase == GamePhase.Interrogation ? npcState.Pressure : 0,
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

            var npcState = npcRuntimeManager.GetOrCreateDialogueState(rawInput.NpcId);
            var candidateTopics = candidateTopicResolver.Resolve(
                rawInput.NpcId,
                rawInput.Phase,
                databaseManager,
                progressManager,
                npcRuntimeManager);

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
                Annoyance = rawInput.Phase == GamePhase.Exploration ? npcState.Annoyance : 0,
                Pressure = rawInput.Phase == GamePhase.Interrogation ? npcState.Pressure : 0,
                CurrentInterrogationLevel = rawInput.Phase == GamePhase.Interrogation
                    ? npcState.CurrentInterrogationLevel
                    : 0,
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
            PopulateRelevantBeats(context, databaseManager, progressManager);
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

        private static void PopulateRelevantBeats(
            DialogueTurnContext context,
            DatabaseManager databaseManager,
            ProgressManager progressManager)
        {
            foreach (var node in databaseManager.DialogueBeatDatabase.GetNodesByNpc(context.NpcId))
            {
                if (node == null)
                {
                    continue;
                }

                if (progressManager.IsDialogueBeatVisited(node.nodeId))
                {
                    AddUnique(context.RelevantVisitedBeatIds, node.nodeId);
                }

                if (!string.IsNullOrWhiteSpace(node.caughtLieId) && progressManager.IsCaughtLie(node.caughtLieId))
                {
                    AddUnique(context.RelevantCaughtLieIds, node.caughtLieId);
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
                NewAnnoyance = interpretedAction.Phase == GamePhase.Exploration ? npcState.Annoyance : 0,
                NewPressure = interpretedAction.Phase == GamePhase.Interrogation ? npcState.Pressure : 0,
            };

            if (interpretedAction.IsIrrelevant || string.IsNullOrWhiteSpace(interpretedAction.MatchedTopicId))
            {
                ApplyInvalidInputPenalty(
                    result,
                    interpretedAction.Phase,
                    npcState,
                    npcRuntimeManager,
                    interpretedAction.NpcId,
                    "irrelevant_input");
                return result;
            }

            var matchedTopic = FindTopic(candidateTopics, interpretedAction.MatchedTopicId);
            if (matchedTopic == null)
            {
                ApplyInvalidInputPenalty(
                    result,
                    interpretedAction.Phase,
                    npcState,
                    npcRuntimeManager,
                    interpretedAction.NpcId,
                    "topic_outside_candidate_set");
                return result;
            }

            if (matchedTopic.Availability != DialogueTopicAvailability.Available)
            {
                ApplyInvalidInputPenalty(
                    result,
                    interpretedAction.Phase,
                    npcState,
                    npcRuntimeManager,
                    interpretedAction.NpcId,
                    "topic_not_available");
                return result;
            }

            if (matchedTopic.IsSafeRoleplayTopic || matchedTopic.IsOpenFallbackTopic)
            {
                result.ResolutionType = DialogueResolutionType.NoProgress;
                ValidateNonProgressFallbackResponseUsage(interpretedAction, result);

                if (!result.AcceptAiResponse)
                {
                    return result;
                }

                if (IsRepeatedResolvedTopic(matchedTopic, npcState))
                {
                    ApplyRepeatedTopicPenalty(
                        result,
                        interpretedAction.Phase,
                        npcState,
                        npcRuntimeManager,
                        interpretedAction.NpcId);
                    return result;
                }

                MarkTopicOutcome(npcRuntimeManager, interpretedAction.NpcId, matchedTopic);
                return result;
            }

            if (interpretedAction.Phase == GamePhase.Exploration ||
                interpretedAction.Phase == GamePhase.Interrogation)
            {
                ResolveBeatProgress(
                    rawInput,
                    interpretedAction,
                    matchedTopic,
                    databaseManager,
                    progressManager,
                    npcState,
                    npcRuntimeManager,
                    result);
                return result;
            }

            result.ResolutionType = DialogueResolutionType.NoProgress;
            ValidateAiResponseUsage(interpretedAction, matchedTopic, progressManager, result);
            if (result.AcceptAiResponse)
            {
                MarkTopicOutcome(npcRuntimeManager, interpretedAction.NpcId, matchedTopic);
            }
            return result;
        }

        private static void ValidateNonProgressFallbackResponseUsage(
            InterpretedDialogueAction interpretedAction,
            DialogueResolutionResult result)
        {
            if (!string.IsNullOrWhiteSpace(interpretedAction.UsedBeatId))
            {
                RejectAiResponse(result, "non_progress_fallback_used_beat");
                return;
            }

            if (!string.IsNullOrWhiteSpace(interpretedAction.UsedStatementId))
            {
                RejectAiResponse(result, "non_progress_fallback_used_statement");
                return;
            }

            foreach (var revealId in interpretedAction.UsedRevealIds ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(revealId))
                {
                    RejectAiResponse(result, "non_progress_fallback_used_reveal");
                    return;
                }
            }
        }

        private static void ResolveBeatProgress(
            RawDialogueInput rawInput,
            InterpretedDialogueAction interpretedAction,
            DialogueCandidateTopic matchedTopic,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcDialogueRuntimeState npcState,
            NpcRuntimeManager npcRuntimeManager,
            DialogueResolutionResult result)
        {
            var beatNode = FindNextUnlockableBeat(
                interpretedAction,
                matchedTopic,
                databaseManager.DialogueBeatDatabase,
                progressManager,
                npcState.CurrentInterrogationLayerId,
                rawInput.PresentedEvidenceId);

            if (beatNode == null)
            {
                result.ResolutionType = DialogueResolutionType.NoProgress;
                ValidateAiResponseUsage(interpretedAction, matchedTopic, progressManager, result);

                if (!result.AcceptAiResponse)
                {
                    return;
                }

                if (IsRepeatedResolvedTopic(matchedTopic, npcState))
                {
                    ApplyRepeatedTopicPenalty(
                        result,
                        interpretedAction.Phase,
                        npcState,
                        npcRuntimeManager,
                        interpretedAction.NpcId);
                    return;
                }

                MarkTopicOutcome(npcRuntimeManager, interpretedAction.NpcId, matchedTopic);
                return;
            }

            var unlockedBeatsBefore = new HashSet<string>(progressManager.VisitedDialogueBeatIds);
            var caughtLiesBefore = new HashSet<string>(progressManager.CaughtLieIds);
            var unlockedFactsBefore = new HashSet<string>(progressManager.UnlockedFactIds);
            var unlockedStatementsBefore = new HashSet<string>(progressManager.UnlockedStatementIds);
            var unlockedLayersBefore = new HashSet<string>(progressManager.UnlockedInterrogationLayerIds);
            var unlockedTokensBefore = new HashSet<string>(progressManager.RuntimeState.UnlockedProgressTokens);

            if (!progressManager.VisitDialogueBeat(beatNode.nodeId))
            {
                result.ResolutionType = DialogueResolutionType.NoProgress;
                return;
            }

            AddNewUnlockedIds(unlockedBeatsBefore, progressManager.VisitedDialogueBeatIds, result.VisitedBeatIds);
            AddNewUnlockedIds(caughtLiesBefore, progressManager.CaughtLieIds, result.CaughtLieIds);
            AddNewUnlockedIds(unlockedStatementsBefore, progressManager.UnlockedStatementIds, result.UnlockedStatementIds);
            AddNewUnlockedIds(unlockedFactsBefore, progressManager.UnlockedFactIds, result.UnlockedFactIds);
            AddNewUnlockedIds(unlockedLayersBefore, progressManager.UnlockedInterrogationLayerIds, result.UnlockedLayerIds);
            AddNewUnlockedIds(unlockedTokensBefore, progressManager.RuntimeState.UnlockedProgressTokens, result.UnlockedTokenIds);

            ApplyInterrogationPressureForValidatedBeat(
                interpretedAction.Phase,
                interpretedAction.NpcId,
                npcState,
                npcRuntimeManager,
                result);
            ApplyCurrentInterrogationLayer(
                interpretedAction.Phase,
                interpretedAction.NpcId,
                databaseManager.TruthDatabase,
                npcRuntimeManager,
                result);

            result.ResolutionType = DetermineProgressResolutionType(result);

            ValidateAiResponseUsage(interpretedAction, matchedTopic, progressManager, result);
            if (result.AcceptAiResponse)
            {
                MarkTopicOutcome(npcRuntimeManager, interpretedAction.NpcId, matchedTopic);
            }
        }

        private static bool HasUnlockableUnusedBeat(DialogueCandidateTopic matchedTopic)
        {
            foreach (var beat in matchedTopic.RelatedBeatNodes)
            {
                if (beat != null && beat.IsUnlockable && !beat.IsVisited)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRepeatedResolvedTopic(
            DialogueCandidateTopic matchedTopic,
            NpcDialogueRuntimeState npcState)
        {
            return npcState.DiscussedTopicIds.Contains(matchedTopic.TopicId) &&
                   !HasUnlockableUnusedBeat(matchedTopic);
        }

        private static void MarkTopicOutcome(
            NpcRuntimeManager npcRuntimeManager,
            string npcId,
            DialogueCandidateTopic matchedTopic)
        {
            if (HasUnlockableUnusedBeat(matchedTopic))
            {
                npcRuntimeManager.MarkTopicDiscussed(npcId, matchedTopic.TopicId);
                return;
            }

            npcRuntimeManager.MarkTopicResolved(npcId, matchedTopic.TopicId);
        }

        private static DialogueBeatNodeData FindNextUnlockableBeat(
            InterpretedDialogueAction interpretedAction,
            DialogueCandidateTopic matchedTopic,
            DialogueBeatDatabase beatDatabase,
            ProgressManager progressManager,
            string currentInterrogationLayerId,
            string presentedEvidenceId)
        {
            if (string.IsNullOrWhiteSpace(interpretedAction.UsedBeatId))
            {
                return null;
            }

            if (!beatDatabase.TryGetNode(interpretedAction.UsedBeatId, out var node) ||
                node == null ||
                progressManager.IsDialogueBeatVisited(node.nodeId))
            {
                return null;
            }

            var phaseMatches = string.Equals(node.phase, interpretedAction.Phase.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!phaseMatches ||
                !string.Equals(matchedTopic.TopicId, FindTopicIdForBeat(beatDatabase, node.nodeId), StringComparison.Ordinal))
            {
                return null;
            }

            return AreBeatRequirementsSatisfied(node, progressManager, currentInterrogationLayerId, presentedEvidenceId)
                ? node
                : null;
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

            if (!IsUsedBeatAllowed(interpretedAction.UsedBeatId, matchedTopic, result))
            {
                RejectAiResponse(result, "used_beat_not_allowed");
                return;
            }

            if (!AreUsedRevealsAllowed(interpretedAction.UsedRevealIds, progressManager, result))
            {
                RejectAiResponse(result, "used_reveal_not_allowed");
            }
        }

        private static bool IsUsedBeatAllowed(
            string usedBeatId,
            DialogueCandidateTopic matchedTopic,
            DialogueResolutionResult result)
        {
            if (string.IsNullOrWhiteSpace(usedBeatId))
            {
                return true;
            }

            foreach (var beat in matchedTopic.RelatedBeatNodes)
            {
                if (!string.Equals(beat.NodeId, usedBeatId, StringComparison.Ordinal))
                {
                    continue;
                }

                return beat.IsUnlockable || result.VisitedBeatIds.Contains(usedBeatId);
            }

            return false;
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

        private static bool AreBeatRequirementsSatisfied(
            DialogueBeatNodeData node,
            ProgressManager progressManager,
            string currentInterrogationLayerId,
            string presentedEvidenceId)
        {
            var hasPresentedEvidenceRequirement = node.requiredEvidenceIds != null && node.requiredEvidenceIds.Count > 0;
            var presentedRequiredEvidence = !hasPresentedEvidenceRequirement;

            foreach (var evidenceId in node.requiredEvidenceIds ?? new List<string>())
            {
                if (!progressManager.IsEvidenceCollected(evidenceId))
                {
                    return false;
                }

                if (string.Equals(evidenceId, presentedEvidenceId, StringComparison.Ordinal))
                {
                    presentedRequiredEvidence = true;
                }
            }

            foreach (var factId in node.requiredFactIds ?? new List<string>())
            {
                if (!progressManager.IsFactUnlocked(factId))
                {
                    return false;
                }
            }

            foreach (var statementId in node.requiredStatementIds ?? new List<string>())
            {
                if (!progressManager.IsStatementUnlocked(statementId))
                {
                    return false;
                }
            }

            foreach (var layerId in node.requiredLayerIds ?? new List<string>())
            {
                if (!IsInterrogationLayerRequirementSatisfied(layerId, progressManager, currentInterrogationLayerId))
                {
                    return false;
                }
            }

            foreach (var tokenId in node.requiredTokenIds ?? new List<string>())
            {
                if (!progressManager.IsProgressTokenUnlocked(tokenId))
                {
                    return false;
                }
            }

            return presentedRequiredEvidence;
        }

        private static bool IsInterrogationLayerRequirementSatisfied(
            string layerId,
            ProgressManager progressManager,
            string currentInterrogationLayerId)
        {
            return progressManager.IsInterrogationLayerUnlocked(layerId) ||
                   string.Equals(layerId, currentInterrogationLayerId, StringComparison.Ordinal);
        }

        private static string FindTopicIdForBeat(DialogueBeatDatabase beatDatabase, string beatId)
        {
            foreach (var topic in beatDatabase.TopicById.Values)
            {
                if (topic == null)
                {
                    continue;
                }

                foreach (var node in topic.nodes ?? new List<DialogueBeatNodeData>())
                {
                    if (node != null && string.Equals(node.nodeId, beatId, StringComparison.Ordinal))
                    {
                        return topic.topicId ?? string.Empty;
                    }
                }
            }

            return string.Empty;
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
                IsUnlocked = true,
                IsUnlockable = isUnlockable,
            };

            AddRange(context.UnlockRequirements, statement.unlockRequirements);
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

        private static DialogueResolutionType DetermineProgressResolutionType(DialogueResolutionResult result)
        {
            var progressBucketCount = 0;
            if (result.UnlockedFactIds.Count > 0)
            {
                progressBucketCount++;
            }

            if (result.UnlockedStatementIds.Count > 0)
            {
                progressBucketCount++;
            }

            if (result.UnlockedLayerIds.Count > 0)
            {
                progressBucketCount++;
            }

            if (result.UnlockedTokenIds.Count > 0)
            {
                progressBucketCount++;
            }

            if (result.PressureDelta != 0)
            {
                progressBucketCount++;
            }

            if (result.VisitedBeatIds.Count > 0 && progressBucketCount == 0)
            {
                return DialogueResolutionType.NoProgress;
            }

            if (progressBucketCount > 1)
            {
                return DialogueResolutionType.CompositeProgress;
            }

            if (result.UnlockedLayerIds.Count > 0)
            {
                return DialogueResolutionType.InterrogationLayerUnlocked;
            }

            if (result.UnlockedFactIds.Count > 0)
            {
                return DialogueResolutionType.FactUnlocked;
            }

            if (result.UnlockedStatementIds.Count > 0)
            {
                return DialogueResolutionType.StatementUnlocked;
            }

            if (result.UnlockedTokenIds.Count > 0)
            {
                return DialogueResolutionType.TokenUnlocked;
            }

            if (result.PressureDelta != 0)
            {
                return DialogueResolutionType.PressureChanged;
            }

            return DialogueResolutionType.NoProgress;
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

        private static void ApplyInvalidInputPenalty(
            DialogueResolutionResult result,
            GamePhase phase,
            NpcDialogueRuntimeState npcState,
            NpcRuntimeManager npcRuntimeManager,
            string npcId,
            string punishReason)
        {
            if (phase != GamePhase.Exploration)
            {
                result.ResolutionType = DialogueResolutionType.Punished;
                result.AcceptAiResponse = false;
                result.ResponseRejectReason = punishReason;
                result.NewAnnoyance = 0;
                result.NewPressure = phase == GamePhase.Interrogation ? npcState.Pressure : 0;
                result.PunishReason = punishReason;
                return;
            }

            var oldAnnoyance = npcState.Annoyance;
            var newAnnoyance = npcRuntimeManager.AddAnnoyance(npcId, DefaultAnnoyanceGain);

            result.ResolutionType = DialogueResolutionType.Punished;
            result.AcceptAiResponse = false;
            result.ResponseRejectReason = punishReason;
            result.AnnoyanceDelta = newAnnoyance - oldAnnoyance;
            result.NewAnnoyance = newAnnoyance;
            result.NewPressure = 0;
            result.PunishReason = punishReason;
        }

        private static void ApplyRepeatedTopicPenalty(
            DialogueResolutionResult result,
            GamePhase phase,
            NpcDialogueRuntimeState npcState,
            NpcRuntimeManager npcRuntimeManager,
            string npcId)
        {
            if (phase != GamePhase.Exploration)
            {
                result.ResolutionType = DialogueResolutionType.NoProgress;
                result.NewAnnoyance = 0;
                result.NewPressure = phase == GamePhase.Interrogation ? npcState.Pressure : 0;
                result.PunishReason = "resolved_topic_repeated";
                return;
            }

            var oldAnnoyance = npcState.Annoyance;
            var newAnnoyance = npcRuntimeManager.AddAnnoyance(npcId, DefaultAnnoyanceGain);

            result.AnnoyanceDelta = newAnnoyance - oldAnnoyance;
            result.NewAnnoyance = newAnnoyance;
            result.NewPressure = 0;
            result.PunishReason = "resolved_topic_repeated";

            if (newAnnoyance >= RepeatTopicRefusalAnnoyanceThreshold)
            {
                result.ResolutionType = DialogueResolutionType.Punished;
                result.AcceptAiResponse = false;
                result.ResponseRejectReason = "resolved_topic_repeated";
                return;
            }

            result.ResolutionType = DialogueResolutionType.NoProgress;
        }

        private static void ApplyInterrogationPressureForValidatedBeat(
            GamePhase phase,
            string npcId,
            NpcDialogueRuntimeState npcState,
            NpcRuntimeManager npcRuntimeManager,
            DialogueResolutionResult result)
        {
            if (phase != GamePhase.Interrogation)
            {
                return;
            }

            var oldPressure = npcState.Pressure;
            var newPressure = npcRuntimeManager.AddInterrogationPressure(npcId, DefaultInterrogationPressureGain);
            result.PressureDelta = newPressure - oldPressure;
            result.NewPressure = newPressure;
            result.NewAnnoyance = 0;
        }

        private static void ApplyCurrentInterrogationLayer(
            GamePhase phase,
            string npcId,
            TruthDatabase truthDatabase,
            NpcRuntimeManager npcRuntimeManager,
            DialogueResolutionResult result)
        {
            if (phase != GamePhase.Interrogation)
            {
                return;
            }

            foreach (var layerId in result.UnlockedLayerIds)
            {
                npcRuntimeManager.SetCurrentInterrogationLayer(
                    npcId,
                    layerId,
                    GetInterrogationLayerLevel(npcId, layerId, truthDatabase));
            }
        }

        private static int GetInterrogationLayerLevel(
            string npcId,
            string layerId,
            TruthDatabase truthDatabase)
        {
            var level = 0;
            foreach (var layer in truthDatabase.GetInterrogationLayersByNpc(npcId))
            {
                level++;
                if (layer != null && string.Equals(layer.layerId, layerId, StringComparison.Ordinal))
                {
                    return level;
                }
            }

            return 0;
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
