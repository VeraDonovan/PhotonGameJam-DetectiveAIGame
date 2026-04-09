using System;
using UnityEngine;

[Serializable]
public class NPCSet {
    public string templateVersion;
    public string schemaType;
    public Language language;
    public string caseId;
    public NPCData[] npcs; // NPC数组
}

[Serializable]
public class Language {
    public string schema;
    public string content;
}

[Serializable]
public class NPCData {
    public string npcId;
    public string roleType;
    public string displayName;
    public int age;
    public string locationId;
    public string relationshipToVictim;
    public string archetype;
    public bool isKiller;
    public string backstory;
    public string motive;
    public string hiddenTruth;
    public string[] knowledge;
    public Stats stats;
    public string initialState;
    public string initialStatement;
    public InterrogationLayer[] interrogationLayers;
}

[Serializable]
public class Stats {
    public int composure;
    public int lie;
    public int aggression;
    public int cooperation;
    public int guilt;
    public int trauma;
}

[Serializable]
public class InterrogationLayer {
    public int layer;
    public string topic;
    public int pressureGoal;
    public string unlockLine;
    public string collapseLine;
    public Breakpoint[] breakpoints;
}

[Serializable]
public class Breakpoint {
    public string prompt;
    public string excuse;
    public int pressure;
}

