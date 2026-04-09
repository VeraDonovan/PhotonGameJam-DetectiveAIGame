using UnityEngine;
using TMPro;

public class DialogueController : MonoBehaviour {
    public static DialogueController Instance;
    public NPCData currentNPC;
    public TMP_InputField playerInputField;  // 输入框引用

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    // 按钮点击时调用
    public void OnSubmitButtonClick() {
        string input = playerInputField.text;
        ProcessPlayerInput(input);
    }

    public async void ProcessPlayerInput(string playerInput) {
        if (currentNPC == null) {
            Debug.LogError("❌ 当前NPC未设置，无法进行对话");
            return;
        }

        string prompt = $"玩家问：{playerInput}\n" +
                        $"NPC背景：{currentNPC.backstory}\n" +
                        $"NPC初始陈述：{currentNPC.initialStatement}\n" +
                        $"请以NPC {currentNPC.displayName} 的身份回答。";

        Debug.Log("📨 Prompt: " + prompt);

        // 暂时用假数据代替 AI
        string aiResponse = $"NPC {currentNPC.displayName} 回复：你好，我收到了你的输入：{playerInput}";

        DialogueManager.Instance.ShowDialogue(aiResponse);
    }

    public void SetCurrentNPC(NPCData npc) {
        currentNPC = npc;
        Debug.Log("🎯 当前对话NPC已设置为: " + npc.displayName);
    }
}
