using System;
using System.Text;
using DetectiveGame.Core;
using DetectiveGame.Gameplay.Dialogue;
using UnityEngine;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class DialogueCandidateTopicDebugReport : MonoBehaviour
    {
        [SerializeField] private string npcId = "npc_1";
        [SerializeField] private GamePhase phase = GamePhase.Exploration;
        [SerializeField] private KeyCode printReportKey = KeyCode.T;
        [SerializeField] private bool printOnStart = true;

        private AppRoot appRoot;
        private DialogueCandidateTopicResolver resolver;

        private void Awake()
        {
            appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                throw new InvalidOperationException("DialogueCandidateTopicDebugReport requires AppRoot.Instance.");
            }

            if (string.IsNullOrWhiteSpace(npcId))
            {
                throw new InvalidOperationException("DialogueCandidateTopicDebugReport requires an npcId.");
            }

            resolver = new DialogueCandidateTopicResolver();
        }

        private void Start()
        {
            if (printOnStart)
            {
                PrintReport();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(printReportKey))
            {
                PrintReport();
            }
        }

        private void PrintReport()
        {
            var topicSet = resolver.Resolve(
                npcId,
                phase,
                appRoot.DatabaseManager,
                appRoot.ProgressManager);

            var report = new StringBuilder();
            report.AppendLine("=== DIALOGUE CANDIDATE TOPIC REPORT ===");
            report.AppendLine($"NPC: {npcId}");
            report.AppendLine($"Phase: {phase}");
            report.AppendLine($"Topic Count: {topicSet.Topics.Count}");

            foreach (var topic in topicSet.Topics)
            {
                AppendTopic(report, topic);
            }

            Debug.Log(report.ToString(), this);
        }

        private static void AppendTopic(StringBuilder report, DialogueCandidateTopic topic)
        {
            report.AppendLine();
            report.AppendLine($"[Topic] {topic.TopicId}");
            report.AppendLine($"Display Name: {topic.DisplayName}");
            report.AppendLine($"Availability: {topic.Availability}");
            report.AppendLine($"Synthetic: {topic.IsSynthetic}");
            report.AppendLine($"Search Phase Topic: {topic.IsSearchPhaseTopic}");
            report.AppendLine($"Interrogation Phase Topic: {topic.IsInterrogationPhaseTopic}");
            report.AppendLine($"Has Unlocked Statement Version: {topic.HasUnlockedStatementVersion}");
            AppendList(report, "Related Statements", topic.RelatedStatementIds);
            AppendList(report, "Related Interrogation Layers", topic.RelatedInterrogationLayerIds);
            AppendList(report, "Required Evidence", topic.RequiredEvidenceIds);
            AppendList(report, "Required Facts", topic.RequiredFactIds);
            AppendList(report, "Required Statements", topic.RequiredStatementIds);
            AppendList(report, "Required Layers", topic.RequiredInterrogationLayerIds);
            AppendList(report, "Required Tokens", topic.RequiredTokenIds);
            AppendList(report, "Missing Requirements", topic.MissingRequirementIds);
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
