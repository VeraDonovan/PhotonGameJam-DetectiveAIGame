using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance;
    public TMP_Text dialogueText;
    public GameObject dialoguePanel;

    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float punctuationDelay = 0.2f;
    [SerializeField] private bool enablePunctuationDelay = true;

    private TMPTypeWriter typeWriter;
    private readonly SortedDictionary<int, string> orderedDialogueQueue = new SortedDictionary<int, string>();
    private int nextOrderedDialogueId;
    private bool hasOrderedDialogue;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }

        if (dialoguePanel != null) {
            dialoguePanel.SetActive(false);
        }

        if (dialogueText != null) {
            typeWriter = new TMPTypeWriter(dialogueText, typingSpeed, punctuationDelay, enablePunctuationDelay);
            typeWriter.SetCompleteFunc(ShowNextOrderedDialogue);
        }
    }

    public void ShowDialogue(string text) {
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(true);
        }

        if (typeWriter != null) {
            typeWriter.StartTyping(text);
        } else if (dialogueText != null) {
            dialogueText.text = text;
        }
    }

    public void ReserveOrderedDialogue(int dialogueId) {
        if (!hasOrderedDialogue || dialogueId < nextOrderedDialogueId) {
            nextOrderedDialogueId = dialogueId;
            hasOrderedDialogue = true;
        }
    }

    public void ShowDialogueInOrder(int dialogueId, string text) {
        orderedDialogueQueue[dialogueId] = text;
        ShowNextOrderedDialogue();
    }

    public void HideDialogue() {
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(false);
        }
    }

    private void ShowNextOrderedDialogue() {
        if (typeWriter == null || typeWriter.IsTyping() || !hasOrderedDialogue) {
            return;
        }

        if (!orderedDialogueQueue.TryGetValue(nextOrderedDialogueId, out string text)) {
            return;
        }

        orderedDialogueQueue.Remove(nextOrderedDialogueId);
        nextOrderedDialogueId++;
        ShowDialogue(text);
    }

    private void OnDestroy() {
        typeWriter?.OnDestroy();
    }
}
