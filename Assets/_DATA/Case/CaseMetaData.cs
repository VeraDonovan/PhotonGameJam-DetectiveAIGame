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
        public CaseBackgroundData caseBackground;
    }

    [Serializable]
    public sealed class CaseBackgroundData
    {
        public string briefBackground;
    }
}
