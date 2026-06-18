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
        private sealed class SuspectDetailCache
        {
            public string ProfileText;
            public readonly List<CachedStatementLine> VisibleStatements = new List<CachedStatementLine>();
        }

        private sealed class CachedStatementLine
        {
            public string StatementId;
            public string Text;
        }

        [Header("Icon View")]
        [SerializeField] private Sprite defaultSuspectIcon;
        [SerializeField] private Transform iconContentRoot;
        [SerializeField] private SuspectIconEntry iconEntryPrefab;

        [Header("Detail View")]
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private string defaultDetailText = string.Empty;

        private readonly Dictionary<string, SuspectIconEntry> entriesByNpcId = new Dictionary<string, SuspectIconEntry>();
        private readonly Dictionary<string, SuspectDetailCache> detailCacheByNpcId = new Dictionary<string, SuspectDetailCache>();

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
            RebuildDetailCaches();
            EnsureSuspectEntries(npcDatabase.NpcById.Keys);
            RefreshAllEntryDetails();
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

        var entry = Instantiate(iconEntryPrefab, iconContentRoot);

        // 👉 新增：根据 npcId 去 Resources 文件夹加载图片
        Debug.Log($"尝试加载 NPC 图标: {npcId}");
        Sprite npcIcon = Resources.Load<Sprite>("NpcIcons/" + npcId);
        if (npcIcon == null)
        {
            npcIcon = defaultSuspectIcon; // 如果找不到，就用默认图
        }

        entry.Initialize(
            npc.npcId,
            npc.displayName,
            BuildDetailText(npc.npcId),
            npcIcon,
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
            if (string.IsNullOrWhiteSpace(eventData.TopicId) ||
                !statementDatabase.TryGetTopic(eventData.TopicId, out var topic) ||
                topic == null ||
                !topic.suspectDetailVisible)
            {
                return;
            }

            if (!statementDatabase.TryGetStatement(eventData.StatementId, out var statement) || statement == null)
            {
                return;
            }

            if (!detailCacheByNpcId.TryGetValue(topic.npcId, out var cache))
            {
                cache = CreateDetailCache(topic.npcId);
                detailCacheByNpcId[topic.npcId] = cache;
            }

            ApplyUnlockedStatement(cache, statement);

            if (entriesByNpcId.TryGetValue(topic.npcId, out var entry))
            {
                entry.SetDetailText(BuildDetailText(topic.npcId));
            }

            if (selectedEntry != null && string.Equals(selectedEntry.NpcId, topic.npcId, StringComparison.Ordinal))
            {
                SetDetailText(BuildDetailText(topic.npcId));
            }
        }

        private string BuildDetailText(string npcId)
        {
            if (!detailCacheByNpcId.TryGetValue(npcId, out var cache))
            {
                return string.Empty;
            }

            var lines = new List<string>();
            AddIfNotBlank(lines, cache.ProfileText);

            for (var i = 0; i < cache.VisibleStatements.Count; i++)
            {
                AddIfNotBlank(lines, cache.VisibleStatements[i].Text);
            }

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

        private void RebuildDetailCaches()
        {
            detailCacheByNpcId.Clear();

            foreach (var npc in npcDatabase.NpcById.Values)
            {
                if (npc == null ||
                    !string.Equals(npc.roleType, "suspect", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(npc.npcId))
                {
                    continue;
                }

                detailCacheByNpcId[npc.npcId] = CreateDetailCache(npc.npcId);
            }
        }

        private SuspectDetailCache CreateDetailCache(string npcId)
        {
            var cache = new SuspectDetailCache();

            if (npcDatabase.TryGetNpc(npcId, out var npc) && npc != null)
            {
                cache.ProfileText = npc.profileText;
            }

            foreach (var topic in statementDatabase.GetTopicsByNpc(npcId))
            {
                if (topic == null || !topic.suspectDetailVisible)
                {
                    continue;
                }

                if (!progressManager.LatestStatementIdByTopic.TryGetValue(topic.topicId, out var statementId) ||
                    string.IsNullOrWhiteSpace(statementId) ||
                    !statementDatabase.TryGetStatement(statementId, out var statement) ||
                    statement == null)
                {
                    continue;
                }

                ApplyUnlockedStatement(cache, statement);
            }

            return cache;
        }

        private void ApplyUnlockedStatement(SuspectDetailCache cache, StatementEntryData statement)
        {
            if (cache == null || statement == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(statement.replacesStatementId))
            {
                cache.VisibleStatements.RemoveAll(
                    line => string.Equals(line.StatementId, statement.replacesStatementId, StringComparison.Ordinal));
            }

            if (string.IsNullOrWhiteSpace(statement.statementId) || string.IsNullOrWhiteSpace(statement.text))
            {
                return;
            }

            cache.VisibleStatements.RemoveAll(
                line => string.Equals(line.StatementId, statement.statementId, StringComparison.Ordinal));

            cache.VisibleStatements.Add(new CachedStatementLine
            {
                StatementId = statement.statementId,
                Text = statement.text.Trim()
            });
        }

        private void RefreshAllEntryDetails()
        {
            foreach (var pair in entriesByNpcId)
            {
                pair.Value.SetDetailText(BuildDetailText(pair.Key));
            }
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
