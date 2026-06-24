#if UNITY_EDITOR
using System;
using System.Text;
using DetectiveGame.Core;
using UnityEditor;
using UnityEngine;

namespace DetectiveGame.Gameplay.Dialogue.Editor
{
    public static class DialogueConversationCompressionVerifier
    {
        private const string MenuPath = "Tools/Dialogue/Run Compression Verifier";

        [MenuItem(MenuPath)]
        public static void RunFromMenu()
        {
            int failures = RunAll();
            if (failures == 0)
            {
                Debug.Log("[CompressionVerifier] All checks passed.");
            }
            else
            {
                Debug.LogError($"[CompressionVerifier] {failures} check(s) failed. See log above.");
            }
        }

        public static int RunAll()
        {
            int failures = 0;
            failures += RunFoldBatchTests();
            failures += RunSessionStorageTests();
            failures += RunHistorySelectorTests();
            failures += RunLagPromoteTests();
            failures += RunPromptSectionTests();
            return failures;
        }

        private static int RunFoldBatchTests()
        {
            int failures = 0;
            int savedK = DialogueConversationConfig.RecentVerbatimExchangeCount;
            int savedBatch = DialogueConversationConfig.TurnSummaryBatchSize;

            try
            {
                DialogueConversationConfig.RecentVerbatimExchangeCount = 3;
                DialogueConversationConfig.TurnSummaryBatchSize = 1;

                for (int turn = 1; turn <= 8; turn++)
                {
                    var session = BuildSessionWithExchangeCount(turn);
                    bool shouldFold = DialogueConversationSummarizer.TryGetTurnFoldBatch(
                        session,
                        out int startIndex,
                        out int batchSize);

                    bool expectedFold = turn >= 4;
                    if (shouldFold != expectedFold)
                    {
                        LogFail($"k=3 batch=1 turn={turn}: fold={shouldFold}, expected={expectedFold}");
                        failures++;
                        continue;
                    }

                    if (!shouldFold)
                    {
                        continue;
                    }

                    int expectedStart = turn - 4;
                    if (startIndex != expectedStart || batchSize != 1)
                    {
                        LogFail($"k=3 batch=1 turn={turn}: start={startIndex} batch={batchSize}, expected start={expectedStart} batch=1");
                        failures++;
                    }
                }

                DialogueConversationConfig.TurnSummaryBatchSize = 3;
                for (int turn = 1; turn <= 9; turn++)
                {
                    var session = BuildSessionWithExchangeCount(turn);
                    if (turn > 6)
                    {
                        session.AdvanceSummarizedExchangeCount(3);
                    }

                    bool shouldFold = DialogueConversationSummarizer.TryGetTurnFoldBatch(
                        session,
                        out int startIndex,
                        out int batchSize);

                    bool expectedFold = turn == 6 || turn == 9;
                    if (shouldFold != expectedFold)
                    {
                        LogFail($"k=3 batch=3 turn={turn}: fold={shouldFold}, expected={expectedFold}");
                        failures++;
                        continue;
                    }

                    if (turn == 6 && (startIndex != 0 || batchSize != 3))
                    {
                        LogFail($"k=3 batch=3 turn=6: start={startIndex} batch={batchSize}");
                        failures++;
                    }

                    if (turn == 9 && (startIndex != 3 || batchSize != 3))
                    {
                        LogFail($"k=3 batch=3 turn=9: start={startIndex} batch={batchSize}");
                        failures++;
                    }
                }
            }
            finally
            {
                DialogueConversationConfig.RecentVerbatimExchangeCount = savedK;
                DialogueConversationConfig.TurnSummaryBatchSize = savedBatch;
            }

            return failures;
        }

        private static int RunSessionStorageTests()
        {
            int failures = 0;
            var session = new DialogueConversationSession("npc_test");

            for (int i = 0; i < 20; i++)
            {
                session.AddExchange($"p{i}", $"n{i}");
            }

            if (session.Exchanges.Count != 20)
            {
                LogFail($"Session should retain all exchanges, got {session.Exchanges.Count}");
                failures++;
            }

            return failures;
        }

        private static int RunHistorySelectorTests()
        {
            int failures = 0;
            int savedK = DialogueConversationConfig.RecentVerbatimExchangeCount;
            int savedOpening = DialogueConversationConfig.OpeningVerbatimExchangeCount;

            try
            {
                DialogueConversationConfig.RecentVerbatimExchangeCount = 3;
                DialogueConversationConfig.OpeningVerbatimExchangeCount = 0;

                var session = BuildSessionWithExchangeCount(10);
                var selector = new DialogueConversationHistorySelector();

                int turnCount = selector.SelectForApi(session, DialoguePromptMode.Turn).Count;
                if (turnCount != 3)
                {
                    LogFail($"Turn verbatim window: expected 3, got {turnCount}");
                    failures++;
                }

                int openingCount = selector.SelectForApi(session, DialoguePromptMode.Opening).Count;
                if (openingCount != 0)
                {
                    LogFail($"Opening verbatim default: expected 0, got {openingCount}");
                    failures++;
                }

                DialogueConversationConfig.OpeningVerbatimExchangeCount = 1;
                openingCount = selector.SelectForApi(session, DialoguePromptMode.Opening).Count;
                if (openingCount != 1)
                {
                    LogFail($"Opening verbatim=1: expected 1, got {openingCount}");
                    failures++;
                }
            }
            finally
            {
                DialogueConversationConfig.RecentVerbatimExchangeCount = savedK;
                DialogueConversationConfig.OpeningVerbatimExchangeCount = savedOpening;
            }

            return failures;
        }

        private static int RunLagPromoteTests()
        {
            int failures = 0;
            var session = new DialogueConversationSession("npc_lag");

            session.SetPendingTurnSummary("summary_after_turn_4");
            if (!string.IsNullOrEmpty(session.ActiveTurnSummary))
            {
                LogFail("ActiveTurnSummary should stay empty before promote");
                failures++;
            }

            session.PromotePendingTurnSummaryIfAny();
            if (session.ActiveTurnSummary != "summary_after_turn_4" || !string.IsNullOrEmpty(session.PendingTurnSummary))
            {
                LogFail("Promote should move PendingTurnSummary to Active and clear Pending");
                failures++;
            }

            session.SetPendingTurnSummary("summary_after_turn_5");
            if (session.ActiveTurnSummary != "summary_after_turn_4")
            {
                LogFail("Active should remain until next promote");
                failures++;
            }

            session.PromotePendingTurnSummaryIfAny();
            if (session.ActiveTurnSummary != "summary_after_turn_5")
            {
                LogFail("Second promote should replace Active with latest Pending");
                failures++;
            }

            session.SetPendingOpeningSummary("opening_v2");
            session.PromotePendingOpeningSummaryIfAny();
            if (session.ActiveOpeningSummary != "opening_v2")
            {
                LogFail("Opening promote failed");
                failures++;
            }

            return failures;
        }

        private static int RunPromptSectionTests()
        {
            int failures = 0;
            var builder = new DialoguePromptBuilder();
            var context = new DialogueApiPromptContext
            {
                NpcId = "npc_test",
                Phase = GamePhase.Exploration,
                RawInput = new RawDialogueInput { RawPlayerText = "测试" },
                TurnConversationSummary = "旧对话要点",
                OpeningContextSummary = "开场延续要点",
            };
            context.RecentConversation.Add(new DialogueConversationExchange
            {
                PlayerText = "你好",
                NpcText = "什么事？",
            });

            var sections = new DialoguePromptSections
            {
                DialogueBasePrompt = "base",
                NpcContextRulesPrompt = "rules",
                RevealLogicRulesPrompt = "reveal",
            };

            string turnUser = builder.Build(context, sections).UserMessage;
            if (!turnUser.Contains("CONVERSATION SUMMARY") || !turnUser.Contains("旧对话要点"))
            {
                LogFail("Turn prompt missing CONVERSATION SUMMARY content");
                failures++;
            }

            string openingUser = builder.BuildOpening(context).UserMessage;
            if (!openingUser.Contains("OPENING CONTEXT SUMMARY") || !openingUser.Contains("开场延续要点"))
            {
                LogFail("Opening prompt missing OPENING CONTEXT SUMMARY content");
                failures++;
            }

            return failures;
        }

        private static DialogueConversationSession BuildSessionWithExchangeCount(int count)
        {
            var session = new DialogueConversationSession("npc_fold");
            for (int i = 0; i < count; i++)
            {
                session.AddExchange($"player_{i}", $"npc_{i}");
            }

            return session;
        }

        private static void LogFail(string message)
        {
            Debug.LogError("[CompressionVerifier] FAIL: " + message);
        }
    }
}
#endif
