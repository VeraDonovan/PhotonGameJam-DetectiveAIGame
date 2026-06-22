using System.Collections;
using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;
using UnityEngine;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueConversationSummarizer : IDialogueConversationSummarizer
    {
        private const int SummaryMaxTokens = 256;

        public static string DescribeTurnFoldSkipReason(DialogueConversationSession session)
        {
            if (session == null)
            {
                return "null_session";
            }

            int k = DialogueConversationConfig.RecentVerbatimExchangeCount;
            int count = session.Exchanges.Count;
            if (count <= k)
            {
                return $"count_le_k(count={count},k={k})";
            }

            int overflowEnd = count - k - 1;
            int pendingCount = overflowEnd - session.SummarizedExchangeCount + 1;
            int configuredBatch = DialogueConversationConfig.TurnSummaryBatchSize;
            if (configuredBatch <= 0)
            {
                return $"invalid_batch({configuredBatch})";
            }

            if (pendingCount < configuredBatch)
            {
                return $"pending_lt_batch(pending={pendingCount},batch={configuredBatch})";
            }

            int startIndex = session.SummarizedExchangeCount;
            if (startIndex + configuredBatch - 1 > overflowEnd)
            {
                return $"batch_out_of_range(start={startIndex},batch={configuredBatch},overflowEnd={overflowEnd})";
            }

            return "ready_to_fold";
        }

        public IEnumerator MaybeSummarizeTurnOverflow(DialogueConversationSession session)
        {
            if (session == null)
            {
                yield break;
            }

            if (!TryGetTurnFoldBatch(session, out int startIndex, out int batchSize))
            {
                DialogueHistoryCompressionLogger.LogTurnFoldSkipped(
                    session,
                    DescribeTurnFoldSkipReason(session));
                yield break;
            }

            DialogueHistoryCompressionLogger.LogTurnFoldStart(session, startIndex, batchSize);
            float startedAt = Time.realtimeSinceStartup;

            DeepSeekDialogueClient dialogueClient = DeepSeekDialogueClient.Instance;
            if (dialogueClient == null)
            {
                Debug.LogWarning("[DialogueConversationSummarizer] Turn fold skipped: DeepSeekDialogueClient.Instance is null.");
                DialogueHistoryCompressionLogger.LogTurnFoldFailed(
                    session,
                    Time.realtimeSinceStartup - startedAt,
                    "DeepSeekDialogueClient.Instance is null");
                yield break;
            }

            string systemPrompt =
                "你在压缩中文侦探游戏里 NPC 与警察的对话历史。\n" +
                "任务：把「已有摘要」与「新滑出的对话」合并成一条更短的滚动摘要。\n" +
                "规则：\n" +
                "- 禁止发明新事实、新证据、新 unlock。\n" +
                "- 用分号分隔的要点短句，不要散文复述，不要重复 RECENT CONVERSATION 里的原文。\n" +
                "- 保留已答话题与 NPC 态度/语气线索，便于识别重复提问。\n" +
                "- 合并已有摘要时去重压缩，勿逐条扩写；总长度 80-120 字。\n" +
                "- 只输出摘要正文，不要 JSON、不要解释。";

            string userPrompt = BuildTurnFoldUserPrompt(session, startIndex, batchSize);

            string summaryText = string.Empty;
            string error = string.Empty;

            yield return dialogueClient.SendDialogueRequest(
                systemPrompt,
                userPrompt,
                response => summaryText = response,
                err => error = err,
                maxTokensOverride: SummaryMaxTokens);

            float waitSeconds = Time.realtimeSinceStartup - startedAt;
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning("[DialogueConversationSummarizer] Turn fold failed: " + error);
                DialogueHistoryCompressionLogger.LogTurnFoldFailed(session, waitSeconds, error);
                yield break;
            }

            string normalized = NormalizeSummaryText(summaryText);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                Debug.LogWarning("[DialogueConversationSummarizer] Turn fold returned empty summary.");
                DialogueHistoryCompressionLogger.LogTurnFoldFailed(session, waitSeconds, "empty_summary");
                yield break;
            }

            session.SetPendingTurnSummary(normalized);
            session.AdvanceSummarizedExchangeCount(batchSize);
            DialogueHistoryCompressionLogger.LogTurnFoldDone(
                session,
                waitSeconds,
                startIndex,
                batchSize,
                normalized);
        }

        public IEnumerator MaybeUpdateOpeningSummary(
            DialogueConversationSession session,
            DialogueConversationExchange latestExchange,
            GamePhase phase,
            int annoyance)
        {
            if (session == null || latestExchange == null)
            {
                yield break;
            }

            DialogueHistoryCompressionLogger.LogOpeningUpdateStart(session);
            float startedAt = Time.realtimeSinceStartup;

            DeepSeekDialogueClient dialogueClient = DeepSeekDialogueClient.Instance;
            if (dialogueClient == null)
            {
                Debug.LogWarning("[DialogueConversationSummarizer] Opening summary skipped: DeepSeekDialogueClient.Instance is null.");
                DialogueHistoryCompressionLogger.LogOpeningUpdateFailed(
                    session,
                    Time.realtimeSinceStartup - startedAt,
                    "DeepSeekDialogueClient.Instance is null");
                yield break;
            }

            string systemPrompt =
                "你在维护中文侦探游戏里 NPC 的「下次开场上下文」。\n" +
                "玩家可能关闭对话面板后再按 E 与同一 NPC 对话；你需要提炼 NPC 再次主动开口时应延续的信息。\n" +
                "规则：\n" +
                "- 合并上一版摘要与本回合要点，去重压缩，勿扩写为更长散文。\n" +
                "- 用分号分隔的要点短句；聚焦关系、情绪、已聊话题、NPC 态度。\n" +
                "- 禁止发明新事实；禁止复述本回合原文。\n" +
                "- 总长度 60-100 字；只输出摘要正文，不要 JSON、不要解释。";

            string userPrompt = BuildOpeningUpdateUserPrompt(session, latestExchange, phase, annoyance);

            string summaryText = string.Empty;
            string error = string.Empty;

            yield return dialogueClient.SendDialogueRequest(
                systemPrompt,
                userPrompt,
                response => summaryText = response,
                err => error = err,
                maxTokensOverride: SummaryMaxTokens);

            float waitSeconds = Time.realtimeSinceStartup - startedAt;
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning("[DialogueConversationSummarizer] Opening summary update failed: " + error);
                DialogueHistoryCompressionLogger.LogOpeningUpdateFailed(session, waitSeconds, error);
                yield break;
            }

            string normalized = NormalizeSummaryText(summaryText);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                Debug.LogWarning("[DialogueConversationSummarizer] Opening summary update returned empty text.");
                DialogueHistoryCompressionLogger.LogOpeningUpdateFailed(session, waitSeconds, "empty_summary");
                yield break;
            }

            session.SetPendingOpeningSummary(normalized);
            DialogueHistoryCompressionLogger.LogOpeningUpdateDone(session, waitSeconds, normalized);
        }

        public static bool TryGetTurnFoldBatch(
            DialogueConversationSession session,
            out int startIndex,
            out int batchSize)
        {
            startIndex = 0;
            batchSize = 0;

            if (session == null)
            {
                return false;
            }

            int k = DialogueConversationConfig.RecentVerbatimExchangeCount;
            int count = session.Exchanges.Count;
            if (count <= k)
            {
                return false;
            }

            int overflowEnd = count - k - 1;
            int pendingCount = overflowEnd - session.SummarizedExchangeCount + 1;
            int configuredBatch = DialogueConversationConfig.TurnSummaryBatchSize;
            if (configuredBatch <= 0 || pendingCount < configuredBatch)
            {
                return false;
            }

            startIndex = session.SummarizedExchangeCount;
            batchSize = configuredBatch;
            if (startIndex + batchSize - 1 > overflowEnd)
            {
                return false;
            }

            return true;
        }

        private static string BuildTurnFoldUserPrompt(
            DialogueConversationSession session,
            int startIndex,
            int batchSize)
        {
            var builder = new StringBuilder();
            builder.AppendLine("已有 Turn 对话摘要（可能为空）：");
            builder.AppendLine(GetLatestTurnSummaryForFold(session));
            builder.AppendLine();
            builder.AppendLine("请将以下对话 fold 进摘要（与已有摘要合并去重，总 80-120 字要点）：");

            IReadOnlyList<DialogueConversationExchange> exchanges = session.Exchanges;
            for (int i = startIndex; i < startIndex + batchSize; i++)
            {
                DialogueConversationExchange exchange = exchanges[i];
                builder.Append("- 玩家: ");
                builder.AppendLine(exchange?.PlayerText ?? string.Empty);
                builder.Append("  NPC: ");
                builder.AppendLine(exchange?.NpcText ?? string.Empty);
            }

            return builder.ToString();
        }

        private static string BuildOpeningUpdateUserPrompt(
            DialogueConversationSession session,
            DialogueConversationExchange latestExchange,
            GamePhase phase,
            int annoyance)
        {
            var builder = new StringBuilder();
            builder.AppendLine("上一版 Opening 开场上下文摘要（可能为空）：");
            builder.AppendLine(GetLatestOpeningSummaryForUpdate(session));
            builder.AppendLine();
            builder.Append("阶段: ");
            builder.AppendLine(phase.ToString());
            builder.Append("烦扰值: ");
            builder.AppendLine(annoyance.ToString());
            builder.AppendLine("本回合刚结束的对话（提炼要点，勿复述原文）：");
            builder.Append("- 玩家: ");
            builder.AppendLine(latestExchange.PlayerText ?? string.Empty);
            builder.Append("- NPC: ");
            builder.AppendLine(latestExchange.NpcText ?? string.Empty);
            return builder.ToString();
        }

        private static string GetLatestTurnSummaryForFold(DialogueConversationSession session)
        {
            if (!string.IsNullOrWhiteSpace(session.PendingTurnSummary))
            {
                return session.PendingTurnSummary;
            }

            return string.IsNullOrWhiteSpace(session.ActiveTurnSummary)
                ? "无"
                : session.ActiveTurnSummary;
        }

        private static string GetLatestOpeningSummaryForUpdate(DialogueConversationSession session)
        {
            if (!string.IsNullOrWhiteSpace(session.PendingOpeningSummary))
            {
                return session.PendingOpeningSummary;
            }

            return string.IsNullOrWhiteSpace(session.ActiveOpeningSummary)
                ? "无"
                : session.ActiveOpeningSummary;
        }

        private static string NormalizeSummaryText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return string.Empty;
            }

            string text = rawText.Trim();
            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
            {
                text = text.Substring(1, text.Length - 2).Trim();
            }

            return text;
        }
    }
}
