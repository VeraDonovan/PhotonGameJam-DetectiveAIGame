using DetectiveGame.Core;
using UnityEngine;
using System;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class EvidencePickupDebugInput : MonoBehaviour
    {
        [SerializeField] private KeyCode triggerKey = KeyCode.E;
        [SerializeField] private string[] evidenceIds =
        {
            "A-3", "A-4", "B-3",
            "A-8", "B-7", "B-8", "C-1",
            "A-9", "A-11", "A-13", "B-14", "C-2",
            "A-10", "B-9", "B-10", "A-16", "B-17", "C-3"
        };
        [SerializeField] private bool enforceEvidenceRequirements = true;

        private AppRoot appRoot;

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

            TryAddNextAvailableEvidence();
        }

        private void TryAddNextAvailableEvidence()
        {
            foreach (var evidenceId in evidenceIds)
            {
                if (appRoot.ProgressManager.IsEvidenceCollected(evidenceId))
                {
                    continue;
                }

                if (!appRoot.DatabaseManager.EvidenceDatabase.TryGetEvidence(evidenceId, out var evidenceData))
                {
                    Debug.LogWarning(
                        $"[EvidencePickupDebugInput] Configured EvidenceId '{evidenceId}' was not found in EvidenceDatabase.");
                    continue;
                }

                if (enforceEvidenceRequirements && !AreEvidenceRequirementsMet(evidenceId, out _))
                {
                    continue;
                }

                Debug.Log(
                    $"[EvidencePickupDebugInput] Key '{triggerKey}' pressed. Sending next available evidence '{evidenceId}' ({evidenceData.displayName}).");

                appRoot.ProgressManager.AddEvidence(evidenceId);
                return;
            }

            Debug.Log(
                $"[EvidencePickupDebugInput] Key '{triggerKey}' pressed but no more configured evidence ids are currently available.");
        }

        private bool AreEvidenceRequirementsMet(string evidenceId, out string missingRequirement)
        {
            foreach (var requirementId in appRoot.DatabaseManager.EvidenceDatabase.GetRequirements(evidenceId))
            {
                if (appRoot.ProgressManager.IsEvidenceCollected(requirementId))
                {
                    continue;
                }

                missingRequirement = requirementId;
                return false;
            }

            missingRequirement = string.Empty;
            return true;
        }
    }
}
