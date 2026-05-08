using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    public void ShowDialogue(string text)
    {
        dialogueText.text = text;
        dialoguePanel.SetActive(true);
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
