using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class FactGraphData
    {
        public List<FactData> facts = new List<FactData>();
        public List<FactRelationshipData> factRelationships = new List<FactRelationshipData>();
    }

    [Serializable]
    public sealed class FactData
    {
        public string factId;
        public string displayName;
        public string summary;
        public string category;
        public FactTruthData truth;
        public FactUnlockData unlock;
        public FactProgressionData progression;
        public FactScopeData scope;
    }

    [Serializable]
    public sealed class FactTruthData
    {
        public string truthClass;
        public bool isHiddenTruth;
    }

    [Serializable]
    public sealed class FactUnlockData
    {
        public string unlockType;
        public List<string> requirementsAll = new List<string>();
        public List<string> requirementsAny = new List<string>();
        public List<string> sourceEvidenceIds = new List<string>();
        public List<string> sourceDialogueIds = new List<string>();
        public List<string> sourceFactIds = new List<string>();
    }

    [Serializable]
    public sealed class FactProgressionData
    {
        public List<string> usedFor = new List<string>();
        public List<string> unlocksFactIds = new List<string>();
        public List<string> unlocksDialogueIds = new List<string>();
        public List<string> unlocksEvidenceIds = new List<string>();
        public List<string> supportsEndingIds = new List<string>();
    }

    [Serializable]
    public sealed class FactScopeData
    {
        public List<string> relatedNpcIds = new List<string>();
        public List<string> relatedLocationIds = new List<string>();
        public List<string> phaseTags = new List<string>();
    }

    [Serializable]
    public sealed class FactRelationshipData
    {
        public string fromFactId;
        public string toFactId;
        public string relationshipType;
        public string displayText;
    }
}
