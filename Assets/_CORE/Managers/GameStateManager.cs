using UnityEngine;
using System;

namespace DetectiveGame.Core
{
    public sealed class GameStateManager : MonoBehaviour
    {
        [SerializeField] private GamePhase startingPhase = GamePhase.Intro;

        private EventManager eventManager;

        public GamePhase CurrentPhase { get; private set; }

        public void Initialize(EventManager sharedEventManager)
        {
            eventManager = sharedEventManager;
            ValidateDependencies();
            CurrentPhase = startingPhase;
        }

        public bool TrySetPhase(GamePhase nextPhase)
        {
            if (CurrentPhase == nextPhase)
            {
                return false;
            }

            CurrentPhase = nextPhase;
            eventManager.Publish(new GamePhaseChangedEvent(CurrentPhase));
            return true;
        }

        private void ValidateDependencies()
        {
            if (eventManager == null)
            {
                throw new InvalidOperationException("GameStateManager requires EventManager during initialization.");
            }
        }
    }
}
