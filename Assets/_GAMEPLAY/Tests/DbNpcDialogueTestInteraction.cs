using System;
using System.Collections.Generic;
using DetectiveGame.Core;
using UnityEngine;

namespace _GAMEPLAY.Tests
{
    public sealed class DbNpcDialogueTestInteraction : MonoBehaviour
    {
        [Header("NPC")]
        [SerializeField] private string npcId = "npc_1";

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private string playerTag = "Player";
        private bool _playerInRange;

        private void Start()
        {
            ValidateDependencies();
        }

        private void Update()
        {
            UpdateInteraction();
        }

        private void UpdateInteraction()
        {
            if (!_playerInRange)
            {
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                ValidateNpcFacts();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                _playerInRange = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                _playerInRange = false;
            }
        }

        private void ValidateNpcFacts()
        {
            var appRoot = AppRoot.Instance;
            var npcRuntimeManager = appRoot?.NpcRuntimeManager;
            var progressManager = appRoot?.ProgressManager;
            var factDatabase = appRoot?.DatabaseManager?.FactDatabase;

            if (npcRuntimeManager == null || progressManager == null || factDatabase == null)
            {
                throw new InvalidOperationException(
                    "DbNpcDialogueTestInteraction requires AppRoot, NpcRuntimeManager, ProgressManager, and FactDatabase.");
            }

            npcRuntimeManager.RegisterNpc(npcId);
            progressManager.RegisterSuspect(npcId);

            var unlockedFactIds = new List<string>();
            foreach (var factId in factDatabase.GetFactIdsByNpc(npcId))
            {
                if (!factDatabase.TryGetFact(factId, out var fact) ||
                    fact == null ||
                    progressManager.IsFactUnlocked(factId) ||
                    !IsExplorationDialogueFact(fact) ||
                    !AreEvidenceRequirementsMet(progressManager, fact.unlock?.sourceEvidenceIds) ||
                    !AreFactRequirementsMet(progressManager, fact.unlock?.sourceFactIds) ||
                    !AreDialogueNpcRequirementsMet(npcRuntimeManager, fact.unlock?.sourceDialogueIds))
                {
                    continue;
                }

                if (progressManager.UnlockFact(factId))
                {
                    unlockedFactIds.Add(factId);
                }
            }

            Debug.Log(
                $"[DbNpcDialogueTestInteraction] Interacted with npc '{npcId}'. UnlockedFactCount={unlockedFactIds.Count}.");
        }

        private void ValidateDependencies()
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                throw new InvalidOperationException("DbNpcDialogueTestInteraction requires an npcId.");
            }

            if (string.IsNullOrWhiteSpace(playerTag))
            {
                throw new InvalidOperationException("DbNpcDialogueTestInteraction requires a playerTag.");
            }

            var appRoot = AppRoot.Instance;
            if (appRoot == null || appRoot.DatabaseManager == null || appRoot.DatabaseManager.NpcDatabase == null)
            {
                throw new InvalidOperationException("DbNpcDialogueTestInteraction requires AppRoot.DatabaseManager.NpcDatabase.");
            }

            if (!appRoot.DatabaseManager.NpcDatabase.TryGetNpc(npcId, out var databaseNpcData) || databaseNpcData == null)
            {
                throw new InvalidOperationException($"DbNpcDialogueTestInteraction could not find npcId '{npcId}' in NpcDatabase.");
            }
        }

        private static bool IsExplorationDialogueFact(FactData fact)
        {
            if (fact.unlock == null || fact.scope == null)
            {
                return false;
            }

            var unlockType = fact.unlock.unlockType;
            if (!string.Equals(unlockType, "dialogue_only", StringComparison.Ordinal) &&
                !string.Equals(unlockType, "evidence_plus_dialogue", StringComparison.Ordinal))
            {
                return false;
            }

            IReadOnlyList<string> phaseTags = fact.scope.phaseTags == null
                ? Array.Empty<string>()
                : fact.scope.phaseTags;
            foreach (var phaseTag in phaseTags)
            {
                if (string.Equals(phaseTag, "exploration", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AreEvidenceRequirementsMet(
            ProgressManager progressManager,
            IReadOnlyList<string> sourceEvidenceIds)
        {
            foreach (var evidenceId in sourceEvidenceIds ?? Array.Empty<string>())
            {
                if (!progressManager.IsEvidenceCollected(evidenceId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreFactRequirementsMet(
            ProgressManager progressManager,
            IReadOnlyList<string> sourceFactIds)
        {
            foreach (var factId in sourceFactIds ?? Array.Empty<string>())
            {
                if (!progressManager.IsFactUnlocked(factId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreDialogueNpcRequirementsMet(
            NpcRuntimeManager npcRuntimeManager,
            IReadOnlyList<string> sourceDialogueIds)
        {
            foreach (var dialogueId in sourceDialogueIds ?? Array.Empty<string>())
            {
                var requiredNpcId = ExtractNpcId(dialogueId);
                if (string.IsNullOrWhiteSpace(requiredNpcId) ||
                    !HasDiscoveredNpc(npcRuntimeManager, requiredNpcId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasDiscoveredNpc(NpcRuntimeManager npcRuntimeManager, string npcId)
        {
            foreach (var discoveredNpcId in npcRuntimeManager.DiscoveredNpcIds)
            {
                if (string.Equals(discoveredNpcId, npcId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ExtractNpcId(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                return string.Empty;
            }

            var separatorIndex = dialogueId.IndexOf('.');
            return separatorIndex <= 0 ? string.Empty : dialogueId.Substring(0, separatorIndex);
        }
    }
}
