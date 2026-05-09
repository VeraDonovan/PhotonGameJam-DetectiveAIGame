using UnityEngine;
using System;

public class NPCSpawner : MonoBehaviour {
    public GameObject npcPrefab; // 在 Inspector 拖入你的 NPC 预制体

    void Start() {
        Debug.Log("🔍 正在加载 NPC 集合...");
        NPC_appearance_Set npcSet = NPCLoader.Instance.npcSet;
        NPCSet dialogueSet = DialogueLoader.Instance.npcSet;

        if (npcSet == null || npcSet.npcs == null) {
            Debug.LogError("❌ 没有加载到 NPC 外貌数据");
            return;
        }
        if (dialogueSet == null || dialogueSet.npcs == null) {
            Debug.LogError("❌ 没有加载到 NPC 对话数据");
            return;
        }

        for (int i = 0; i < npcSet.npcs.Length; i++) {
            NPCConfig appearance = npcSet.npcs[i];
            NPCData dialogue = Array.Find(dialogueSet.npcs, d => d.npcId == appearance.npc_id);

            if (dialogue == null) {
                Debug.LogWarning($"⚠️ 没找到 NPC 对话数据: {appearance.npc_id}");
                continue;
            }

            Vector3 spawnPos = new Vector3(i * 2, 0, 0);
            GameObject npcObj = Instantiate(npcPrefab, spawnPos, Quaternion.identity);

            NPCAssembler assembler = npcObj.GetComponent<NPCAssembler>();
            assembler.AssembleNPC(appearance, dialogue);

            Debug.Log($"✅ 生成 NPC: {appearance.name} (ID: {appearance.npc_id})");
        }
    }
}
