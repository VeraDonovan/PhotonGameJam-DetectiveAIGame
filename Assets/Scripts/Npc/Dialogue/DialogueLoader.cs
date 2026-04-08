using UnityEngine;

public class DialogueLoader : MonoBehaviour {
    public static DialogueLoader Instance;   // 单例
    public NPCSet npcSet;                    // 存储所有NPC的集合

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        // 加载 Resources/npc.json
        TextAsset jsonFile = Resources.Load<TextAsset>("npc");
        if (jsonFile == null) {
            Debug.LogError("❌ 没找到 npc.json，请确认文件放在 Resources 文件夹里");
            return;
        }

        npcSet = JsonUtility.FromJson<NPCSet>(jsonFile.text);
        Debug.Log("✅ 已加载 NPC 集合，数量: " + npcSet.npcs.Length);
    }

    /// <summary>
    /// 根据 npcId 查找对应的 NPC
    /// </summary>
    public NPCData FindNPCById(string npcId) {
        if (npcSet == null || npcSet.npcs == null) {
            Debug.LogError("❌ NPC集合未加载");
            return null;
        }

        foreach (var npc in npcSet.npcs) {
            if (npc.npcId == npcId) {
                return npc;
            }
        }

        Debug.LogWarning("⚠️ 没找到 NPC: " + npcId);
        return null;
    }
}
