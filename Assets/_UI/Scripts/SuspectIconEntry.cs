using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveGame.UI
{
    public sealed class SuspectIconEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;

        private string npcId = string.Empty;
        private string detailText = string.Empty;
        private Action<SuspectIconEntry> onSelected;

        public string NpcId => npcId;
        public string DetailText => detailText;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Initialize(
            string id,
            string displayName,
            string initialDetailText,
            Sprite iconSprite,
            Action<SuspectIconEntry> selectedCallback)
        {
            npcId = id ?? string.Empty;
            detailText = initialDetailText ?? string.Empty;
            onSelected = selectedCallback;

            if (titleText != null)
            {
                titleText.text = displayName ?? string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.enabled = iconSprite != null;
            }
        }

        public void SetDetailText(string value)
        {
            detailText = value ?? string.Empty;
        }

        public void SetSelected(bool isSelected)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = !isSelected;
        }

        private void HandleClicked()
        {
            onSelected?.Invoke(this);
        }
    }
}
