using UnityEngine;
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour {
    public Transform player;              // 玩家对象
    public GameObject promptText;         // 提示UI，比如一个Text或Image
    public float interactDistance = 3f;   // 交互距离

    private NPCAssembler assembler;

    void Awake() {
        assembler = GetComponent<NPCAssembler>();
        promptText.SetActive(false); // 默认隐藏提示
    }

    void Update() {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= interactDistance) {
            // 显示提示
            promptText.SetActive(true);
            promptText.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);

            // 检测玩家是否按下 F 键
            if (Input.GetKeyDown(KeyCode.F)) {
                OnInteract();
            }
        } else {
            promptText.SetActive(false);
        }
    }

    void OnInteract() {
        Debug.Log("👉 玩家按下 F 与 NPC 交互: " + assembler.dialogueConfig.displayName);

        // 这里可以选择不同功能，比如进入对话
        if (DialogueController.Instance != null)
        {
            DialogueController.Instance.SetCurrentNPC(assembler.dialogueConfig);
            DialogueController.Instance.StartNpcOpeningDialogue();
        }

        // 或者执行其他功能
        // Debug.Log("执行其他功能");
    }
}
