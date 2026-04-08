using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class EndingSetData
    {
        public List<EndingData> endings = new List<EndingData>();
    }

    [Serializable]
    public sealed class EndingData
    {
        public string endingId;
        public string displayName;
        public string category;
        public EndingRequirementsData requirements;
        public string unlockConditionText;
        public string result;
    }

    [Serializable]
    public sealed class EndingRequirementsData
    {
        public List<string> requiredFactIds = new List<string>();
        public List<string> requiredNpcLayerIds = new List<string>();
        public List<string> requiredEvidenceIds = new List<string>();
        public List<string> requiredAnyFactIds = new List<string>();
    }
}
