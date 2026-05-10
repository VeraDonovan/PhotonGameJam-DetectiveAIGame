using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class StatementSetData
    {
        public string templateVersion;
        public string schemaType;
        public LanguageData language;
        public string caseId;
        public StatementUiRulesData uiRules;
        public List<StatementTopicData> statementTopics = new List<StatementTopicData>();
    }

    [Serializable]
    public sealed class StatementUiRulesData
    {
        public string replacementMode;
        public string historyStorage;
        public string suspectPanelDisplay;
    }

    [Serializable]
    public sealed class StatementTopicData
    {
        public string topicId;
        public string npcId;
        public string displayName;
        public int sortOrder;
        public bool suspectDetailVisible;
        public List<StatementEntryData> entries = new List<StatementEntryData>();
    }

    [Serializable]
    public sealed class StatementEntryData
    {
        public string statementId;
        public string status;
        public string phase;
        public string text;
        public List<string> unlockRequirements = new List<string>();
        public string replacesStatementId;
    }
}
