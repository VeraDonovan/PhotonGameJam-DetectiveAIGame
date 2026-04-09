[System.Serializable]
    public class NPC_appearance_Set {
    public NPCConfig[] npcs;
}

[System.Serializable]
public class NPCConfig {
    public string npc_id;
    public string name;
    public Appearance appearance;
}

[System.Serializable]
public class Appearance {
    public string head;
    public string body;
    public string outfit;
}
