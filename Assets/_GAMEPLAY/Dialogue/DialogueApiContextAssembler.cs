using System;
using System.Collections.Generic;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueApiContextAssembler : IDialogueApiContextAssembler
    {
        private readonly DialogueCandidateTopicResolver candidateTopicResolver;
        private readonly IDialogueConversationHistorySelector historySelector;

        public DialogueApiContextAssembler()
            : this(new DialogueCandidateTopicResolver(), new DialogueConversationHistorySelector())
        {
        }

        public DialogueApiContextAssembler(
            DialogueCandidateTopicResolver candidateTopicResolver,
            IDialogueConversationHistorySelector historySelector)
        {
            this.candidateTopicResolver = candidateTopicResolver ??
                                          throw new ArgumentNullException(nameof(candidateTopicResolver));
            this.historySelector = historySelector ??
                                   throw new ArgumentNullException(nameof(historySelector));
        }

        public DialogueApiPromptContext Assemble(
            RawDialogueInput rawInput,
            DatabaseManager databaseManager,
            ProgressManager progressManager,
            NpcRuntimeManager npcRuntimeManager,
            DialogueConversationSession conversationSession,
            DialoguePromptMode mode)
        {
            ValidateInputs(rawInput, databaseManager, progressManager, npcRuntimeManager);

            var npcState = npcRuntimeManager.GetOrCreateDialogueState(rawInput.NpcId);
            var candidateTopics = candidateTopicResolver.Resolve(
                rawInput.NpcId,
                rawInput.Phase,
                databaseManager,
                progressManager,
                npcRuntimeManager);

            var context = new DialogueApiPromptContext
            {
                NpcId = rawInput.NpcId,
                Phase = rawInput.Phase,
                RawInput = rawInput,
                CandidateTopics = candidateTopics,
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
            PopulateRecentConversation(context, conversationSession, mode);

            return context;
        }

        private void PopulateRecentConversation(
            DialogueApiPromptContext context,
            DialogueConversationSession conversationSession,
            DialoguePromptMode mode)
        {
            if (conversationSession == null ||
                !string.Equals(conversationSession.NpcId, context.NpcId, StringComparison.Ordinal))
            {
                return;
            }

            foreach (var exchange in historySelector.SelectForApi(conversationSession, mode))
            {
                context.RecentConversation.Add(new DialogueConversationExchange
                {
                    PlayerText = exchange.PlayerText,
                    NpcText = exchange.NpcText,
                });
            }
        }

        private static void PopulateDatabaseContext(
            DialogueApiPromptContext context,
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
        }

        private static void PopulateRelevantFacts(
            DialogueApiPromptContext context,
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
            DialogueApiPromptContext context,
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
                    context.RelevantUnlockedStatements.Add(CreateStatementContext(statement));
                }
            }
        }

        private static void PopulateRelevantBeats(
            DialogueApiPromptContext context,
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
            DialogueApiPromptContext context,
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

        private static DialogueStatementEntryContext CreateStatementContext(StatementEntryData statement)
        {
            var context = new DialogueStatementEntryContext
            {
                StatementId = statement.statementId ?? string.Empty,
                Phase = statement.phase ?? string.Empty,
                Text = statement.text ?? string.Empty,
                IsUnlocked = true,
                IsUnlockable = true,
            };

            AddRange(context.UnlockRequirements, statement.unlockRequirements);
            return context;
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

        private static void ValidateInputs(
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
                throw new ArgumentException("DialogueApiContextAssembler requires RawDialogueInput.NpcId.", nameof(rawInput));
            }
        }
    }
}
