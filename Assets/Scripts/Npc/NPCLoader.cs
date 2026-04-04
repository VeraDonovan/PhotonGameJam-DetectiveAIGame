using UnityEngine;
using System.IO;

public class NPCLoader {
    public static NPCConfig LoadNPCConfig(string fileName) {
        string path = Path.Combine(Application.dataPath, "_DATA/NPC/" + fileName);
        if (!File.Exists(path)) {
            Debug.LogError("❌ NPC 配置文件未找到: " + path);
            return null;
        }
        string json = File.ReadAllText(path);
        Debug.Log("json 内容: " + json);

         NPCConfig npcConfig = JsonUtility.FromJson<NPCConfig>(json);
        Debug.Log(npcConfig == null ? "⚠️ npcConfig 是 NULL" : "✅ npcConfig 已创建");

        // 检查子对象
        if (npcConfig != null) {
            Debug.Log("NPC ID: " + npcConfig.npc_id);
            Debug.Log("NPC 名字: " + npcConfig.name);
            Debug.Log(npcConfig.appearance == null ? "⚠️ appearance 是 NULL" : "✅ appearance 已创建");

            if (npcConfig.appearance != null) {
                Debug.Log("Head: " + npcConfig.appearance.head);
                Debug.Log("Body: " + npcConfig.appearance.body);
                Debug.Log("Outfit: " + npcConfig.appearance.outfit);
            }
        }

        return npcConfig;
    }
}
