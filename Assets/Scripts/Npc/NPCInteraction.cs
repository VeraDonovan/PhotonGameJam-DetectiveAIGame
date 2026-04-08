using UnityEngine;
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour {
    public Transform player;              // 玩家对象
    public GameObject interactionPanel;   // 包含两个按钮的UI面板
    public Button talkButton;
    public Button otherButton;

    public float interactDistance = 3f;   // 交互距离

    private NPCAssembler assembler;

    void Awake() {
        assembler = GetComponent<NPCAssembler>();
        interactionPanel.SetActive(false);

        // 按钮绑定事件
        talkButton.onClick.AddListener(OnTalkClicked);
        otherButton.onClick.AddListener(OnOtherClicked);
    }

    void Update() {
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= interactDistance) {
            interactionPanel.SetActive(true);
            Debug.Log("遇到的npc的姓名是: " + assembler.dialogueConfig.displayName);
            // 让面板跟随 NPC 出现（在 NPC 上方）
            interactionPanel.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        } else {
            interactionPanel.SetActive(false);
        }
    }

    void OnTalkClicked() {
        Debug.Log("👉 点击了对话按钮");
        Debug.Log("📊 NPC数据: " + assembler.dialogueConfig.displayName);
        DialogueManager.Instance.ShowDialogue(assembler.dialogueConfig.initialStatement);
    }

    void OnOtherClicked() {
        Debug.Log("👉 点击了另一个功能");
    }
}
