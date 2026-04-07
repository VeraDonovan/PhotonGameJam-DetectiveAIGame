using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveGame.UI
{
    public sealed class EvidenceIconEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;

        private string evidenceId = string.Empty;
        private string detailText = string.Empty;
        private Action<EvidenceIconEntry> onSelected;

        public string EvidenceId => evidenceId;
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
            string summary,
            Sprite iconSprite,
            Action<EvidenceIconEntry> selectedCallback)
        {
            evidenceId = id ?? string.Empty;
            detailText = summary ?? string.Empty;
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
            Debug.Log($"[EvidenceIconEntry] Clicked evidence entry '{evidenceId}'.");
            onSelected?.Invoke(this);
        }
    }
}
