using System;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class NpcAiProfileData
    {
        public string templateVersion;
        public string schemaType;
        public LanguageData language;
        public string caseId;
        public string npcId;
        public string displayName;
        public string coreRole;
        public string rawJson;
    }
}
