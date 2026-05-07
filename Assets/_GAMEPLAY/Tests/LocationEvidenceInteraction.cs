using System;
using System.Collections.Generic;
using DetectiveGame.Core;
using UnityEngine;

namespace DetectiveGame.Gameplay.Evidence
{
    public class LocationEvidenceInteraction : MonoBehaviour
    {
        [SerializeField] private string locationId;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private float interactDistance = 3f;

        private AppRoot appRoot;
        private Transform player;

        private void Awake()
        {
            appRoot = AppRoot.Instance;

            if (appRoot == null)
            {
                throw new InvalidOperationException("LocationEvidenceInteraction requires AppRoot.Instance.");
            }

            if (string.IsNullOrWhiteSpace(locationId))
            {
                throw new InvalidOperationException("LocationEvidenceInteraction requires a locationId.");
            }

            if (string.IsNullOrWhiteSpace(playerTag))
            {
                throw new InvalidOperationException("LocationEvidenceInteraction requires a playerTag.");
            }

            var playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null)
            {
                throw new InvalidOperationException(
                    $"LocationEvidenceInteraction could not find a GameObject with tag '{playerTag}'.");
            }

            player = playerObject.transform;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(interactKey))
            {
                return;
            }

            if (Vector2.Distance(transform.position, player.position) <= interactDistance)
            {
                Interact();
            }
        }

        public void Interact()
        {
            var addedCount = AddAvailableEvidenceForLocation();

            Debug.Log(
                $"[LocationEvidenceInteraction] Interacted with location '{locationId}'. AddedEvidenceCount={addedCount}.");
        }

        private int AddAvailableEvidenceForLocation()
        {
            var addedCount = 0;
            var evidenceDatabase = appRoot.DatabaseManager.EvidenceDatabase;
            var collectedEvidenceAtInteractionStart = new HashSet<string>(appRoot.ProgressManager.CollectedEvidenceIds);

            foreach (var evidenceId in evidenceDatabase.GetEvidenceIdsByLocation(locationId))
            {
                if (collectedEvidenceAtInteractionStart.Contains(evidenceId))
                {
                    continue;
                }

                if (!evidenceDatabase.TryGetEvidence(evidenceId, out var evidenceData))
                {
                    Debug.LogWarning(
                        $"[LocationEvidenceInteraction] Evidence '{evidenceId}' listed for location '{locationId}' was not found in EvidenceDatabase.");
                    continue;
                }

                if (!AreEvidenceRequirementsMet(
                        evidenceId,
                        collectedEvidenceAtInteractionStart,
                        out var missingRequirementId))
                {
                    Debug.Log(
                        $"[LocationEvidenceInteraction] Evidence '{evidenceId}' ({evidenceData.displayName}) is not available. Missing requirement '{missingRequirementId}'.");
                    continue;
                }

                if (appRoot.ProgressManager.AddEvidence(evidenceId))
                {
                    addedCount++;
                    Debug.Log(
                        $"[LocationEvidenceInteraction] Added evidence '{evidenceId}' ({evidenceData.displayName}) from location '{locationId}'.");
                }
            }

            return addedCount;
        }

        private bool AreEvidenceRequirementsMet(
            string evidenceId,
            HashSet<string> collectedEvidenceIds,
            out string missingRequirementId)
        {
            foreach (var requirementId in appRoot.DatabaseManager.EvidenceDatabase.GetRequirements(evidenceId))
            {
                if (collectedEvidenceIds.Contains(requirementId))
                {
                    continue;
                }

                missingRequirementId = requirementId;
                return false;
            }

            missingRequirementId = string.Empty;
            return true;
        }
    }
}
