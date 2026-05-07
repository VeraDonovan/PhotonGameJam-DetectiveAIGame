using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChoiceEntry : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;

    private string choiceId;
    private Action<ChoiceEntry> onSelected;

    public string ChoiceId => choiceId;

    public void Initialize(string id, string displayName, Action<ChoiceEntry> callback)
    {
        choiceId = id;
        titleText.text = displayName;
        onSelected = callback;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(this));
    }

    public void SetSelected(bool isSelected)
    {
        button.interactable = !isSelected;
    }
}
