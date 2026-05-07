using System;
using System.Text;
using DetectiveGame.Core;
using DetectiveGame.Gameplay.Dialogue;
using UnityEngine;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class DialogueTurnResolverDebugInput : MonoBehaviour
    {
        [SerializeField] private KeyCode resolveKey = KeyCode.Y;
        [SerializeField] private string npcId = "npc_1";
        [SerializeField] private GamePhase phase = GamePhase.Exploration;
        [SerializeField] private string matchedTopicId = "lin_discovery_time";
        [SerializeField] private DialogueActionType actionType = DialogueActionType.AskInfo;
        [SerializeField] private string rawPlayerText = "When did you find the body?";
        [SerializeField] private string presentedEvidenceId = string.Empty;
        [SerializeField] private string[] setupProgressTokens =
        {
            "talk_lin_intro"
        };

        private AppRoot appRoot;
        private DialogueTurnResolver resolver;

        private void Awake()
        {
            appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                throw new InvalidOperationException("DialogueTurnResolverDebugInput requires AppRoot.Instance.");
            }

            if (string.IsNullOrWhiteSpace(npcId))
            {
                throw new InvalidOperationException("DialogueTurnResolverDebugInput requires an npcId.");
            }

            if (string.IsNullOrWhiteSpace(matchedTopicId))
            {
                throw new InvalidOperationException("DialogueTurnResolverDebugInput requires a matchedTopicId.");
            }

            resolver = new DialogueTurnResolver();
        }

        private void Update()
        {
            if (Input.GetKeyDown(resolveKey))
            {
                ResolveTurn();
            }
        }

        private void ResolveTurn()
        {
            UnlockSetupTokens();

            var rawInput = new RawDialogueInput
            {
                NpcId = npcId,
                Phase = phase,
                RawPlayerText = rawPlayerText,
                PresentedEvidenceId = presentedEvidenceId,
            };

            var interpretedAction = new InterpretedDialogueAction
            {
                NpcId = npcId,
                Phase = phase,
                MatchedTopicId = matchedTopicId,
                ActionType = actionType,
                PresentedEvidenceId = presentedEvidenceId,
                Confidence = 1f,
                IsIrrelevant = false,
            };

            var context = resolver.Resolve(
                rawInput,
                interpretedAction,
                appRoot.DatabaseManager,
                appRoot.ProgressManager,
                appRoot.NpcRuntimeManager);

            Debug.Log(BuildReport(context), this);
        }

        private void UnlockSetupTokens()
        {
            foreach (var tokenId in setupProgressTokens ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(tokenId))
                {
                    appRoot.ProgressManager.UnlockProgressToken(tokenId);
                }
            }
        }

        private static string BuildReport(DialogueTurnContext context)
        {
            var report = new StringBuilder();
            report.AppendLine("=== DIALOGUE TURN RESOLVER REPORT ===");
            report.AppendLine($"NPC: {context.NpcId}");
            report.AppendLine($"Phase: {context.Phase}");
            report.AppendLine($"Matched Topic: {context.InterpretedAction.MatchedTopicId}");
            report.AppendLine($"Resolution Type: {context.ResolutionResult.ResolutionType}");
            report.AppendLine($"Annoyance Delta: {context.ResolutionResult.AnnoyanceDelta}");
            report.AppendLine($"Annoyance After: {context.ResolutionResult.NewAnnoyance}");
            AppendList(report, "Unlocked Statements", context.ResolutionResult.UnlockedStatementIds);
            AppendList(report, "Unlocked Facts", context.ResolutionResult.UnlockedFactIds);
            AppendList(report, "Unlocked Tokens", context.ResolutionResult.UnlockedTokenIds);
            AppendList(report, "Unlocked Layers", context.ResolutionResult.UnlockedLayerIds);
            report.AppendLine($"Punish Reason: {context.ResolutionResult.PunishReason}");
            return report.ToString();
        }

        private static void AppendList(StringBuilder report, string label, System.Collections.Generic.IReadOnlyList<string> values)
        {
            report.Append(label);
            report.Append(": ");

            if (values == null || values.Count == 0)
            {
                report.AppendLine("None");
                return;
            }

            report.AppendLine(string.Join(", ", values));
        }
    }
}
