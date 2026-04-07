using System.Collections.Generic;
using DetectiveGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveGame.UI
{
    public sealed class EvidencePanelManager : MonoBehaviour
    {
        [Header("Icon View")]
        [SerializeField] private Transform iconContentRoot;
        [SerializeField] private EvidenceIconEntry iconEntryPrefab;

        [Header("Detail View")]
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private string defaultDetailText = string.Empty;

        [Header("Progress View")]
        [SerializeField] private TMP_Text[] progressTexts;
        [SerializeField] private string progressFormat = "{0}/{1}";
        [SerializeField] private Image progressFill;

        private readonly Dictionary<string, EvidenceIconEntry> entriesById = new Dictionary<string, EvidenceIconEntry>();

        private EventManager eventManager;
        private ProgressManager progressManager;
        private EvidenceDatabase evidenceDatabase;
        private EvidenceIconEntry selectedEntry;

        private void Awake()
        {
            ResolveCoreReferences();
            SetDetailText(defaultDetailText);
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            RefreshFromRuntimeState();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void ResolveCoreReferences()
        {
            var appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                return;
            }

            eventManager = appRoot.EventManager;
            progressManager = appRoot.ProgressManager;
        }

        private void SubscribeToEvents()
        {
            eventManager?.Subscribe<EvidenceAddedEvent>(HandleEvidenceAdded);
        }

        private void UnsubscribeFromEvents()
        {
            eventManager?.Unsubscribe<EvidenceAddedEvent>(HandleEvidenceAdded);
        }

        private void HandleEvidenceAdded(EvidenceAddedEvent eventData)
        {
            AddEvidenceEntry(eventData.EvidenceId);
            RefreshProgressView();
        }

        private void RefreshFromRuntimeState()
        {
            if (progressManager == null)
            {
                RefreshProgressView();
                return;
            }

            foreach (var evidenceId in progressManager.CollectedEvidenceIds)
            {
                AddEvidenceEntry(evidenceId);
            }

            RefreshProgressView();
        }

        private void AddEvidenceEntry(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId) || entriesById.ContainsKey(evidenceId))
            {
                return;
            }

            if (iconContentRoot == null || iconEntryPrefab == null || evidenceDatabase == null)
            {
                return;
            }

            if (!evidenceDatabase.TryGetEvidence(evidenceId, out var evidenceData))
            {
                return;
            }

            var entry = Instantiate(iconEntryPrefab, iconContentRoot);
            entry.Initialize(
                evidenceId,
                evidenceData.displayName,
                evidenceData.summary,
                null,
                HandleEntrySelected);

            entriesById.Add(evidenceId, entry);

            if (selectedEntry == null)
            {
                HandleEntrySelected(entry);
            }
        }

        private void HandleEntrySelected(EvidenceIconEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (selectedEntry != null)
            {
                selectedEntry.SetSelected(false);
            }

            selectedEntry = entry;
            selectedEntry.SetSelected(true);
            SetDetailText(entry.DetailText);
        }

        private void SetDetailText(string value)
        {
            if (detailText != null)
            {
                detailText.text = value ?? string.Empty;
            }
        }

        public void SetEvidenceDatabase(EvidenceDatabase database)
        {
            evidenceDatabase = database;
            RefreshFromRuntimeState();
        }

        private void RefreshProgressView()
        {
            var collectedCount = progressManager != null ? progressManager.CollectedEvidenceIds.Count : 0;
            var totalCount = evidenceDatabase != null ? evidenceDatabase.EvidenceById.Count : 0;
            var progressValue = totalCount > 0 ? (float)collectedCount / totalCount : 0f;
            var progressLabel = string.Format(progressFormat, collectedCount, totalCount);

            if (progressTexts != null)
            {
                for (var i = 0; i < progressTexts.Length; i++)
                {
                    if (progressTexts[i] != null)
                    {
                        progressTexts[i].text = progressLabel;
                    }
                }
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = progressValue;
            }
        }
    }
}
