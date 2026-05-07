using System;
using DetectiveGame.Core;
using UnityEngine;

namespace _GAMEPLAY.Observation
{
    public class ObservationFactInteraction : MonoBehaviour
    {
        [SerializeField] private string factId;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private float interactDistance = 3f;

        private AppRoot _appRoot;
        private Transform _player;

        private void Awake()
        {
            _appRoot = AppRoot.Instance;
            if (_appRoot == null)
            {
                throw new InvalidOperationException("ObservationFactInteraction requires AppRoot.Instance.");
            }

            if (string.IsNullOrWhiteSpace(factId))
            {
                throw new InvalidOperationException("ObservationFactInteraction requires a factId.");
            }

            if (string.IsNullOrWhiteSpace(playerTag))
            {
                throw new InvalidOperationException("ObservationFactInteraction requires a playerTag.");
            }

            var playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null)
            {
                throw new InvalidOperationException(
                    $"ObservationFactInteraction could not find a GameObject with tag '{playerTag}'.");
            }

            _player = playerObject.transform;

            if (_appRoot.DatabaseManager == null || _appRoot.DatabaseManager.FactDatabase == null)
            {
                throw new InvalidOperationException(
                    "ObservationFactInteraction requires AppRoot.DatabaseManager.FactDatabase.");
            }

            if (!_appRoot.DatabaseManager.FactDatabase.TryGetFact(factId, out var fact) || fact == null)
            {
                throw new InvalidOperationException(
                    $"ObservationFactInteraction could not find factId '{factId}' in FactDatabase.");
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(interactKey))
            {
                return;
            }

            if (Vector2.Distance(transform.position, _player.position) <= interactDistance)
            {
                Interact();
            }
        }

        public void Interact()
        {
            var factDatabase = _appRoot.DatabaseManager.FactDatabase;
            var progressManager = _appRoot.ProgressManager;

            if (!factDatabase.TryGetFact(factId, out var fact) || fact == null)
            {
                Debug.LogWarning(
                    $"[ObservationFactInteraction] Fact '{factId}' was not found during interaction.");
                return;
            }

            if (!string.Equals(fact.unlock?.unlockType, "interaction", StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"[ObservationFactInteraction] Fact '{factId}' is not authored as an interaction unlock.");
                return;
            }

            if (progressManager.IsFactUnlocked(factId))
            {
                Debug.Log(
                    $"[ObservationFactInteraction] Fact '{factId}' was already unlocked.");
                return;
            }

            var unlocked = progressManager.UnlockFact(factId);
            Debug.Log(
                $"[ObservationFactInteraction] Interacted with observation fact '{factId}'. UnlockSucceeded={unlocked}.");
        }
    }
}
