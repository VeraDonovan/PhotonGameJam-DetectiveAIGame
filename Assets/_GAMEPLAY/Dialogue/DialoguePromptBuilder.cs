using System;
using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialoguePromptBuilder
    {
        public DialoguePromptMessages Build(DialogueTurnContext context, DialoguePromptSections promptSections)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (promptSections == null)
            {
                throw new ArgumentNullException(nameof(promptSections));
            }

            return new DialoguePromptMessages
            {
                SystemMessage = BuildSystemMessage(promptSections),
                UserMessage = BuildUserMessage(context),
            };
        }

        private static string BuildSystemMessage(DialoguePromptSections promptSections)
        {
            var builder = new StringBuilder();

            AppendPromptSection(builder, promptSections.DialogueBasePrompt);
            AppendPromptSection(builder, promptSections.NpcContextRulesPrompt);
            AppendPromptSection(builder, promptSections.RevealLogicRulesPrompt);

            return builder.ToString();
        }

        private static void AppendPromptSection(StringBuilder builder, string sectionText)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine(sectionText);
        }

        private static string BuildUserMessage(DialogueTurnContext context)
        {
            var builder = new StringBuilder();

            AppendTurnState(builder, context);
            AppendNpcPublicProfile(builder, context.NpcPublicProfile);
            AppendRawNpcAiProfile(builder, context.NpcAiProfileRawJson);
            AppendCandidateTopics(builder, context.CandidateTopics.Topics);
            AppendUnlockedState(builder, context);
            AppendAllowedInterrogationLayers(builder, context.AllowedInterrogationLayers);
            AppendRecentConversation(builder, context.RecentConversation);
            AppendPlayerInput(builder, context.RawInput);
            AppendOutputSchema(builder);

            return builder.ToString();
        }

        private static void AppendTurnState(StringBuilder builder, DialogueTurnContext context)
        {
            builder.AppendLine("TURN STATE");
            AppendKeyValue(builder, "npcId", context.NpcId);
            AppendKeyValue(builder, "phase", context.Phase.ToString());
            AppendKeyValue(builder, "annoyance", context.Annoyance.ToString());

            if (context.Phase == GamePhase.Interrogation)
            {
                AppendKeyValue(builder, "pressure", context.Pressure.ToString());
                AppendKeyValue(builder, "currentInterrogationLayerId", context.CurrentInterrogationLayerId);
            }

            AppendKeyValue(
                builder,
                "presentedEvidenceId",
                string.IsNullOrWhiteSpace(context.RawInput.PresentedEvidenceId)
                    ? "none"
                    : context.RawInput.PresentedEvidenceId);
            builder.AppendLine();
        }

        private static void AppendNpcPublicProfile(StringBuilder builder, NpcData npc)
        {
            builder.AppendLine("PUBLIC NPC PROFILE");

            if (npc == null)
            {
                builder.AppendLine("none");
                builder.AppendLine();
                return;
            }

            AppendKeyValue(builder, "npcId", npc.npcId);
            AppendKeyValue(builder, "displayName", npc.displayName);
            AppendKeyValue(builder, "roleType", npc.roleType);
            AppendKeyValue(builder, "gender", npc.gender);
            AppendKeyValue(builder, "age", npc.age.ToString());
            AppendKeyValue(builder, "occupation", npc.occupation);
            AppendKeyValue(builder, "relationshipToVictim", npc.relationshipToVictim);
            AppendKeyValue(builder, "profileText", npc.profileText);
            builder.AppendLine();
        }

        private static void AppendRawNpcAiProfile(StringBuilder builder, string rawJson)
        {
            builder.AppendLine("RAW NPC AI ROLEPLAY PROFILE JSON");
            builder.AppendLine(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);
            builder.AppendLine();
        }

        private static void AppendCandidateTopics(StringBuilder builder, IReadOnlyList<DialogueCandidateTopic> topics)
        {
            builder.AppendLine("CANDIDATE TOPICS");
            builder.AppendLine("Only these available topic ids may be selected. Use irrelevant if none match.");

            var wroteTopic = false;
            foreach (var topic in topics ?? Array.Empty<DialogueCandidateTopic>())
            {
                if (topic == null || topic.Availability != DialogueTopicAvailability.Available)
                {
                    continue;
                }

                wroteTopic = true;
                builder.Append("- topicId: ");
                builder.AppendLine(EscapeLine(topic.TopicId));
                AppendKeyValue(builder, "  displayName", topic.DisplayName);
                AppendKeyValue(builder, "  safeRoleplayTopic", topic.IsSafeRoleplayTopic.ToString());
                AppendStringList(builder, "  matchHints", topic.MatchHints);
                AppendKeyValue(builder, "  searchPhaseTopic", topic.IsSearchPhaseTopic.ToString());
                AppendKeyValue(builder, "  interrogationPhaseTopic", topic.IsInterrogationPhaseTopic.ToString());
                AppendStringList(builder, "  requiredEvidenceIds", topic.RequiredEvidenceIds);
                AppendStringList(builder, "  requiredFactIds", topic.RequiredFactIds);
                AppendStringList(builder, "  requiredStatementIds", topic.RequiredStatementIds);
                AppendStringList(builder, "  requiredLayerIds", topic.RequiredInterrogationLayerIds);
                AppendStringList(builder, "  requiredTokenIds", topic.RequiredTokenIds);
                AppendStatementGuidance(builder, topic.RelatedStatements);
            }

            if (!wroteTopic)
            {
                builder.AppendLine("none");
            }

            builder.AppendLine();
        }

        private static void AppendStatementGuidance(
            StringBuilder builder,
            IReadOnlyList<DialogueStatementEntryContext> statements)
        {
            if (statements == null || statements.Count == 0)
            {
                return;
            }

            builder.AppendLine("  statementGuidance:");
            foreach (var statement in statements)
            {
                if (statement == null)
                {
                    continue;
                }

                builder.Append("  - statementId: ");
                builder.AppendLine(EscapeLine(statement.StatementId));
                AppendKeyValue(builder, "    phase", statement.Phase);
                AppendKeyValue(builder, "    text", statement.Text);
                AppendKeyValue(builder, "    aiUsage", statement.AiUsage);
                AppendKeyValue(builder, "    responseIntent", statement.ResponseIntent);
                AppendKeyValue(builder, "    isUnlocked", statement.IsUnlocked.ToString());
                AppendKeyValue(builder, "    isUnlockable", statement.IsUnlockable.ToString());
                AppendStringList(builder, "    dialogueSamples", statement.DialogueSamples);
                AppendStringList(builder, "    avoidSaying", statement.AvoidSaying);
            }
        }

        private static void AppendUnlockedState(StringBuilder builder, DialogueTurnContext context)
        {
            builder.AppendLine("RUNTIME UNLOCKED STATE");
            AppendStringList(builder, "unlockedFactIds", context.RelevantUnlockedFactIds);
            AppendStringList(builder, "unlockedStatementIds", context.RelevantUnlockedStatementIds);
            AppendStringList(builder, "unlockedLayerIds", context.RelevantUnlockedLayerIds);
            AppendStringList(builder, "allowedRevealIds", context.AllowedRevealIds);
            AppendStringList(builder, "mustWithholdIds", context.MustWithholdIds);
            builder.AppendLine();

            builder.AppendLine("UNLOCKED STATEMENT DETAILS");
            if (context.RelevantUnlockedStatements.Count == 0)
            {
                builder.AppendLine("none");
            }
            else
            {
                foreach (var statement in context.RelevantUnlockedStatements)
                {
                    builder.Append("- statementId: ");
                    builder.AppendLine(EscapeLine(statement.StatementId));
                    AppendKeyValue(builder, "  phase", statement.Phase);
                    AppendKeyValue(builder, "  text", statement.Text);
                    AppendKeyValue(builder, "  responseIntent", statement.ResponseIntent);
                }
            }

            builder.AppendLine();
        }

        private static void AppendAllowedInterrogationLayers(
            StringBuilder builder,
            IReadOnlyList<TruthInterrogationLayerData> layers)
        {
            builder.AppendLine("ALLOWED INTERROGATION REVEAL DATA");
            builder.AppendLine("This is the only hidden-layer reveal data available for response wording.");

            if (layers == null || layers.Count == 0)
            {
                builder.AppendLine("none");
                builder.AppendLine();
                return;
            }

            foreach (var layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                builder.Append("- layerId: ");
                builder.AppendLine(EscapeLine(layer.layerId));
                AppendKeyValue(builder, "  roundType", layer.roundType);
                AppendKeyValue(builder, "  topic", layer.topic);
                AppendKeyValue(builder, "  revealGoal", layer.revealGoal);
                AppendKeyValue(builder, "  aiGuidance", layer.aiGuidance);
                AppendStringList(builder, "  revealFactIds", layer.revealFactIds);
                AppendStringList(builder, "  relatedStatementTopicIds", layer.relatedStatementTopicIds);
                AppendStringList(builder, "  examplePhrasings", layer.examplePhrasings);
            }

            builder.AppendLine();
        }

        private static void AppendRecentConversation(
            StringBuilder builder,
            IReadOnlyList<DialogueConversationExchange> exchanges)
        {
            builder.AppendLine("RECENT CONVERSATION");

            if (exchanges == null || exchanges.Count == 0)
            {
                builder.AppendLine("none");
                builder.AppendLine();
                return;
            }

            for (var i = 0; i < exchanges.Count; i++)
            {
                var exchange = exchanges[i];
                if (exchange == null)
                {
                    continue;
                }

                builder.Append("- exchangeIndex: ");
                builder.AppendLine(i.ToString());
                AppendKeyValue(builder, "  player", exchange.PlayerText);
                AppendKeyValue(builder, "  npc", exchange.NpcText);
            }

            builder.AppendLine();
        }

        private static void AppendPlayerInput(StringBuilder builder, RawDialogueInput rawInput)
        {
            builder.AppendLine("CURRENT PLAYER INPUT");
            AppendKeyValue(builder, "rawText", rawInput.RawPlayerText);
            AppendKeyValue(
                builder,
                "presentedEvidenceId",
                string.IsNullOrWhiteSpace(rawInput.PresentedEvidenceId)
                    ? "none"
                    : rawInput.PresentedEvidenceId);
            builder.AppendLine();
        }

        private static void AppendOutputSchema(StringBuilder builder)
        {
            builder.AppendLine("OUTPUT JSON SCHEMA");
            builder.AppendLine("Return exactly one JSON object in this shape:");
            builder.AppendLine("{");
            builder.AppendLine("  \"interpretation\": {");
            builder.AppendLine("    \"topicId\": \"candidate_topic_id_or_irrelevant\",");
            builder.AppendLine("    \"confidence\": 0.0,");
            builder.AppendLine("    \"isIrrelevant\": false");
            builder.AppendLine("  },");
            builder.AppendLine("  \"response\": {");
            builder.AppendLine("    \"prose\": \"short in-character Chinese NPC response\",");
            builder.AppendLine("    \"usedStatementId\": \"\",");
            builder.AppendLine("    \"usedRevealIds\": []");
            builder.AppendLine("  }");
            builder.AppendLine("}");
        }

        private static void AppendKeyValue(StringBuilder builder, string key, string value)
        {
            builder.Append(key);
            builder.Append(": ");
            builder.AppendLine(EscapeLine(value));
        }

        private static void AppendStringList(StringBuilder builder, string label, IReadOnlyList<string> values)
        {
            builder.Append(label);
            builder.Append(": [");

            if (values != null)
            {
                var wroteValue = false;
                foreach (var value in values)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    if (wroteValue)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(EscapeLine(value));
                    wroteValue = true;
                }
            }

            builder.AppendLine("]");
        }

        private static string EscapeLine(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }

    public sealed class DialoguePromptMessages
    {
        public string SystemMessage { get; set; } = string.Empty;
        public string UserMessage { get; set; } = string.Empty;
    }

    public sealed class DialoguePromptSections
    {
        public string DialogueBasePrompt { get; set; } = string.Empty;
        public string NpcContextRulesPrompt { get; set; } = string.Empty;
        public string RevealLogicRulesPrompt { get; set; } = string.Empty;
    }
}
