using UnityEngine;

public class NPCInteractionPrompt : MonoBehaviour
{
    public float interactDistance = 2.5f;   // 玩家与 NPC 的交互距离
    public Transform player;                // 玩家对象引用
    public GameObject fPrompt;              // 提示 UI Canvas
    public GameObject dialoguePanel;        // 对话面板引用

    void Start()
    {
        if (fPrompt != null)
            fPrompt.SetActive(false); // 默认隐藏提示 UI
    }

    void Update()
    {
        if (player == null || fPrompt == null) return;

        // 如果对话面板正在打开，就强制关闭提示
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            fPrompt.SetActive(false);
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        // 玩家靠近时显示提示
        if (distance <= interactDistance)
        {
            fPrompt.SetActive(true);
        }
        else
        {
            fPrompt.SetActive(false); // 玩家离开时隐藏提示
        }
    }

    public void HidePrompt()
    {
        if (fPrompt != null)
            fPrompt.SetActive(false);
    }
}
