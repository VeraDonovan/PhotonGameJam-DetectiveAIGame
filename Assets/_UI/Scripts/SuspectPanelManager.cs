using System;
using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;
using TMPro;
using UnityEngine;

namespace DetectiveGame.UI
{
    public sealed class SuspectPanelManager : MonoBehaviour
    {
        [Header("Icon View")]
        [SerializeField] private Sprite defaultSuspectIcon;
        [SerializeField] private Transform iconContentRoot;
        [SerializeField] private SuspectIconEntry iconEntryPrefab;

        [Header("Detail View")]
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private string defaultDetailText = string.Empty;

        private readonly Dictionary<string, SuspectIconEntry> entriesByNpcId = new Dictionary<string, SuspectIconEntry>();

        private NpcRuntimeManager npcRuntimeManager;
        private NpcDatabase npcDatabase;
        private FactDatabase factDatabase;
        private EventManager eventManager;
        private SuspectIconEntry selectedEntry;

        private void Awake()
        {
            ResolveCoreReferences();
            ValidateConfiguration();
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
            eventManager = appRoot.EventManager;
            npcRuntimeManager = appRoot.NpcRuntimeManager;
            npcDatabase = appRoot.DatabaseManager.NpcDatabase;
            factDatabase = appRoot.DatabaseManager.FactDatabase;
        }

        private void ValidateConfiguration()
        {
            if (eventManager == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires AppRoot.EventManager.");
            }

            if (npcDatabase == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires AppRoot.DatabaseManager.NpcDatabase.");
            }

            if (npcRuntimeManager == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires AppRoot.NpcRuntimeManager.");
            }

            if (factDatabase == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires AppRoot.DatabaseManager.FactDatabase.");
            }

            if (iconContentRoot == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires iconContentRoot to be assigned.");
            }

            if (iconEntryPrefab == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires iconEntryPrefab to be assigned.");
            }
        }

        private void SubscribeToEvents()
        {
            eventManager.Subscribe<NpcDiscoveredEvent>(HandleNpcDiscovered);
            eventManager.Subscribe<FactUnlockedEvent>(HandleFactUnlocked);
        }

        private void UnsubscribeFromEvents()
        {
            eventManager.Unsubscribe<NpcDiscoveredEvent>(HandleNpcDiscovered);
            eventManager.Unsubscribe<FactUnlockedEvent>(HandleFactUnlocked);
        }

        private void RefreshFromRuntimeState()
        {
            EnsureSuspectEntries(npcRuntimeManager.DiscoveredNpcIds);
            RefreshSelectedDetail();
        }

        private void HandleNpcDiscovered(NpcDiscoveredEvent eventData)
        {
            EnsureSuspectEntries(new[] { eventData.NpcId });
            RefreshSelectedDetail();
        }

        private void EnsureSuspectEntries(IEnumerable<string> npcIds)
        {
            foreach (var npcId in npcIds)
            {
                if (string.IsNullOrWhiteSpace(npcId) || !npcDatabase.TryGetNpc(npcId, out var npc))
                {
                    continue;
                }

                if (npc == null || !string.Equals(npc.roleType, "suspect", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entriesByNpcId.ContainsKey(npcId))
                {
                    continue;
                }

                var baseDetailText = BuildBaseDetailText(npc.npcId);
                var entry = Instantiate(iconEntryPrefab, iconContentRoot);
                entry.Initialize(
                    npc.npcId,
                    npc.displayName,
                    baseDetailText,
                    defaultSuspectIcon,
                    HandleEntrySelected);

                entriesByNpcId.Add(npcId, entry);

                if (selectedEntry == null)
                {
                    HandleEntrySelected(entry);
                }
            }
        }

        private void HandleFactUnlocked(FactUnlockedEvent eventData)
        {
            if (!factDatabase.TryGetFact(eventData.FactId, out var fact) || fact == null)
            {
                return;
            }

            var updatedSelectedEntry = false;
            foreach (var npcId in fact.scope?.relatedNpcIds ?? new List<string>())
            {
                if (!entriesByNpcId.TryGetValue(npcId, out var entry))
                {
                    continue;
                }

                AppendFactToEntry(entry, fact.summary);
                updatedSelectedEntry |= selectedEntry == entry;
            }

            if (updatedSelectedEntry)
            {
                RefreshSelectedDetail();
            }
        }

        private string BuildBaseDetailText(string npcId)
        {
            if (!npcDatabase.TryGetNpc(npcId, out var npc) || npc == null)
            {
                return string.Empty;
            }

            var lines = new List<string>();

            AddIfNotBlank(lines, npc.relationshipToVictim);
            AddIfNotBlank(lines, npc.initialStatement);

            if (lines.Count == 0)
            {
                return defaultDetailText ?? string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append("- ");
                builder.Append(lines[i]);
            }

            return builder.ToString();
        }

        private static void AddIfNotBlank(List<string> lines, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                lines.Add(value.Trim());
            }
        }

        private void AppendFactToEntry(SuspectIconEntry entry, string factSummary)
        {
            if (entry == null || string.IsNullOrWhiteSpace(factSummary))
            {
                return;
            }

            var trimmedSummary = factSummary.Trim();
            if (entry.DetailText.Contains(trimmedSummary, StringComparison.Ordinal))
            {
                return;
            }

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(entry.DetailText))
            {
                builder.Append(entry.DetailText.TrimEnd());
                builder.AppendLine();
            }

            builder.Append("- ");
            builder.Append(trimmedSummary);
            entry.SetDetailText(builder.ToString());
        }

        private void HandleEntrySelected(SuspectIconEntry entry)
        {
            if (selectedEntry != null)
            {
                selectedEntry.SetSelected(false);
            }

            selectedEntry = entry;
            selectedEntry.SetSelected(true);
            RefreshSelectedDetail();
        }

        private void RefreshSelectedDetail()
        {
            SetDetailText(selectedEntry != null ? selectedEntry.DetailText : defaultDetailText);
        }

        private void SetDetailText(string value)
        {
            if (detailText != null)
            {
                detailText.text = value ?? string.Empty;
            }
        }
    }
}
