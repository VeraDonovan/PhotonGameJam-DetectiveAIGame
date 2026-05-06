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

        private NpcDatabase npcDatabase;
        private StatementDatabase statementDatabase;
        private EventManager eventManager;
        private ProgressManager progressManager;
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
            npcDatabase = appRoot.DatabaseManager.NpcDatabase;
            statementDatabase = appRoot.DatabaseManager.StatementDatabase;
            progressManager = appRoot.ProgressManager;
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

            if (statementDatabase == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires AppRoot.DatabaseManager.StatementDatabase.");
            }

            if (progressManager == null)
            {
                throw new InvalidOperationException("SuspectPanelManager requires AppRoot.ProgressManager.");
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
            eventManager.Subscribe<StatementUnlockedEvent>(HandleStatementUnlocked);
        }

        private void UnsubscribeFromEvents()
        {
            eventManager.Unsubscribe<StatementUnlockedEvent>(HandleStatementUnlocked);
        }

        private void RefreshFromRuntimeState()
        {
            EnsureSuspectEntries(npcDatabase.NpcById.Keys);
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

                var baseDetailText = BuildDetailText(npc.npcId);
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

        private void HandleStatementUnlocked(StatementUnlockedEvent eventData)
        {
            if (!statementDatabase.TryGetStatement(eventData.StatementId, out var statement) || statement == null)
            {
                return;
            }

            foreach (var npc in npcDatabase.NpcById.Values)
            {
                if (!entriesByNpcId.TryGetValue(npc.npcId, out var entry))
                {
                    continue;
                }

                entry.SetDetailText(BuildDetailText(npc.npcId));
            }

            RefreshSelectedDetail();
        }

        private string BuildDetailText(string npcId)
        {
            if (!npcDatabase.TryGetNpc(npcId, out var npc) || npc == null)
            {
                return string.Empty;
            }

            var lines = new List<string>();

            AddIfNotBlank(lines, npc.profileText);
            AppendLatestUnlockedTopicStatements(lines, npcId);

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

        private void AppendLatestUnlockedTopicStatements(List<string> lines, string npcId)
        {
            var topics = new List<StatementTopicData>(statementDatabase.GetTopicsByNpc(npcId));
            topics.Sort((left, right) => left.sortOrder.CompareTo(right.sortOrder));

            foreach (var topic in topics)
            {
                if (topic == null || !topic.suspectDetailVisible)
                {
                    continue;
                }

                var latestEntry = GetLatestUnlockedStatement(topic.topicId);
                if (latestEntry == null)
                {
                    continue;
                }

                AddIfNotBlank(lines, latestEntry.text);
            }
        }

        private StatementEntryData GetLatestUnlockedStatement(string topicId)
        {
            StatementEntryData latestEntry = null;
            foreach (var entry in statementDatabase.GetStatementsByTopic(topicId))
            {
                if (entry != null && progressManager.IsStatementUnlocked(entry.statementId))
                {
                    latestEntry = entry;
                }
            }

            return latestEntry;
        }

        private static void AddIfNotBlank(List<string> lines, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                lines.Add(value.Trim());
            }
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
            foreach (var pair in entriesByNpcId)
            {
                pair.Value.SetDetailText(BuildDetailText(pair.Key));
            }

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
