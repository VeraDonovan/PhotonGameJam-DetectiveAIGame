using System;
using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialoguePromptBuilder : IDialoguePromptBuilder
    {
        public DialoguePromptMessages Build(DialogueApiPromptContext context, DialoguePromptSections promptSections)
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

        public DialoguePromptMessages BuildOpening(DialogueApiPromptContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new DialoguePromptMessages
            {
                SystemMessage = BuildOpeningSystemMessage(),
                UserMessage = BuildOpeningUserMessage(context),
            };
        }

        private static string BuildOpeningSystemMessage()
        {
            return
                "你在生成一款中文侦探游戏里的NPC开场白。\n" +
                "玩家是来调查案件的警察，你是在对警察开口说第一句话。\n" +
                "只返回一句简短的中文台词。\n" +
                "不要输出JSON。\n" +
                "不要解释规则。\n" +
                "不要复述提示词。\n" +
                "不要输出旁白、括号说明、系统信息或分析。\n" +
                "优先根据 OPENING CONTEXT SUMMARY 延续此前对话的语气与关系；若无摘要则根据公开资料自然开场。\n" +
                "只根据给定的公开资料、当前阶段和开场上下文，用符合角色的方式先开口。";
        }

        private static string BuildOpeningUserMessage(DialogueApiPromptContext context)
        {
            var builder = new StringBuilder();
            builder.AppendLine("当前是NPC主动开口的开场。");
            builder.Append("阶段: ");
            builder.AppendLine(context.Phase.ToString());
            builder.AppendLine("玩家身份: 警察");
            builder.AppendLine("场景: 你正在接受警方关于案件的问话。");

            if (context.NpcPublicProfile != null)
            {
                builder.Append("姓名: ");
                builder.AppendLine(context.NpcPublicProfile.displayName ?? string.Empty);
                builder.Append("身份: ");
                builder.AppendLine(context.NpcPublicProfile.occupation ?? string.Empty);
                builder.Append("与死者关系: ");
                builder.AppendLine(context.NpcPublicProfile.relationshipToVictim ?? string.Empty);
                builder.Append("公开简介: ");
                builder.AppendLine(context.NpcPublicProfile.profileText ?? string.Empty);
            }

            AppendOpeningContextSummary(builder, context.OpeningContextSummary);

            if (DialogueConversationConfig.OpeningVerbatimExchangeCount > 0)
            {
                builder.AppendLine("最近对话:");
                if (context.RecentConversation == null || context.RecentConversation.Count == 0)
                {
                    builder.AppendLine("无");
                }
                else
                {
                    for (int i = 0; i < context.RecentConversation.Count; i++)
                    {
                        var exchange = context.RecentConversation[i];
                        builder.Append("玩家: ");
                        builder.AppendLine(exchange.PlayerText ?? string.Empty);
                        builder.Append("NPC: ");
                        builder.AppendLine(exchange.NpcText ?? string.Empty);
                    }
                }
            }

            builder.AppendLine("要求:");
            builder.AppendLine("玩家刚刚开始接触你，这一回合还没有输入具体问题。");
            builder.AppendLine("你要意识到对方是警察，因此开场语气要符合被警方询问时的反应。");
            builder.AppendLine("请直接说一句自然的中文开场白。");
            return builder.ToString();
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

        private static string BuildUserMessage(DialogueApiPromptContext context)
        {
            var builder = new StringBuilder();

            AppendTurnState(builder, context);
            AppendNpcPublicProfile(builder, context.NpcPublicProfile);
            AppendRawNpcAiProfile(builder, context.NpcAiProfileRawJson);
            AppendCandidateTopics(builder, context.CandidateTopics.Topics);
            AppendUnlockedState(builder, context);
            AppendAllowedInterrogationLayers(builder, context.AllowedInterrogationLayers);
            AppendConversationSummary(builder, context.TurnConversationSummary);
            AppendRecentConversation(builder, context.RecentConversation);
            AppendPlayerInput(builder, context.RawInput);
            AppendOutputSchema(builder);

            return builder.ToString();
        }

        private static void AppendTurnState(StringBuilder builder, DialogueApiPromptContext context)
        {
            builder.AppendLine("TURN STATE");
            AppendKeyValue(builder, "npcId", context.NpcId);
            AppendKeyValue(builder, "phase", context.Phase.ToString());
            AppendKeyValue(builder, "annoyance", context.Annoyance.ToString());

            if (context.Phase == GamePhase.Interrogation)
            {
                AppendKeyValue(builder, "currentInterrogationLevel", context.CurrentInterrogationLevel.ToString());
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
                AppendKeyValue(builder, "  openFallbackTopic", topic.IsOpenFallbackTopic.ToString());
                AppendStringList(builder, "  matchHints", topic.MatchHints);
                AppendKeyValue(builder, "  searchPhaseTopic", topic.IsSearchPhaseTopic.ToString());
                AppendKeyValue(builder, "  interrogationPhaseTopic", topic.IsInterrogationPhaseTopic.ToString());
                AppendStringList(builder, "  requiredEvidenceIds", topic.RequiredEvidenceIds);
                AppendStringList(builder, "  requiredFactIds", topic.RequiredFactIds);
                AppendStringList(builder, "  requiredStatementIds", topic.RequiredStatementIds);
                AppendStringList(builder, "  requiredLayerIds", topic.RequiredInterrogationLayerIds);
                AppendStringList(builder, "  requiredTokenIds", topic.RequiredTokenIds);
                AppendStatementGuidance(builder, topic.RelatedStatements);
                AppendBeatGuidance(builder, topic.RelatedBeatNodes);
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
                AppendKeyValue(builder, "    isUnlocked", statement.IsUnlocked.ToString());
                AppendKeyValue(builder, "    isUnlockable", statement.IsUnlockable.ToString());
            }
        }

        private static void AppendBeatGuidance(
            StringBuilder builder,
            IReadOnlyList<DialogueBeatNodeContext> beatNodes)
        {
            if (beatNodes == null || beatNodes.Count == 0)
            {
                return;
            }

            builder.AppendLine("  beatGuidance:");
            foreach (var beat in beatNodes)
            {
                if (beat == null)
                {
                    continue;
                }

                builder.Append("  - nodeId: ");
                builder.AppendLine(EscapeLine(beat.NodeId));
                AppendKeyValue(builder, "    availabilityType", beat.AvailabilityType);
                AppendKeyValue(builder, "    truthStatus", beat.TruthStatus);
                AppendKeyValue(builder, "    triggerType", beat.TriggerType);
                AppendKeyValue(builder, "    triggerId", beat.TriggerId);
                AppendKeyValue(builder, "    triggerParentId", beat.TriggerParentId);
                AppendKeyValue(builder, "    triggerIntent", beat.TriggerIntent);
                AppendKeyValue(builder, "    triggerPromptLabel", beat.TriggerPromptLabel);
                AppendKeyValue(builder, "    text", beat.Text);
                AppendKeyValue(builder, "    unlockStatementId", beat.UnlockStatementId);
                AppendKeyValue(builder, "    caughtLieId", beat.CaughtLieId);
                AppendKeyValue(builder, "    isLie", beat.IsLie.ToString());
                AppendKeyValue(builder, "    isVisited", beat.IsVisited.ToString());
                AppendKeyValue(builder, "    isUnlockable", beat.IsUnlockable.ToString());
                AppendStringList(builder, "    behavior", beat.Behavior);
                AppendStringList(builder, "    requiredIds", beat.RequiredIds);
                AppendStringList(builder, "    nextSuggestedNodeIds", beat.NextSuggestedNodeIds);
            }
        }

        private static void AppendUnlockedState(StringBuilder builder, DialogueApiPromptContext context)
        {
            builder.AppendLine("RUNTIME UNLOCKED STATE");
            AppendStringList(builder, "unlockedFactIds", context.RelevantUnlockedFactIds);
            AppendStringList(builder, "unlockedStatementIds", context.RelevantUnlockedStatementIds);
            AppendStringList(builder, "unlockedLayerIds", context.RelevantUnlockedLayerIds);
            AppendStringList(builder, "visitedBeatIds", context.RelevantVisitedBeatIds);
            AppendStringList(builder, "caughtLieIds", context.RelevantCaughtLieIds);
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

        private static void AppendConversationSummary(StringBuilder builder, string summary)
        {
            builder.AppendLine("CONVERSATION SUMMARY");
            builder.AppendLine(string.IsNullOrWhiteSpace(summary) ? "none" : summary);
            builder.AppendLine();
        }

        private static void AppendOpeningContextSummary(StringBuilder builder, string summary)
        {
            builder.AppendLine("OPENING CONTEXT SUMMARY");
            builder.AppendLine(string.IsNullOrWhiteSpace(summary) ? "无" : summary);
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
            builder.AppendLine("If topicId is a safeRoleplayTopic or openFallbackTopic, usedBeatId and usedStatementId must be empty strings and usedRevealIds must be [].");
            builder.AppendLine("If you use an authored beat or statement, choose the authored topicId that owns it.");
            builder.AppendLine("{");
            builder.AppendLine("  \"interpretation\": {");
            builder.AppendLine("    \"topicId\": \"candidate_topic_id_or_irrelevant\",");
            builder.AppendLine("    \"confidence\": 0.0,");
            builder.AppendLine("    \"isIrrelevant\": false");
            builder.AppendLine("  },");
            builder.AppendLine("  \"response\": {");
            builder.AppendLine("    \"prose\": \"short in-character Chinese NPC response\",");
            builder.AppendLine("    \"usedBeatId\": \"\",");
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
