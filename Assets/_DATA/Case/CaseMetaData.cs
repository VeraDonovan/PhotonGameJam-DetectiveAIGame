using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class CaseMetaData
    {
        public string templateVersion;
        public string schemaType;
        public string caseId;
        public string title;
        public CaseLinkedDataFiles linkedDataFiles;
        public List<string> startingEvidenceIds;
        public CaseBackgroundData caseBackground;
    }

    [Serializable]
    public sealed class CaseLinkedDataFiles
    {
        public string npc;
        public string evidence;
        public string facts;
        public string statements;
        public string dialogueBeats;
        public string truth;
        public string ending;
        public CaseNpcAiLinkedDataFiles npcAi;
    }

    [Serializable]
    public sealed class CaseNpcAiLinkedDataFiles
    {
        public string lin;
        public string wei;
        public string zhang;

        public string GetFileNameForNpc(string npcId)
        {
            switch (npcId)
            {
                case "lin":
                    return lin;
                case "wei":
                    return wei;
                case "zhang":
                    return zhang;
                default:
                    return null;
            }
        }
    }

    [Serializable]
    public sealed class CaseBackgroundData
    {
        public string briefBackground;
    }
}
