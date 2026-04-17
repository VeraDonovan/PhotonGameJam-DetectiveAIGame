using System;
using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;
using UnityEngine;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class CaseDatabaseDebugReport : MonoBehaviour
    {
        [SerializeField] private KeyCode printReportKey = KeyCode.R;
        [SerializeField] private bool printOnStart;

        private AppRoot appRoot;

        private void Awake()
        {
            appRoot = AppRoot.Instance;

            if (appRoot == null)
            {
                throw new InvalidOperationException("CaseDatabaseDebugReport requires AppRoot.Instance.");
            }
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
            var databaseManager = appRoot.DatabaseManager;
            var errors = new List<string>();
            var report = new StringBuilder();

            report.AppendLine("=== CASE DATABASE REPORT ===");
            AppendLoadSummary(report, databaseManager);
            AppendMissingReferenceReport(report, errors, databaseManager);
            AppendEvidenceChainReport(report, databaseManager);
            AppendNpcTruthCoverageReport(report, databaseManager);
            AppendEndingExplainabilityReport(report, databaseManager);

            if (errors.Count == 0)
            {
                Debug.Log(report.ToString(), this);
                return;
            }

            Debug.LogWarning(report.ToString(), this);
        }

        private static void AppendLoadSummary(StringBuilder report, DatabaseManager databaseManager)
        {
            report.AppendLine();
            report.AppendLine("[Load Summary]");
            report.AppendLine($"Evidence: {databaseManager.EvidenceDatabase.EvidenceById.Count}");
            report.AppendLine($"Facts: {databaseManager.FactDatabase.FactById.Count}");
            report.AppendLine($"NPCs: {databaseManager.NpcDatabase.NpcById.Count}");
            report.AppendLine($"NPC Truths: {databaseManager.TruthDatabase.NpcTruthByNpcId.Count}");
            report.AppendLine($"Dialogue Triggers: {databaseManager.TruthDatabase.DialogueTriggerById.Count}");
            report.AppendLine($"Interrogation Layers: {databaseManager.TruthDatabase.InterrogationLayerById.Count}");
            report.AppendLine($"Rooms: {databaseManager.SceneDatabase.RoomById.Count}");
            report.AppendLine($"Scene Objects: {databaseManager.SceneDatabase.ObjectById.Count}");
            report.AppendLine($"Endings: {databaseManager.EndingDatabase.EndingById.Count}");
        }

        private static void AppendMissingReferenceReport(
            StringBuilder report,
            List<string> errors,
            DatabaseManager databaseManager)
        {
            ValidateEvidenceReferences(errors, databaseManager);
            ValidateFactReferences(errors, databaseManager);
            ValidateNpcTruthReferences(errors, databaseManager);
            ValidateSceneReferences(errors, databaseManager);
            ValidateEndingReferences(errors, databaseManager);

            report.AppendLine();
            report.AppendLine("[Missing References]");

            if (errors.Count == 0)
            {
                report.AppendLine("None");
                return;
            }

            foreach (var error in errors)
            {
                report.AppendLine($"- {error}");
            }
        }

        private static void ValidateEvidenceReferences(List<string> errors, DatabaseManager databaseManager)
        {
            foreach (var evidence in databaseManager.EvidenceDatabase.EvidenceById.Values)
            {
                foreach (var requirementId in evidence.requirements ?? new List<string>())
                {
                    if (!databaseManager.EvidenceDatabase.EvidenceById.ContainsKey(requirementId))
                    {
                        errors.Add($"Evidence '{evidence.evidenceId}' requires missing evidence '{requirementId}'.");
                    }
                }
            }

            foreach (var relationship in databaseManager.EvidenceDatabase.RelationshipsBySourceId.Values)
            {
                foreach (var entry in relationship)
                {
                    if (!databaseManager.EvidenceDatabase.EvidenceById.ContainsKey(entry.fromId))
                    {
                        errors.Add($"Evidence relationship has missing fromId '{entry.fromId}'.");
                    }

                    if (!databaseManager.EvidenceDatabase.EvidenceById.ContainsKey(entry.toId))
                    {
                        errors.Add($"Evidence relationship has missing toId '{entry.toId}'.");
                    }
                }
            }
        }

        private static void ValidateFactReferences(List<string> errors, DatabaseManager databaseManager)
        {
            foreach (var fact in databaseManager.FactDatabase.FactById.Values)
            {
                foreach (var requirementId in fact.unlock?.requirementsAll ?? new List<string>())
                {
                    ValidateRequirementId(errors, databaseManager, $"Fact '{fact.factId}' requirementsAll", requirementId);
                }

                foreach (var requirementId in fact.unlock?.requirementsAny ?? new List<string>())
                {
                    ValidateRequirementId(errors, databaseManager, $"Fact '{fact.factId}' requirementsAny", requirementId);
                }

                foreach (var evidenceId in fact.unlock?.sourceEvidenceIds ?? new List<string>())
                {
                    if (!databaseManager.EvidenceDatabase.EvidenceById.ContainsKey(evidenceId))
                    {
                        errors.Add($"Fact '{fact.factId}' references missing source evidence '{evidenceId}'.");
                    }
                }

                foreach (var dialogueId in fact.unlock?.sourceDialogueIds ?? new List<string>())
                {
                    if (!databaseManager.TruthDatabase.InterrogationLayerById.ContainsKey(dialogueId) &&
                        !databaseManager.TruthDatabase.DialogueTriggerById.ContainsKey(dialogueId))
                    {
                        errors.Add($"Fact '{fact.factId}' references missing source dialogue/layer '{dialogueId}'.");
                    }
                }

                foreach (var sourceFactId in fact.unlock?.sourceFactIds ?? new List<string>())
                {
                    if (!databaseManager.FactDatabase.FactById.ContainsKey(sourceFactId))
                    {
                        errors.Add($"Fact '{fact.factId}' references missing source fact '{sourceFactId}'.");
                    }
                }

                foreach (var unlockedFactId in fact.progression?.unlocksFactIds ?? new List<string>())
                {
                    if (!databaseManager.FactDatabase.FactById.ContainsKey(unlockedFactId))
                    {
                        errors.Add($"Fact '{fact.factId}' unlocks missing fact '{unlockedFactId}'.");
                    }
                }

                foreach (var supportedEndingId in fact.progression?.supportsEndingIds ?? new List<string>())
                {
                    if (!databaseManager.EndingDatabase.EndingById.ContainsKey(supportedEndingId))
                    {
                        errors.Add($"Fact '{fact.factId}' supports missing ending '{supportedEndingId}'.");
                    }
                }

                foreach (var npcId in fact.scope?.relatedNpcIds ?? new List<string>())
                {
                    if (!IsKnownNpcOrSpecialEntity(databaseManager, npcId))
                    {
                        errors.Add($"Fact '{fact.factId}' relates to missing npc/entity '{npcId}'.");
                    }
                }

                foreach (var locationId in fact.scope?.relatedLocationIds ?? new List<string>())
                {
                    if (!databaseManager.SceneDatabase.RoomById.ContainsKey(locationId))
                    {
                        errors.Add($"Fact '{fact.factId}' relates to missing location '{locationId}'.");
                    }
                }
            }
        }

        private static void ValidateNpcTruthReferences(List<string> errors, DatabaseManager databaseManager)
        {
            foreach (var npcTruth in databaseManager.TruthDatabase.NpcTruthByNpcId.Values)
            {
                if (!databaseManager.NpcDatabase.NpcById.ContainsKey(npcTruth.npcId))
                {
                    errors.Add($"Truth references missing public NPC '{npcTruth.npcId}'.");
                }

                foreach (var trigger in npcTruth.dialogueTriggers ?? new List<DialogueTriggerData>())
                {
                    foreach (var requirementId in trigger.unlockRequirements ?? new List<string>())
                    {
                        ValidateRequirementId(errors, databaseManager, $"Dialogue trigger '{trigger.triggerId}'", requirementId);
                    }
                }
            }

            foreach (var deductionTruth in databaseManager.TruthDatabase.DeductionTruthById.Values)
            {
                foreach (var factId in deductionTruth.requiresFactIds ?? new List<string>())
                {
                    if (!databaseManager.FactDatabase.FactById.ContainsKey(factId))
                    {
                        errors.Add($"Deduction truth '{deductionTruth.truthId}' requires missing fact '{factId}'.");
                    }
                }
            }
        }

        private static void ValidateSceneReferences(List<string> errors, DatabaseManager databaseManager)
        {
            foreach (var sceneObject in databaseManager.SceneDatabase.ObjectById.Values)
            {
                foreach (var evidenceId in sceneObject.hiddenEvidenceIds ?? new List<string>())
                {
                    if (!databaseManager.EvidenceDatabase.EvidenceById.ContainsKey(evidenceId))
                    {
                        errors.Add($"Scene object '{sceneObject.objectId}' hides missing evidence '{evidenceId}'.");
                    }
                }
            }
        }

        private static void ValidateEndingReferences(List<string> errors, DatabaseManager databaseManager)
        {
            foreach (var ending in databaseManager.EndingDatabase.EndingById.Values)
            {
                foreach (var factId in ending.requirements?.requiredFactIds ?? new List<string>())
                {
                    if (!databaseManager.FactDatabase.FactById.ContainsKey(factId))
                    {
                        errors.Add($"Ending '{ending.endingId}' requires missing fact '{factId}'.");
                    }
                }

                foreach (var factId in ending.requirements?.requiredAnyFactIds ?? new List<string>())
                {
                    if (!databaseManager.FactDatabase.FactById.ContainsKey(factId))
                    {
                        errors.Add($"Ending '{ending.endingId}' requires missing any-fact '{factId}'.");
                    }
                }

                foreach (var evidenceId in ending.requirements?.requiredEvidenceIds ?? new List<string>())
                {
                    if (!databaseManager.EvidenceDatabase.EvidenceById.ContainsKey(evidenceId))
                    {
                        errors.Add($"Ending '{ending.endingId}' requires missing evidence '{evidenceId}'.");
                    }
                }

                foreach (var layerId in ending.requirements?.requiredNpcLayerIds ?? new List<string>())
                {
                    if (!databaseManager.TruthDatabase.InterrogationLayerById.ContainsKey(layerId))
                    {
                        errors.Add($"Ending '{ending.endingId}' requires missing interrogation layer '{layerId}'.");
                    }
                }
            }
        }

        private static void ValidateRequirementId(
            List<string> errors,
            DatabaseManager databaseManager,
            string owner,
            string requirementId)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
            {
                errors.Add($"{owner} contains a blank requirement id.");
                return;
            }

            if (databaseManager.EvidenceDatabase.EvidenceById.ContainsKey(requirementId) ||
                databaseManager.FactDatabase.FactById.ContainsKey(requirementId) ||
                databaseManager.TruthDatabase.InterrogationLayerById.ContainsKey(requirementId) ||
                databaseManager.TruthDatabase.DialogueTriggerById.ContainsKey(requirementId))
            {
                return;
            }

            errors.Add($"{owner} references missing requirement '{requirementId}'.");
        }

        private static void AppendEvidenceChainReport(StringBuilder report, DatabaseManager databaseManager)
        {
            report.AppendLine();
            report.AppendLine("[Evidence Unlock Chains]");

            foreach (var evidence in databaseManager.EvidenceDatabase.EvidenceById.Values)
            {
                var dependents = FindEvidenceDependents(databaseManager, evidence.evidenceId);
                var unlockedFacts = FindFactsRequiringId(databaseManager, evidence.evidenceId);

                if (dependents.Count == 0 && unlockedFacts.Count == 0)
                {
                    continue;
                }

                report.Append($"- {evidence.evidenceId}");
                AppendNamedValue(report, evidence.displayName);

                if (dependents.Count > 0)
                {
                    report.Append($" -> evidence [{string.Join(", ", dependents)}]");
                }

                if (unlockedFacts.Count > 0)
                {
                    report.Append($" -> facts [{string.Join(", ", unlockedFacts)}]");
                }

                report.AppendLine();
            }
        }

        private static void AppendNpcTruthCoverageReport(StringBuilder report, DatabaseManager databaseManager)
        {
            report.AppendLine();
            report.AppendLine("[NPC Truth Coverage]");

            foreach (var npc in databaseManager.NpcDatabase.NpcById.Values)
            {
                var hasTruth = databaseManager.TruthDatabase.TryGetNpcTruth(npc.npcId, out var npcTruth);
                var triggerCount = hasTruth ? databaseManager.TruthDatabase.GetDialogueTriggersByNpc(npc.npcId).Count : 0;
                var layerCount = hasTruth ? databaseManager.TruthDatabase.GetInterrogationLayersByNpc(npc.npcId).Count : 0;

                report.AppendLine($"- {npc.npcId} {npc.displayName}");
                report.AppendLine($"  publicProfile: {ToYesNo(!string.IsNullOrWhiteSpace(npc.profileText))}");
                report.AppendLine($"  truth: {ToYesNo(hasTruth)}");
                report.AppendLine($"  dialogueTriggers: {triggerCount}");
                report.AppendLine($"  interrogationLayers: {layerCount}");

                if (hasTruth)
                {
                    report.AppendLine($"  isKiller: {npcTruth.isKiller}");
                    report.AppendLine($"  motivePresent: {ToYesNo(!string.IsNullOrWhiteSpace(npcTruth.realMotive))}");
                }
            }
        }

        private static void AppendEndingExplainabilityReport(StringBuilder report, DatabaseManager databaseManager)
        {
            report.AppendLine();
            report.AppendLine("[Ending Explainability]");

            foreach (var ending in databaseManager.EndingDatabase.EndingById.Values)
            {
                report.AppendLine($"- {ending.endingId} {ending.displayName}");

                AppendResolvedFactList(
                    report,
                    databaseManager,
                    "requiredFacts",
                    ending.requirements?.requiredFactIds ?? new List<string>());

                AppendResolvedLayerList(
                    report,
                    databaseManager,
                    "requiredLayers",
                    ending.requirements?.requiredNpcLayerIds ?? new List<string>());

                AppendResolvedFactList(
                    report,
                    databaseManager,
                    "requiredAnyFacts",
                    ending.requirements?.requiredAnyFactIds ?? new List<string>());
            }
        }

        private static List<string> FindEvidenceDependents(DatabaseManager databaseManager, string evidenceId)
        {
            var dependents = new List<string>();

            foreach (var evidence in databaseManager.EvidenceDatabase.EvidenceById.Values)
            {
                if (evidence.requirements != null && evidence.requirements.Contains(evidenceId))
                {
                    dependents.Add(evidence.evidenceId);
                }
            }

            return dependents;
        }

        private static List<string> FindFactsRequiringId(DatabaseManager databaseManager, string requirementId)
        {
            var factIds = new List<string>();

            foreach (var fact in databaseManager.FactDatabase.FactById.Values)
            {
                if ((fact.unlock?.requirementsAll != null && fact.unlock.requirementsAll.Contains(requirementId)) ||
                    (fact.unlock?.requirementsAny != null && fact.unlock.requirementsAny.Contains(requirementId)) ||
                    (fact.unlock?.sourceEvidenceIds != null && fact.unlock.sourceEvidenceIds.Contains(requirementId)) ||
                    (fact.unlock?.sourceDialogueIds != null && fact.unlock.sourceDialogueIds.Contains(requirementId)) ||
                    (fact.unlock?.sourceFactIds != null && fact.unlock.sourceFactIds.Contains(requirementId)))
                {
                    factIds.Add(fact.factId);
                }
            }

            return factIds;
        }

        private static void AppendResolvedFactList(
            StringBuilder report,
            DatabaseManager databaseManager,
            string label,
            IEnumerable<string> factIds)
        {
            report.Append($"  {label}: ");

            var hasAny = false;
            foreach (var factId in factIds)
            {
                hasAny = true;
                if (databaseManager.FactDatabase.TryGetFact(factId, out var fact))
                {
                    report.Append($"{factId}({fact.displayName}); ");
                    continue;
                }

                report.Append($"{factId}(MISSING); ");
            }

            report.AppendLine(hasAny ? string.Empty : "None");
        }

        private static void AppendResolvedLayerList(
            StringBuilder report,
            DatabaseManager databaseManager,
            string label,
            IEnumerable<string> layerIds)
        {
            report.Append($"  {label}: ");

            var hasAny = false;
            foreach (var layerId in layerIds)
            {
                hasAny = true;
                if (databaseManager.TruthDatabase.TryGetInterrogationLayer(layerId, out var layer))
                {
                    report.Append($"{layerId}({layer.topic}); ");
                    continue;
                }

                report.Append($"{layerId}(MISSING); ");
            }

            report.AppendLine(hasAny ? string.Empty : "None");
        }

        private static bool IsKnownNpcOrSpecialEntity(DatabaseManager databaseManager, string npcId)
        {
            return databaseManager.NpcDatabase.NpcById.ContainsKey(npcId) ||
                   string.Equals(npcId, "victim", StringComparison.Ordinal);
        }

        private static void AppendNamedValue(StringBuilder report, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                report.Append($" ({value})");
            }
        }

        private static string ToYesNo(bool value)
        {
            return value ? "yes" : "no";
        }
    }
}
