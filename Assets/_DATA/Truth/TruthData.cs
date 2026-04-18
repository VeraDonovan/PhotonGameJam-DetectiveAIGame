using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class TruthData
    {
        public string templateVersion;
        public string schemaType;
        public LanguageData language;
        public string caseId;
        public CaseTruthData caseTruth;
        public List<TruthTimelineEntryData> timeline = new List<TruthTimelineEntryData>();
        public List<NpcTruthData> npcTruths = new List<NpcTruthData>();
        public List<DeductionTruthData> deductionTruths = new List<DeductionTruthData>();
    }

    [Serializable]
    public sealed class LanguageData
    {
        public string schema;
        public string content;
    }

    [Serializable]
    public sealed class CaseTruthData
    {
        public string realKillerId;
        public string realMotive;
        public string coverUp;
        public string coreHiddenStory;
    }

    [Serializable]
    public sealed class TruthTimelineEntryData
    {
        public int sequence;
        public string time;
        public string @event;
        public List<string> participants = new List<string>();
    }

    [Serializable]
    public sealed class NpcTruthData
    {
        public string npcId;
        public string actualRelationshipToVictim;
        public bool isKiller;
        public string backstory;
        public string realMotive;
        public string hiddenTruth;
        public List<string> knowledge = new List<string>();
        public List<DialogueTriggerData> dialogueTriggers = new List<DialogueTriggerData>();
        public List<TruthInterrogationLayerData> interrogationLayers = new List<TruthInterrogationLayerData>();
    }

    [Serializable]
    public sealed class DialogueTriggerData
    {
        public string triggerId;
        public string topic;
        public List<string> unlockRequirements = new List<string>();
        public string revealGoal;
        public string aiGuidance;
        public List<string> withhold = new List<string>();
        public List<string> examplePhrasings = new List<string>();
    }

    [Serializable]
    public sealed class TruthInterrogationLayerData
    {
        public string layerId;
        public string roundType;
        public string topic;
        public string revealGoal;
        public List<string> requiredEvidenceIds = new List<string>();
        public List<string> revealFactIds = new List<string>();
        public string aiGuidance;
        public List<string> examplePhrasings = new List<string>();
    }

    [Serializable]
    public sealed class DeductionTruthData
    {
        public string truthId;
        public string displayName;
        public List<string> requiresFactIds = new List<string>();
    }
}
