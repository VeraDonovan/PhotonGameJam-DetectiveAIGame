using DetectiveGame.Core;
using UnityEngine;
using System;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class EvidencePickupDebugInput : MonoBehaviour
    {
        [SerializeField] private KeyCode triggerKey = KeyCode.E;
        [SerializeField] private string[] evidenceIds = { "A-1", "A-2", "A-3", "A-4", "A-5", "A-6" };

        private AppRoot appRoot;
        private int nextEvidenceIndex;

        private void Awake()
        {
            appRoot = AppRoot.Instance;

            if (appRoot == null)
            {
                throw new InvalidOperationException("EvidencePickupDebugInput requires AppRoot.Instance.");
            }

            if (evidenceIds == null || evidenceIds.Length == 0)
            {
                throw new InvalidOperationException("EvidencePickupDebugInput requires at least one configured evidence id.");
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(triggerKey))
            {
                return;
            }

            if (nextEvidenceIndex >= evidenceIds.Length)
            {
                Debug.Log(
                    $"[EvidencePickupDebugInput] Key '{triggerKey}' pressed but all configured evidence ids have already been sent.");
                return;
            }

            var evidenceId = evidenceIds[nextEvidenceIndex];
            if (!appRoot.DatabaseManager.EvidenceDatabase.TryGetEvidence(evidenceId, out var evidenceData))
            {
                Debug.LogWarning(
                    $"[EvidencePickupDebugInput] Key '{triggerKey}' pressed but EvidenceId '{evidenceId}' was not found in EvidenceDatabase.");
                return;
            }

            Debug.Log(
                $"[EvidencePickupDebugInput] Key '{triggerKey}' pressed. Sending evidence {nextEvidenceIndex + 1}/{evidenceIds.Length}: '{evidenceId}' ({evidenceData.displayName}).");

            if (appRoot.ProgressManager.AddEvidence(evidenceId))
            {
                nextEvidenceIndex++;
            }
        }
    }
}
