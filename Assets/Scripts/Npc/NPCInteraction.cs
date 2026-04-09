using UnityEngine;

public class NPCInteraction : MonoBehaviour {
    private NPCAssembler assembler;

    void Awake() {
        assembler = GetComponent<NPCAssembler>();
    }

    void OnMouseDown() {
        if (assembler == null || assembler.dialogueConfig == null) {
            Debug.LogError("❌ NPCAssembler 或 dialogueConfig 没有设置");
            return;
        }

        // 点击 NPC 时，显示初始陈述
        string initialLine = assembler.dialogueConfig.initialStatement;
        DialogueManager.Instance.ShowDialogue(initialLine);

        Debug.Log($"👆 点击了NPC: {assembler.dialogueConfig.displayName}, 初始陈述: {initialLine}");
    }
}
