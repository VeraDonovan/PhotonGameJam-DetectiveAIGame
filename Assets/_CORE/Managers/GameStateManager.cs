using UnityEngine;
using System;

namespace DetectiveGame.Core
{
    public sealed class GameStateManager : MonoBehaviour
    {
        [SerializeField] private GamePhase startingPhase = GamePhase.Exploration;
        [SerializeField] private int maxInterrogationSuspects = 2;

        private EventManager eventManager;
        private ProgressManager progressManager;

        public GamePhase CurrentPhase { get; private set; }

        public void Initialize(EventManager sharedEventManager, ProgressManager sharedProgressManager)
        {
            eventManager = sharedEventManager;
            progressManager = sharedProgressManager;
            ValidateDependencies();
            CurrentPhase = startingPhase;
        }

        public bool TryStartGame()
        {
            CurrentPhase = GamePhase.Exploration;
            eventManager.Publish(new GamePhaseChangedEvent(CurrentPhase));
            return true;
        }

        public bool TryBeginInterrogation()
        {
            return TrySetPhase(GamePhase.Interrogation);
        }

        public bool TrySetPhase(GamePhase nextPhase)
        {
            if (CurrentPhase == nextPhase || !CanTransitionTo(nextPhase))
            {
                return false;
            }

            CurrentPhase = nextPhase;
            eventManager.Publish(new GamePhaseChangedEvent(CurrentPhase));
            return true;
        }

        private bool CanTransitionTo(GamePhase nextPhase)
        {
            switch (CurrentPhase)
            {
                case GamePhase.Exploration:
                    return nextPhase == GamePhase.Interrogation &&
                           HasValidInterrogationSelection();
                case GamePhase.Interrogation:
                    return nextPhase == GamePhase.Exploration;
                default:
                    return false;
            }
        }

        private bool HasValidInterrogationSelection()
        {
            var selectedCount = progressManager.SelectedSuspectIds.Count;
            return selectedCount > 0 && selectedCount <= maxInterrogationSuspects;
        }

        private void ValidateDependencies()
        {
            if (eventManager == null)
            {
                throw new InvalidOperationException("GameStateManager requires EventManager during initialization.");
            }

            if (progressManager == null)
            {
                throw new InvalidOperationException("GameStateManager requires ProgressManager during initialization.");
            }
        }
    }
}
