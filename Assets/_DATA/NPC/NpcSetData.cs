using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class NpcSetData
    {
        public List<NpcData> npcs = new List<NpcData>();
        public List<TriggerDialogData> triggerDialogs = new List<TriggerDialogData>();
    }

    [Serializable]
    public sealed class NpcData
    {
        public string npcId;
        public string roleType;
        public string displayName;
        public int age;
        public string locationId;
        public string relationshipToVictim;
        public string archetype;
        public bool isKiller;
        public string backstory;
        public string motive;
        public string hiddenTruth;
        public List<string> knowledge = new List<string>();
        public NpcStatsData stats;
        public string initialState;
        public string initialStatement;
        public List<NpcInterrogationLayerData> interrogationLayers = new List<NpcInterrogationLayerData>();
    }

    [Serializable]
    public sealed class NpcStatsData
    {
        public int composure;
        public int lie;
        public int aggression;
        public int cooperation;
        public int guilt;
        public int trauma;
    }

    [Serializable]
    public sealed class NpcInterrogationLayerData
    {
        public int layer;
        public string topic;
        public int pressureGoal;
        public string unlockLine;
        public string collapseLine;
        public string forcedTwist;
        public List<NpcBreakpointData> breakpoints = new List<NpcBreakpointData>();
    }

    [Serializable]
    public sealed class NpcBreakpointData
    {
        public string prompt;
        public string excuse;
        public int pressure;
    }

    [Serializable]
    public sealed class TriggerDialogData
    {
        public string dialogId;
        public List<string> triggerRequirements = new List<string>();
        public List<string> participants = new List<string>();
        public string type;
        public List<string> content = new List<string>();
        public string purpose;
    }
}
