using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DetectiveGame.Gameplay.Dialogue
{
    public static class DialogueHistoryCompressionLogger
    {
        public const string LogPrefix = "[DialogueHistory]";
        private static bool isEnabled = false;
        private static string logFilePath;

        public static bool IsEnabled => isEnabled;

        public static void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public static void LogConfig(string npcId = null)
        {
            if (!isEnabled)
            {
                return;
            }

            Write(
                "SESSION_START",
                "---- new play session ----");
            Write(
                "CONFIG",
                $"k={DialogueConversationConfig.RecentVerbatimExchangeCount} " +
                $"batch={DialogueConversationConfig.TurnSummaryBatchSize} " +
                $"openingVerbatim={DialogueConversationConfig.OpeningVerbatimExchangeCount}" +
                (string.IsNullOrWhiteSpace(npcId) ? string.Empty : $" npc={npcId}"));
        }

        public static void LogTurnPromptBuilt(DialogueConversationSession session, DialogueApiPromptContext context)
        {
            if (!isEnabled || session == null || context == null)
            {
                return;
            }

            Write(
                "TURN_PROMPT",
                SessionTag(session) +
                $" exchanges={session.Exchanges.Count} summarized={session.SummarizedExchangeCount} " +
                $"verbatimInPrompt={context.RecentConversation.Count} " +
                $"activeTurnSummaryLen={LengthOrZero(session.ActiveTurnSummary)} " +
                $"pendingTurnSummaryLen={LengthOrZero(session.PendingTurnSummary)} " +
                $"hasActiveTurnSummary={!string.IsNullOrWhiteSpace(session.ActiveTurnSummary)}");
        }

        public static void LogOpeningPromptBuilt(DialogueConversationSession session, DialogueApiPromptContext context)
        {
            if (!isEnabled || session == null || context == null)
            {
                return;
            }

            Write(
                "OPENING_PROMPT",
                SessionTag(session) +
                $" exchanges={session.Exchanges.Count} " +
                $"openingVerbatimInPrompt={context.RecentConversation.Count} " +
                $"activeOpeningSummaryLen={LengthOrZero(session.ActiveOpeningSummary)} " +
                $"pendingOpeningSummaryLen={LengthOrZero(session.PendingOpeningSummary)} " +
                $"hasActiveOpeningSummary={!string.IsNullOrWhiteSpace(session.ActiveOpeningSummary)}");
        }

        public static void LogPromoteTurn(DialogueConversationSession session, bool promoted)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write(
                "PROMOTE_TURN",
                SessionTag(session) +
                $" promoted={promoted} " +
                $"activeTurnSummaryLen={LengthOrZero(session.ActiveTurnSummary)} " +
                $"pendingTurnSummaryLen={LengthOrZero(session.PendingTurnSummary)}");
        }

        public static void LogPromoteOpening(DialogueConversationSession session, bool promoted)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write(
                "PROMOTE_OPENING",
                SessionTag(session) +
                $" promoted={promoted} " +
                $"activeOpeningSummaryLen={LengthOrZero(session.ActiveOpeningSummary)} " +
                $"pendingOpeningSummaryLen={LengthOrZero(session.PendingOpeningSummary)}");
        }

        public static void LogTurnEndScheduled(DialogueConversationSession session)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            bool willFold = DialogueConversationSummarizer.TryGetTurnFoldBatch(
                session,
                out int startIndex,
                out int batchSize);

            int k = DialogueConversationConfig.RecentVerbatimExchangeCount;
            int count = session.Exchanges.Count;
            int overflowEnd = count > k ? count - k - 1 : -1;
            int pendingCount = count > k ? overflowEnd - session.SummarizedExchangeCount + 1 : 0;

            Write(
                "TURN_END",
                SessionTag(session) +
                $" exchanges={count} k={k} batch={DialogueConversationConfig.TurnSummaryBatchSize} " +
                $"summarized={session.SummarizedExchangeCount} overflowEnd={overflowEnd} pendingFold={pendingCount} " +
                $"expectedTurnFold={willFold}" +
                (willFold ? $" foldStart={startIndex} foldBatch={batchSize}" : string.Empty) +
                (!willFold ? $" skipReason={DialogueConversationSummarizer.DescribeTurnFoldSkipReason(session)}" : string.Empty) +
                " expectedOpeningUpdate=true");
        }

        public static void LogTurnFoldStart(DialogueConversationSession session, int startIndex, int batchSize)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write(
                "TURN_FOLD_START",
                SessionTag(session) +
                $" startIndex={startIndex} batch={batchSize} exchanges={session.Exchanges.Count}");
        }

        public static void LogTurnFoldDone(
            DialogueConversationSession session,
            float waitSeconds,
            int startIndex,
            int batchSize,
            string summaryPreview)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write(
                "TURN_FOLD_DONE",
                SessionTag(session) +
                $" waitMs={Mathf.RoundToInt(waitSeconds * 1000f)} " +
                $" startIndex={startIndex} batch={batchSize} " +
                $"summarizedAfter={session.SummarizedExchangeCount} " +
                $"pendingTurnSummaryLen={LengthOrZero(session.PendingTurnSummary)} " +
                $"preview={QuotePreview(summaryPreview)}");
        }

        public static void LogTurnFoldSkipped(DialogueConversationSession session, string reason)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write("TURN_FOLD_SKIP", SessionTag(session) + $" reason={reason}");
        }

        public static void LogTurnFoldFailed(DialogueConversationSession session, float waitSeconds, string error)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write(
                "TURN_FOLD_FAIL",
                SessionTag(session) +
                $" waitMs={Mathf.RoundToInt(waitSeconds * 1000f)} error={QuotePreview(error)}");
        }

        public static void LogOpeningUpdateStart(DialogueConversationSession session)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write("OPENING_UPDATE_START", SessionTag(session));
        }

        public static void LogOpeningUpdateDone(
            DialogueConversationSession session,
            float waitSeconds,
            string summaryPreview)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write(
                "OPENING_UPDATE_DONE",
                SessionTag(session) +
                $" waitMs={Mathf.RoundToInt(waitSeconds * 1000f)} " +
                $"pendingOpeningSummaryLen={LengthOrZero(session.PendingOpeningSummary)} " +
                $"preview={QuotePreview(summaryPreview)}");
        }

        public static void LogOpeningUpdateFailed(DialogueConversationSession session, float waitSeconds, string error)
        {
            if (!isEnabled || session == null)
            {
                return;
            }

            Write(
                "OPENING_UPDATE_FAIL",
                SessionTag(session) +
                $" waitMs={Mathf.RoundToInt(waitSeconds * 1000f)} error={QuotePreview(error)}");
        }

        public static string GetLogFilePath()
        {
            EnsureLogFilePath();
            return logFilePath;
        }

        private static void Write(string eventType, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {eventType} | {message}";
            Debug.Log($"{LogPrefix} {line}");

            try
            {
                EnsureLogFilePath();
                File.AppendAllText(logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{LogPrefix} Failed to write log file: {exception.Message}");
            }
        }

        private static void EnsureLogFilePath()
        {
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                return;
            }

            string logsDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            Directory.CreateDirectory(logsDirectory);
            logFilePath = Path.Combine(logsDirectory, "dialogue-history-compression.log");
        }

        private static string SessionTag(DialogueConversationSession session)
        {
            return $"npc={session.NpcId} ";
        }

        private static int LengthOrZero(string value)
        {
            return string.IsNullOrEmpty(value) ? 0 : value.Length;
        }

        private static string QuotePreview(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "\"\"";
            }

            string singleLine = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (singleLine.Length > 80)
            {
                singleLine = singleLine.Substring(0, 80) + "...";
            }

            return "\"" + singleLine + "\"";
        }
    }
}
