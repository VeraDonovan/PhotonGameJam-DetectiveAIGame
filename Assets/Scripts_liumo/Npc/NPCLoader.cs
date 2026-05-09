using UnityEngine;

public class NPCLoader : MonoBehaviour {
    public static NPCLoader Instance;
    public NPC_appearance_Set npcSet;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() {
        Debug.Log("loader start");
        TextAsset jsonFile = Resources.Load<TextAsset>("npc_appearance_set");
        if (jsonFile == null) {
            Debug.LogError("❌ 没找到 npc_appearance_set.json，请确认放在 Resources 文件夹里");
            return;
        }
        Debug.Log("📄 JSON 原始内容:\n" + jsonFile.text);
        npcSet = JsonUtility.FromJson<NPC_appearance_Set>(jsonFile.text);
    //      if (npcSet == null) {
    //     Debug.LogError("❌ 解析失败，npcSet 是 null");
    //     return;
    // }

    // if (npcSet.npcs == null) {
    //     Debug.LogError("❌ 解析失败，npcSet.npcs 是 null");
    //     return;
    // }
    //     Debug.Log("✅ 已加载 NPC 集合，数量: " + npcSet.npcs.Length);
    //         for (int i = 0; i < npcSet.npcs.Length; i++) {
    //     NPCConfig config = npcSet.npcs[i];
    //     Debug.Log($"NPC[{i}] id={config.npc_id}, name={config.name}, head={config.appearance.head}, body={config.appearance.body}, outfit={config.appearance.outfit}");
    // }
    }
}
