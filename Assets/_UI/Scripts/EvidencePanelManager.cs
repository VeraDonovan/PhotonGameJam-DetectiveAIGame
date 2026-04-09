using System.Collections.Generic;
using System;
using DetectiveGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveGame.UI
{
    public sealed class EvidencePanelManager : MonoBehaviour
    {
        [Header("Icon View")]
        [SerializeField] private Sprite defaultEvidenceIcon;
        [SerializeField] private Transform iconContentRoot;
        [SerializeField] private EvidenceIconEntry iconEntryPrefab;

        [Header("Detail View")]
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private string defaultDetailText = string.Empty;

        [Header("Progress View")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressFill;

        private readonly Dictionary<string, EvidenceIconEntry> entriesById = new Dictionary<string, EvidenceIconEntry>();

        private EventManager eventManager;
        private ProgressManager progressManager;
        private EvidenceDatabase evidenceDatabase;
        private EvidenceIconEntry selectedEntry;

        private void Awake()
        {
            ResolveCoreReferences();
            ValidateConfiguration();
            SetDetailText(defaultDetailText);
            RefreshProgressView();
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
            eventManager = appRoot.EventManager;
            progressManager = appRoot.ProgressManager;
            evidenceDatabase = appRoot.DatabaseManager.EvidenceDatabase;

            Debug.Log(
                $"[EvidencePanelManager] Core references resolved. EventManagerReady={eventManager != null}, ProgressManagerReady={progressManager != null}, EvidenceDatabaseReady={evidenceDatabase != null}.");
        }

        private void ValidateConfiguration()
        {
            if (eventManager == null)
            {
                throw new InvalidOperationException("EvidencePanelManager requires AppRoot.EventManager.");
            }

            if (progressManager == null)
            {
                throw new InvalidOperationException("EvidencePanelManager requires AppRoot.ProgressManager.");
            }

            if (evidenceDatabase == null)
            {
                throw new InvalidOperationException("EvidencePanelManager requires AppRoot.DatabaseManager.EvidenceDatabase.");
            }

            if (iconContentRoot == null)
            {
                throw new InvalidOperationException("EvidencePanelManager requires iconContentRoot to be assigned.");
            }

            if (iconEntryPrefab == null)
            {
                throw new InvalidOperationException("EvidencePanelManager requires iconEntryPrefab to be assigned.");
            }
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
            Debug.Log(
                $"[EvidencePanelManager] Received EvidenceAddedEvent for '{eventData.EvidenceId}'. Updating evidence UI.");
            AddEvidenceEntry(eventData.EvidenceId);
            RefreshProgressView();
        }

        private void RefreshFromRuntimeState()
        {
            Debug.Log(
                $"[EvidencePanelManager] Refreshing from runtime state. CollectedEvidenceCount={progressManager.CollectedEvidenceIds.Count}.");
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
                if (entriesById.ContainsKey(evidenceId))
                {
                    Debug.Log(
                        $"[EvidencePanelManager] Skipped adding entry for '{evidenceId}' because it already exists.");
                }

                return;
            }

            if (!evidenceDatabase.TryGetEvidence(evidenceId, out var evidenceData))
            {
                Debug.LogWarning(
                    $"[EvidencePanelManager] EvidenceDatabase lookup failed for '{evidenceId}'.");
                return;
            }

            var entry = Instantiate(iconEntryPrefab, iconContentRoot);
            entry.Initialize(
                evidenceId,
                evidenceData.displayName,
                evidenceData.summary,
                defaultEvidenceIcon,
                HandleEntrySelected);

            entriesById.Add(evidenceId, entry);
            Debug.Log(
                $"[EvidencePanelManager] Added evidence entry for '{evidenceId}' ({evidenceData.displayName}). EntryCount={entriesById.Count}.");

            if (selectedEntry == null)
            {
                HandleEntrySelected(entry);
            }
        }

        private void HandleEntrySelected(EvidenceIconEntry entry)
        {
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
            detailText.text = value ?? string.Empty;
        }

        public void SetEvidenceDatabase(EvidenceDatabase database)
        {
            evidenceDatabase = database;
            Debug.Log(
                $"[EvidencePanelManager] Evidence database assigned through SetEvidenceDatabase. EvidenceCount={evidenceDatabase.EvidenceById.Count}.");
            RefreshFromRuntimeState();
        }

        private void RefreshProgressView()
        {
            var collectedCount = progressManager.CollectedEvidenceIds.Count;
            var totalCount = evidenceDatabase.EvidenceById.Count;
            var progressValue = totalCount > 0 ? (float)collectedCount / totalCount : 0f;
            var progressPercent = Mathf.RoundToInt(progressValue * 100f);
            var progressLabel = $"{progressPercent} %";

            Debug.Log(
                $"[EvidencePanelManager] RefreshProgressView collected={collectedCount}, total={totalCount}, percent={progressPercent}, fill={progressValue:0.00}.");

            progressText.text = progressLabel;
            progressFill.fillAmount = progressValue;
        }
    }
}
