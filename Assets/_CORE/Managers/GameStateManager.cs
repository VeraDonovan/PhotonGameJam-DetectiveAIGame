using UnityEngine;
using System;

namespace DetectiveGame.Core
{
    public sealed class GameStateManager : MonoBehaviour
    {
        [SerializeField] private GamePhase startingPhase = GamePhase.Intro;
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
            return TrySetPhase(GamePhase.Exploration);
        }

        public bool TryBeginInterrogation()
        {
            return TrySetPhase(GamePhase.Interrogation);
        }

        public bool TryOpenAccusation()
        {
            return TrySetPhase(GamePhase.Accusation);
        }

        public bool TryShowResult()
        {
            return TrySetPhase(GamePhase.Result);
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
                case GamePhase.Intro:
                    return nextPhase == GamePhase.Exploration;
                case GamePhase.Exploration:
                    return nextPhase == GamePhase.Interrogation &&
                           HasValidInterrogationSelection();
                case GamePhase.Interrogation:
                    return nextPhase == GamePhase.Accusation &&
                           HasAccusationTarget();
                case GamePhase.Accusation:
                    return nextPhase == GamePhase.Result &&
                           HasAccusationTarget();
                case GamePhase.Result:
                    return false;
                default:
                    return false;
            }
        }

        private bool HasValidInterrogationSelection()
        {
            var selectedCount = progressManager.SelectedSuspectIds.Count;
            return selectedCount > 0 && selectedCount <= maxInterrogationSuspects;
        }

        private bool HasAccusationTarget()
        {
            return !string.IsNullOrWhiteSpace(progressManager.AccusationTargetId);
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
