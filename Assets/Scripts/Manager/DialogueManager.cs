using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance;   // 单例
    public TMP_Text dialogueText;                // 显示对话的文本
    public GameObject dialoguePanel;          // 对话面板（整个UI容器）

    void Awake() {
        // 确保单例唯一
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }

        // 默认隐藏对话面板
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示一段对话
    /// </summary>
    public void ShowDialogue(string text) {
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(true);
        }
        if (dialogueText != null) {
            dialogueText.text = text;
        }
    }

    /// <summary>
    /// 隐藏对话面板
    /// </summary>
    public void HideDialogue() {
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(false);
        }
    }
}
