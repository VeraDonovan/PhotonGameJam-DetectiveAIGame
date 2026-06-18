using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public sealed class ChoiceEntry : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;

    private string choiceId;
    private Action<ChoiceEntry> onSelected;

    public string ChoiceId => choiceId;

    public void Initialize(string id, string name, Action<ChoiceEntry> callback, Sprite icon)
    {
        choiceId = id;
        titleText.text = name;
        iconImage.sprite = icon;
        onSelected = callback;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(this));
    }

    public void SetSelected(bool isSelected)
    {
        button.interactable = !isSelected;
    }
}
