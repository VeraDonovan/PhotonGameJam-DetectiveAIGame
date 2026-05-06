using System.Collections.Generic;
using System;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class ProgressManager : MonoBehaviour
    {
        private EventManager eventManager;
        private DatabaseManager databaseManager;

        public ProgressState RuntimeState { get; private set; } = new ProgressState();
        public IReadOnlyDictionary<string, bool> EvidenceCollectedById => RuntimeState.EvidenceCollectedById;
        public IReadOnlyDictionary<string, bool> FactUnlockedById => RuntimeState.FactUnlockedById;
        public IReadOnlyDictionary<string, bool> StatementUnlockedById => RuntimeState.StatementUnlockedById;
        public IReadOnlyDictionary<string, bool> InterrogationLayerUnlockedById => RuntimeState.InterrogationLayerUnlockedById;
        public IReadOnlyCollection<string> CollectedEvidenceIds => RuntimeState.CollectedEvidenceIds;
        public IReadOnlyCollection<string> UnlockedFactIds => RuntimeState.UnlockedFactIds;
        public IReadOnlyCollection<string> UnlockedStatementIds => RuntimeState.UnlockedStatementIds;
        public IReadOnlyCollection<string> UnlockedInterrogationLayerIds => RuntimeState.UnlockedInterrogationLayerIds;
        public IReadOnlyCollection<string> KnownSuspectIds => RuntimeState.KnownSuspectIds;
        public IReadOnlyCollection<string> SelectedSuspectIds => RuntimeState.SelectedSuspectIds;
        public string AccusationTargetId => RuntimeState.AccusationTargetId;

        public void Initialize(EventManager sharedEventManager, DatabaseManager sharedDatabaseManager)
        {
            eventManager = sharedEventManager;
            databaseManager = sharedDatabaseManager;
            ValidateDependencies();
            ResetRuntime();
        }

        public bool AddEvidence(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId))
            {
                Debug.LogWarning("[ProgressManager] AddEvidence rejected because evidenceId is null or whitespace.");
                return false;
            }

            if (!RuntimeState.CollectedEvidenceIds.Add(evidenceId))
            {
                Debug.Log(
                    $"[ProgressManager] AddEvidence ignored because '{evidenceId}' was already collected.");
                return false;
            }

            RuntimeState.EvidenceCollectedById[evidenceId] = true;
            Debug.Log(
                $"[ProgressManager] Added evidence '{evidenceId}'. Publishing EvidenceAddedEvent. CollectedCount={RuntimeState.CollectedEvidenceIds.Count}.");
            eventManager?.Publish(new EvidenceAddedEvent(evidenceId));
            ProcessProgressionUnlocks();
            return true;
        }

        public bool UnlockFact(string factId)
        {
            if (!TryUnlockFact(factId))
            {
                return false;
            }

            ProcessProgressionUnlocks();
            return true;
        }

        public bool UnlockStatement(string statementId)
        {
            if (!TryUnlockStatement(statementId))
            {
                return false;
            }

            ProcessProgressionUnlocks();
            return true;
        }

        public bool UnlockInterrogationLayer(string layerId)
        {
            if (!TryUnlockInterrogationLayer(layerId))
            {
                return false;
            }

            ProcessProgressionUnlocks();
            return true;
        }

        public bool UnlockProgressToken(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId) || !RuntimeState.UnlockedProgressTokens.Add(tokenId))
            {
                return false;
            }

            RuntimeState.ProgressTokenById[tokenId] = true;
            ProcessProgressionUnlocks();
            return true;
        }

        public bool IsEvidenceCollected(string evidenceId)
        {
            return !string.IsNullOrWhiteSpace(evidenceId) &&
                   RuntimeState.EvidenceCollectedById.TryGetValue(evidenceId, out var isCollected) &&
                   isCollected;
        }

        public bool IsFactUnlocked(string factId)
        {
            return !string.IsNullOrWhiteSpace(factId) &&
                   RuntimeState.FactUnlockedById.TryGetValue(factId, out var isUnlocked) &&
                   isUnlocked;
        }

        public bool IsStatementUnlocked(string statementId)
        {
            return !string.IsNullOrWhiteSpace(statementId) &&
                   RuntimeState.StatementUnlockedById.TryGetValue(statementId, out var isUnlocked) &&
                   isUnlocked;
        }

        public bool IsInterrogationLayerUnlocked(string layerId)
        {
            return !string.IsNullOrWhiteSpace(layerId) &&
                   RuntimeState.InterrogationLayerUnlockedById.TryGetValue(layerId, out var isUnlocked) &&
                   isUnlocked;
        }

        public bool IsProgressTokenUnlocked(string tokenId)
        {
            return !string.IsNullOrWhiteSpace(tokenId) &&
                   RuntimeState.ProgressTokenById.TryGetValue(tokenId, out var isUnlocked) &&
                   isUnlocked;
        }

        public bool RegisterSuspect(string suspectId)
        {
            return !string.IsNullOrWhiteSpace(suspectId) && RuntimeState.KnownSuspectIds.Add(suspectId);
        }

        public bool SelectSuspectForInterrogation(string suspectId)
        {
            if (string.IsNullOrWhiteSpace(suspectId) || !RuntimeState.KnownSuspectIds.Contains(suspectId))
            {
                return false;
            }

            return RuntimeState.SelectedSuspectIds.Add(suspectId);
        }

        public void SubmitAccusation(string suspectId)
        {
            RuntimeState.AccusationTargetId = suspectId;
        }

        public void ResetRuntime()
        {
            RuntimeState = new ProgressState();
            BuildEvidenceProgressMap();
            BuildFactProgressMap();
            BuildStatementProgressMap();
            BuildInterrogationLayerProgressMap();
            ProcessProgressionUnlocks();
        }

        private void BuildEvidenceProgressMap()
        {
            foreach (var evidenceId in databaseManager.EvidenceDatabase.EvidenceById.Keys)
            {
                RuntimeState.EvidenceCollectedById[evidenceId] = false;
            }
        }

        private void BuildFactProgressMap()
        {
            foreach (var factId in databaseManager.FactDatabase.FactById.Keys)
            {
                RuntimeState.FactUnlockedById[factId] = false;
            }
        }

        private void BuildStatementProgressMap()
        {
            foreach (var statementId in databaseManager.StatementDatabase.StatementById.Keys)
            {
                RuntimeState.StatementUnlockedById[statementId] = false;
            }
        }

        private void BuildInterrogationLayerProgressMap()
        {
            foreach (var layerId in databaseManager.TruthDatabase.InterrogationLayerById.Keys)
            {
                RuntimeState.InterrogationLayerUnlockedById[layerId] = false;
            }
        }

        private void ProcessProgressionUnlocks()
        {
            var unlockedAnyProgress = true;
            while (unlockedAnyProgress)
            {
                unlockedAnyProgress = false;

                foreach (var statementId in databaseManager.StatementDatabase.StatementById.Keys)
                {
                    if (RuntimeState.UnlockedStatementIds.Contains(statementId) || !CanUnlockStatement(statementId))
                    {
                        continue;
                    }

                    if (TryUnlockStatement(statementId))
                    {
                        unlockedAnyProgress = true;
                    }
                }

                foreach (var layerId in databaseManager.TruthDatabase.InterrogationLayerById.Keys)
                {
                    if (RuntimeState.UnlockedInterrogationLayerIds.Contains(layerId) || !CanUnlockInterrogationLayer(layerId))
                    {
                        continue;
                    }

                    if (TryUnlockInterrogationLayer(layerId))
                    {
                        unlockedAnyProgress = true;
                    }
                }

                foreach (var factId in databaseManager.FactDatabase.FactById.Keys)
                {
                    if (RuntimeState.UnlockedFactIds.Contains(factId) || !CanUnlockFact(factId))
                    {
                        continue;
                    }

                    if (TryUnlockFact(factId))
                    {
                        unlockedAnyProgress = true;
                    }
                }
            }
        }

        private bool CanUnlockStatement(string statementId)
        {
            if (string.IsNullOrWhiteSpace(statementId) || RuntimeState.UnlockedStatementIds.Contains(statementId))
            {
                return false;
            }

            foreach (var requirementId in databaseManager.StatementDatabase.GetUnlockRequirements(statementId))
            {
                if (!IsRequirementSatisfied(requirementId))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanUnlockFact(string factId)
        {
            if (string.IsNullOrWhiteSpace(factId) || RuntimeState.UnlockedFactIds.Contains(factId))
            {
                return false;
            }

            var requirementsAll = databaseManager.FactDatabase.GetRequirementsAll(factId);
            foreach (var requirementId in requirementsAll)
            {
                if (!IsRequirementSatisfied(requirementId))
                {
                    return false;
                }
            }

            var requirementsAny = databaseManager.FactDatabase.GetRequirementsAny(factId);
            if (requirementsAny.Count == 0)
            {
                return true;
            }

            foreach (var requirementId in requirementsAny)
            {
                if (IsRequirementSatisfied(requirementId))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanUnlockInterrogationLayer(string layerId)
        {
            if (string.IsNullOrWhiteSpace(layerId) ||
                RuntimeState.UnlockedInterrogationLayerIds.Contains(layerId) ||
                !databaseManager.TruthDatabase.TryGetInterrogationLayer(layerId, out var layer) ||
                layer == null)
            {
                return false;
            }

            foreach (var requirementId in layer.requiredEvidenceIds ?? new List<string>())
            {
                if (!IsRequirementSatisfied(requirementId))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsRequirementSatisfied(string requirementId)
        {
            return IsEvidenceCollected(requirementId) ||
                   IsFactUnlocked(requirementId) ||
                   IsStatementUnlocked(requirementId) ||
                   IsInterrogationLayerUnlocked(requirementId) ||
                   IsProgressTokenUnlocked(requirementId);
        }

        private bool TryUnlockFact(string factId)
        {
            if (string.IsNullOrWhiteSpace(factId) || !RuntimeState.UnlockedFactIds.Add(factId))
            {
                return false;
            }

            RuntimeState.FactUnlockedById[factId] = true;
            eventManager?.Publish(new FactUnlockedEvent(factId));
            return true;
        }

        private bool TryUnlockStatement(string statementId)
        {
            if (string.IsNullOrWhiteSpace(statementId) || !RuntimeState.UnlockedStatementIds.Add(statementId))
            {
                return false;
            }

            RuntimeState.StatementUnlockedById[statementId] = true;
            eventManager?.Publish(new StatementUnlockedEvent(statementId));
            return true;
        }

        private bool TryUnlockInterrogationLayer(string layerId)
        {
            if (string.IsNullOrWhiteSpace(layerId) || !RuntimeState.UnlockedInterrogationLayerIds.Add(layerId))
            {
                return false;
            }

            RuntimeState.InterrogationLayerUnlockedById[layerId] = true;
            eventManager?.Publish(new InterrogationLayerUnlockedEvent(layerId));

            if (databaseManager.TruthDatabase.TryGetInterrogationLayer(layerId, out var layer) && layer != null)
            {
                foreach (var factId in layer.revealFactIds ?? new List<string>())
                {
                    TryUnlockFact(factId);
                }
            }

            return true;
        }

        private void ValidateDependencies()
        {
            if (eventManager == null)
            {
                throw new InvalidOperationException("ProgressManager requires EventManager during initialization.");
            }

            if (databaseManager == null)
            {
                throw new InvalidOperationException("ProgressManager requires DatabaseManager during initialization.");
            }

            if (databaseManager.EvidenceDatabase == null)
            {
                throw new InvalidOperationException("ProgressManager requires DatabaseManager.EvidenceDatabase during initialization.");
            }

            if (databaseManager.FactDatabase == null)
            {
                throw new InvalidOperationException("ProgressManager requires DatabaseManager.FactDatabase during initialization.");
            }

            if (databaseManager.StatementDatabase == null)
            {
                throw new InvalidOperationException("ProgressManager requires DatabaseManager.StatementDatabase during initialization.");
            }

            if (databaseManager.TruthDatabase == null)
            {
                throw new InvalidOperationException("ProgressManager requires DatabaseManager.TruthDatabase during initialization.");
            }
        }
    }
}
