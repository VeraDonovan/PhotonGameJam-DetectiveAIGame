using UnityEngine;

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
            CurrentPhase = startingPhase;
        }

        public bool TrySetPhase(GamePhase nextPhase)
        {
            if (CurrentPhase == nextPhase)
            {
                return false;
            }

            CurrentPhase = nextPhase;
            eventManager?.Publish(new GamePhaseChangedEvent(CurrentPhase));
            return true;
        }
    }
}
