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
}
