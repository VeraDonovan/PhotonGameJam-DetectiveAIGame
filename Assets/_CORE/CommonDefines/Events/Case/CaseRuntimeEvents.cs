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

    public readonly struct StatementUnlockedEvent
    {
        public StatementUnlockedEvent(string topicId, string statementId)
        {
            TopicId = topicId;
            StatementId = statementId;
        }

        public string TopicId { get; }
        public string StatementId { get; }
    }

    public readonly struct InterrogationLayerUnlockedEvent
    {
        public InterrogationLayerUnlockedEvent(string layerId)
        {
            LayerId = layerId;
        }

        public string LayerId { get; }
    }

    public readonly struct NpcDiscoveredEvent
    {
        public NpcDiscoveredEvent(string npcId)
        {
            NpcId = npcId;
        }

        public string NpcId { get; }
    }

    public readonly struct CurrentInterrogationTargetChangedEvent
    {
        public CurrentInterrogationTargetChangedEvent(string oldTargetId, string newTargetId)
        {
            OldTargetId = oldTargetId ?? string.Empty;
            NewTargetId = newTargetId ?? string.Empty;
        }

        public string OldTargetId { get; }
        public string NewTargetId { get; }
    }

    public readonly struct NpcAnnoyanceChangedEvent
    {
        public NpcAnnoyanceChangedEvent(string npcId, int oldValue, int newValue, GamePhase phase)
        {
            NpcId = npcId;
            OldValue = oldValue;
            NewValue = newValue;
            Phase = phase;
        }

        public string NpcId { get; }
        public int OldValue { get; }
        public int NewValue { get; }
        public GamePhase Phase { get; }
    }

    public readonly struct NpcPressureChangedEvent
    {
        public NpcPressureChangedEvent(string npcId, int oldValue, int newValue, GamePhase phase)
        {
            NpcId = npcId;
            OldValue = oldValue;
            NewValue = newValue;
            Phase = phase;
        }

        public string NpcId { get; }
        public int OldValue { get; }
        public int NewValue { get; }
        public GamePhase Phase { get; }
    }

    public readonly struct NpcInterrogationLevelChangedEvent
    {
        public NpcInterrogationLevelChangedEvent(
            string npcId,
            int oldLevel,
            int newLevel,
            string oldLayerId,
            string newLayerId)
        {
            NpcId = npcId;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            OldLayerId = oldLayerId ?? string.Empty;
            NewLayerId = newLayerId ?? string.Empty;
        }

        public string NpcId { get; }
        public int OldLevel { get; }
        public int NewLevel { get; }
        public string OldLayerId { get; }
        public string NewLayerId { get; }
    }
}
