using System.Collections.Generic;
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
        public IReadOnlyCollection<string> CollectedEvidenceIds => RuntimeState.CollectedEvidenceIds;
        public IReadOnlyCollection<string> UnlockedFactIds => RuntimeState.UnlockedFactIds;
        public IReadOnlyCollection<string> KnownSuspectIds => RuntimeState.KnownSuspectIds;
        public IReadOnlyCollection<string> SelectedSuspectIds => RuntimeState.SelectedSuspectIds;
        public string AccusationTargetId => RuntimeState.AccusationTargetId;

        public void Initialize(EventManager sharedEventManager, DatabaseManager sharedDatabaseManager)
        {
            eventManager = sharedEventManager;
            databaseManager = sharedDatabaseManager;
            ResetRuntime();
        }

        public bool AddEvidence(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId) || !RuntimeState.CollectedEvidenceIds.Add(evidenceId))
            {
                return false;
            }

            RuntimeState.EvidenceCollectedById[evidenceId] = true;
            eventManager?.Publish(new EvidenceAddedEvent(evidenceId));
            return true;
        }

        public bool UnlockFact(string factId)
        {
            if (string.IsNullOrWhiteSpace(factId) || !RuntimeState.UnlockedFactIds.Add(factId))
            {
                return false;
            }

            RuntimeState.FactUnlockedById[factId] = true;
            eventManager?.Publish(new FactUnlockedEvent(factId));
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
        }

        private void BuildEvidenceProgressMap()
        {
            if (databaseManager?.EvidenceDatabase == null)
            {
                return;
            }

            foreach (var evidenceId in databaseManager.EvidenceDatabase.EvidenceById.Keys)
            {
                RuntimeState.EvidenceCollectedById[evidenceId] = false;
            }
        }

        private void BuildFactProgressMap()
        {
            if (databaseManager?.FactDatabase == null)
            {
                return;
            }

            foreach (var factId in databaseManager.FactDatabase.FactById.Keys)
            {
                RuntimeState.FactUnlockedById[factId] = false;
            }
        }
    }
}
