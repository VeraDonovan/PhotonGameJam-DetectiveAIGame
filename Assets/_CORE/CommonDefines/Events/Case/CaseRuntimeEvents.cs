namespace DetectiveGame.Core
{
    public readonly struct EvidenceAddedEvent
    {
        public EvidenceAddedEvent(string evidenceId)
        {
            EvidenceId = evidenceId;
        }

        public string EvidenceId { get; }
    }

    public readonly struct FactUnlockedEvent
    {
        public FactUnlockedEvent(string factId)
        {
            FactId = factId;
        }

        public string FactId { get; }
    }

    public readonly struct NpcDiscoveredEvent
    {
        public NpcDiscoveredEvent(string npcId)
        {
            NpcId = npcId;
        }

        public string NpcId { get; }
    }
}
