using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class EvidenceGraphData
    {
        public List<EvidenceNodeData> evidenceNodes = new List<EvidenceNodeData>();
        public List<EvidenceRelationshipData> relationships = new List<EvidenceRelationshipData>();
    }

    [Serializable]
    public sealed class EvidenceNodeData
    {
        public string evidenceId;
        public string tier;
        public string displayName;
        public string summary;
        public string locationId;
        public string targetNpcId;
        public List<string> requirements = new List<string>();
        public string mapGroup;
        [NonSerialized] public Sprite iconSprite;
    }

    [Serializable]
    public sealed class EvidenceRelationshipData
    {
        public string fromId;
        public string toId;
        public string relationshipType;
        public string displayText;
    }
}
