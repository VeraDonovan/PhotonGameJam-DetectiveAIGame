using System;
using System.Collections.Generic;

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
        public NpcAiPlayerSideBoundaryData playerSideBoundary;
        public NpcAiRoleplayStatsData roleplayStats;
        public NpcAiIdentityData identity;
        public NpcAiLifeBackgroundData lifeBackground;
        public NpcAiPsychologyData psychology;
        public NpcAiSpeechStyleData speechStyle;
        public NpcAiKnowledgeModelData knowledgeModel;
        public NpcAiSearchPhaseRoleplayData searchPhaseRoleplay;
        public NpcAiInterrogationModesData interrogationModes;
        public List<NpcAiTopicGuideData> topicGuides = new List<NpcAiTopicGuideData>();
        public NpcAiInterrogationStageBehaviorData interrogationStageBehavior;
        public NpcAiSamplePhrasingsData samplePhrasings;
    }

    [Serializable]
    public sealed class NpcAiPlayerSideBoundaryData
    {
        public string publicProfileSource;
        public string statementSource;
        public string truthSource;
        public string rule;
    }

    [Serializable]
    public sealed class NpcAiRoleplayStatsData
    {
        public int baseSuspicion;
        public int lieTendency;
        public int stubbornness;
        public int emotionalVolatility;
        public int chanceOfGivingInEarly;
        public int chanceOfGivingInUnderPressure;
        public int selfControl;
    }

    [Serializable]
    public sealed class NpcAiIdentityData
    {
        public string publicRole;
        public string privateRole;
        public string selfImage;
        public string currentGoal;
    }

    [Serializable]
    public sealed class NpcAiLifeBackgroundData
    {
        public string summary;
        public List<string> importantPast = new List<string>();
        public List<string> dailyLifeMarkers = new List<string>();
    }

    [Serializable]
    public sealed class NpcAiPsychologyData
    {
        public string surfaceEmotion;
        public string hiddenEmotion;
        public string coreFear;
        public string coreWound;
        public List<string> triggerPoints = new List<string>();
    }

    [Serializable]
    public sealed class NpcAiSpeechStyleData
    {
        public string tone;
        public List<string> sentenceStyle = new List<string>();
        public List<string> avoidances = new List<string>();
        public List<string> bodyLanguage = new List<string>();
    }

    [Serializable]
    public sealed class NpcAiKnowledgeModelData
    {
        public List<string> knowsForCertain = new List<string>();
        public List<string> believes = new List<string>();
        public List<string> willHide = new List<string>();
    }

    [Serializable]
    public sealed class NpcAiSearchPhaseRoleplayData
    {
        public string goal;
        public List<string> allowedTopics = new List<string>();
        public List<string> responsePattern = new List<string>();
    }

    [Serializable]
    public sealed class NpcAiInterrogationModesData
    {
        public NpcAiInterrogationModeData baseline;
        public NpcAiInterrogationModeData whenContradicted;
        public NpcAiInterrogationModeData whenPresentedWithCleanupEvidence;
        public NpcAiInterrogationModeData whenPresentedWithAbuseEvidence;
        public NpcAiInterrogationModeData whenPresentedWithLampEvidence;
    }

    [Serializable]
    public sealed class NpcAiInterrogationModeData
    {
        public string demeanor;
        public string tactic;
    }

    [Serializable]
    public sealed class NpcAiTopicGuideData
    {
        public string topicId;
        public string intent;
        public string goodAIResponseShape;
        public string badAIResponseShape;
        public NpcAiSampleLinesData sampleLines;
    }

    [Serializable]
    public sealed class NpcAiSampleLinesData
    {
        public List<string> search = new List<string>();
        public List<string> deflect = new List<string>();
        public List<string> pressured = new List<string>();
        public List<string> collapse = new List<string>();
    }

    [Serializable]
    public sealed class NpcAiInterrogationStageBehaviorData
    {
        public string stage0_search;
        public string stage1_shake;
        public string stage2_collapse;
    }

    [Serializable]
    public sealed class NpcAiSamplePhrasingsData
    {
        public List<string> neutral = new List<string>();
        public List<string> defensive = new List<string>();
        public List<string> cracking = new List<string>();
        public List<string> collapsed = new List<string>();
    }
}
