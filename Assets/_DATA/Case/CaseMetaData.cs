using System;

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
        public CaseBackgroundData caseBackground;
    }

    [Serializable]
    public sealed class CaseLinkedDataFiles
    {
        public string npc;
        public string evidence;
        public string facts;
        public string statements;
        public string truth;
        public string ending;
        public CaseNpcAiLinkedDataFiles npcAi;
    }

    [Serializable]
    public sealed class CaseNpcAiLinkedDataFiles
    {
        public string npc_1;
        public string npc_2;
        public string npc_3;

        public string GetFileNameForNpc(string npcId)
        {
            switch (npcId)
            {
                case "npc_1":
                    return npc_1;
                case "npc_2":
                    return npc_2;
                case "npc_3":
                    return npc_3;
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
