using System.Collections.Generic;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class CaseRuntimeManager : MonoBehaviour
    {
        private EventManager eventManager;

        public CaseRuntimeState RuntimeState { get; private set; } = new CaseRuntimeState();
        public IReadOnlyCollection<string> CollectedEvidenceIds => RuntimeState.CollectedEvidenceIds;
        public IReadOnlyCollection<string> UnlockedFactIds => RuntimeState.UnlockedFactIds;
        public IReadOnlyCollection<string> KnownSuspectIds => RuntimeState.KnownSuspectIds;
        public IReadOnlyCollection<string> SelectedSuspectIds => RuntimeState.SelectedSuspectIds;
        public string AccusationTargetId => RuntimeState.AccusationTargetId;

        public void Initialize(EventManager sharedEventManager)
        {
            eventManager = sharedEventManager;
            ResetRuntime();
        }

        public bool AddEvidence(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId) || !RuntimeState.CollectedEvidenceIds.Add(evidenceId))
            {
                return false;
            }

            eventManager?.Publish(new EvidenceAddedEvent(evidenceId));
            return true;
        }

        public bool UnlockFact(string factId)
        {
            if (string.IsNullOrWhiteSpace(factId) || !RuntimeState.UnlockedFactIds.Add(factId))
            {
                return false;
            }

            eventManager?.Publish(new FactUnlockedEvent(factId));
            return true;
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
            RuntimeState = new CaseRuntimeState();
        }
    }
}
