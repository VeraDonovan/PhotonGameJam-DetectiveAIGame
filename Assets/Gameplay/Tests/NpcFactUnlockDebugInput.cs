using System;
using System.Collections.Generic;
using DetectiveGame.Core;
using UnityEngine;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class NpcFactUnlockDebugInput : MonoBehaviour
    {
        [SerializeField] private KeyCode unlockNextFactKey = KeyCode.F;
        [SerializeField] private string targetNpcId = "npc_1";
        [SerializeField] private string[] factIds =
        {
            "fact_3",
            "fact_4",
            "fact_7",
            "fact_9",
            "fact_10",
            "fact_12"
        };

        private AppRoot appRoot;

        private void Awake()
        {
            appRoot = AppRoot.Instance;

            if (appRoot == null)
            {
                throw new InvalidOperationException("NpcFactUnlockDebugInput requires AppRoot.Instance.");
            }

            if (string.IsNullOrWhiteSpace(targetNpcId))
            {
                throw new InvalidOperationException("NpcFactUnlockDebugInput requires a targetNpcId.");
            }

            if (factIds == null || factIds.Length == 0)
            {
                throw new InvalidOperationException("NpcFactUnlockDebugInput requires at least one configured fact id.");
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(unlockNextFactKey))
            {
                return;
            }

            TryUnlockNextEligibleFact();
        }

        private void TryUnlockNextEligibleFact()
        {
            foreach (var factId in factIds)
            {
                if (appRoot.ProgressManager.IsFactUnlocked(factId))
                {
                    continue;
                }

                if (!appRoot.DatabaseManager.FactDatabase.TryGetFact(factId, out var fact) || fact == null)
                {
                    Debug.LogWarning($"[NpcFactUnlockDebugInput] Fact '{factId}' was not found in FactDatabase.");
                    continue;
                }

                if (!IsFactForTargetNpc(fact))
                {
                    continue;
                }

                if (!AreFactRequirementsMet(fact, out _, out _))
                {
                    continue;
                }

                if (appRoot.ProgressManager.UnlockFact(factId))
                {
                    Debug.Log(
                        $"[NpcFactUnlockDebugInput] Key '{unlockNextFactKey}' pressed. Unlocked fact '{factId}' for '{targetNpcId}' ({fact.displayName}).");
                }

                return;
            }

            Debug.Log($"[NpcFactUnlockDebugInput] Key '{unlockNextFactKey}' pressed but no more configured facts are currently available for '{targetNpcId}'.");
        }

        private bool IsFactForTargetNpc(FactData fact)
        {
            return fact.scope?.relatedNpcIds != null &&
                   fact.scope.relatedNpcIds.Contains(targetNpcId);
        }

        private bool AreFactRequirementsMet(FactData fact, out string missingSourceType, out string missingSourceId)
        {
            foreach (var evidenceId in fact.unlock?.sourceEvidenceIds ?? new List<string>())
            {
                if (appRoot.ProgressManager.IsEvidenceCollected(evidenceId))
                {
                    continue;
                }

                missingSourceType = "evidence";
                missingSourceId = evidenceId;
                return false;
            }

            foreach (var factId in fact.unlock?.sourceFactIds ?? new List<string>())
            {
                if (appRoot.ProgressManager.IsFactUnlocked(factId))
                {
                    continue;
                }

                missingSourceType = "fact";
                missingSourceId = factId;
                return false;
            }

            missingSourceType = string.Empty;
            missingSourceId = string.Empty;
            return true;
        }
    }
}
