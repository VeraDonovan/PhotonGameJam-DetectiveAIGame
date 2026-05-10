using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class DialogueBeatSetData
    {
        public string templateVersion;
        public string schemaType;
        public LanguageData language;
        public string caseId;
        public List<DialogueBeatTopicData> topics = new List<DialogueBeatTopicData>();
    }

    [Serializable]
    public sealed class DialogueBeatTopicData
    {
        public string topicId;
        public string npcId;
        public string displayName;
        public int sortOrder;
        public List<DialogueBeatNodeData> nodes = new List<DialogueBeatNodeData>();
    }

    [Serializable]
    public sealed class DialogueBeatNodeData
    {
        public string nodeId;
        public string phase;
        public string availabilityType;
        public string truthStatus;
        public bool isLie;
        public DialogueBeatTriggerData trigger;
        public string text;
        public List<string> behavior = new List<string>();
        public List<string> requiredEvidenceIds = new List<string>();
        public List<string> requiredFactIds = new List<string>();
        public List<string> requiredStatementIds = new List<string>();
        public List<string> requiredLayerIds = new List<string>();
        public List<string> requiredTokenIds = new List<string>();
        public string unlockStatementId;
        public List<string> unlockFactIds = new List<string>();
        public List<string> unlockLayerIds = new List<string>();
        public List<string> unlockTokenIds = new List<string>();
        public string caughtLieId;
        public List<string> nextSuggestedNodeIds = new List<string>();
    }

    [Serializable]
    public sealed class DialogueBeatTriggerData
    {
        public string type;
        public string id;
        public string parentId;
        public string intent;
        public string promptLabel;
    }
}
