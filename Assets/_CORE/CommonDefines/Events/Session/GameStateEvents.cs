namespace DetectiveGame.Core
{
    public readonly struct GamePhaseChangedEvent
    {
        public GamePhaseChangedEvent(GamePhase phase)
        {
            Phase = phase;
        }

        public GamePhase Phase { get; }
    }

    public readonly struct UiBlockStateChangedEvent
    {
        public UiBlockStateChangedEvent(bool isBlocked)
        {
            IsBlocked = isBlocked;
        }

        public bool IsBlocked { get; }
    }
}
