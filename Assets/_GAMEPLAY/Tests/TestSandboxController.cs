using System;
using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;
using DetectiveGame.Gameplay.Dialogue;
using UnityEngine;

/// <summary>
/// 一轮对话的调试信息，由 DialogueManager 在每轮结算后通过 OnTurnDebug 事件抛出。
/// 仅供测试沙盒使用，不影响玩家游戏。
/// </summary>
public class DialogueTurnDebugInfo
{
    public string NpcId = "";
    public string PlayerText = "";
    public string NpcText = "";
    public string MatchedTopicId = "";
    public bool Accepted = true;
    public string RejectReason = "";
    public string ResolutionType = "";
    public int Pressure;
    public int Annoyance;
    public List<string> UnlockedFactIds = new List<string>();
    public List<string> UnlockedLayerIds = new List<string>();
    public List<string> UnlockedStatementIds = new List<string>();
}

/// <summary>
/// 策划测试页（独立工具，不在玩家游戏里）。
/// - 屏幕左上角「测试模式」按钮 或 F1 呼出/关闭
/// - 选嫌疑人 → 切阶段 → (可选)填证据 → 打字发送
/// - 完整对话历史 + 每句的内部状态（命中话题/解锁/压力/驳回）
/// - 一键复制整段对话+状态，方便反馈问题
/// 纯 IMGUI 实现，自动挂载，不依赖任何场景/Prefab。
/// </summary>
public class TestSandboxController : MonoBehaviour
{
    private static bool _booted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        if (_booted) return;
        _booted = true;
        var go = new GameObject("[TestSandbox]");
        go.AddComponent<TestSandboxController>();
        DontDestroyOnLoad(go);
    }

    private bool _open;
    private string _currentNpc = "";
    private string _input = "";
    private string _presentedEvidence = "";
    private string _status = "";
    private bool _waiting;
    private bool _showContextDetails;
    private bool _showEvidencePicker;
    private Vector2 _pageScroll;
    private Vector2 _scroll;
    private Vector2 _evidenceScroll;

    private readonly Dictionary<string, List<DialogueTurnDebugInfo>> _historyByNpc = new Dictionary<string, List<DialogueTurnDebugInfo>>();
    private static readonly List<DialogueTurnDebugInfo> _emptyHistory = new List<DialogueTurnDebugInfo>();
    private readonly HashSet<string> _flagged = new HashSet<string>();
    private List<DialogueTurnDebugInfo> HistoryOf(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return _emptyHistory;
        if (!_historyByNpc.TryGetValue(npcId, out var list))
        {
            list = new List<DialogueTurnDebugInfo>();
            _historyByNpc[npcId] = list;
        }
        return list;
    }

    private void OnEnable() { DialogueManager.OnTurnDebug += HandleTurn; }
    private void OnDisable() { DialogueManager.OnTurnDebug -= HandleTurn; }

    private void HandleTurn(DialogueTurnDebugInfo info)
    {
        HistoryOf(info.NpcId).Add(info);
        _waiting = false;
        _status = "收到 " + info.NpcId + " 的回复。";
        if (info.NpcId == _currentNpc) _scroll.y = float.MaxValue; // 当前NPC才滚到底
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) _open = !_open;
        if (_open && Input.GetKeyDown(KeyCode.F2)) TriggerOpening();
        if (_open && Input.GetKeyDown(KeyCode.F4)) CollectCompletionEvidence();
        if (_open && Input.GetKeyDown(KeyCode.F5)) ResetSelectedNpcSession();
    }

    private void OnGUI()
    {
        if (!_open)
        {
            if (GUI.Button(new Rect(10, 10, 110, 28), "测试模式 (F1)")) _open = true;
            return;
        }

        float w = Mathf.Max(320, Mathf.Min(1040, Screen.width - 20));
        float h = Mathf.Max(220, Screen.height - 20);
        var panelRect = new Rect(10, 10, w, h);
        bool compactLayout = panelRect.width < 560f;
        float contentWidth = Mathf.Max(260, panelRect.width - 48);
        GUI.DrawTexture(panelRect, Bg()); // 实心不透明背景，避免游戏画面透出来看不清
        Color oldContentColor = GUI.contentColor;
        Color oldBackgroundColor = GUI.backgroundColor;
        int oldLabelFontSize = GUI.skin.label.fontSize;
        int oldButtonFontSize = GUI.skin.button.fontSize;
        int oldTextFieldFontSize = GUI.skin.textField.fontSize;
        float oldButtonFixedHeight = GUI.skin.button.fixedHeight;
        float oldTextFieldFixedHeight = GUI.skin.textField.fixedHeight;
        RectOffset oldButtonPadding = GUI.skin.button.padding;
        RectOffset oldButtonMargin = GUI.skin.button.margin;
        RectOffset oldTextFieldPadding = GUI.skin.textField.padding;
        RectOffset oldTextFieldMargin = GUI.skin.textField.margin;
        GUI.contentColor = Color.black;
        GUI.backgroundColor = new Color(0.72f, 0.72f, 0.74f, 1f);
        GUI.skin.label.fontSize = 10;
        GUI.skin.button.fontSize = 10;
        GUI.skin.textField.fontSize = 10;
        GUI.skin.button.fixedHeight = 18;
        GUI.skin.textField.fixedHeight = 18;
        GUI.skin.button.padding = new RectOffset(4, 4, 1, 1);
        GUI.skin.button.margin = new RectOffset(2, 2, 1, 1);
        GUI.skin.textField.padding = new RectOffset(3, 3, 1, 1);
        GUI.skin.textField.margin = new RectOffset(2, 2, 1, 1);
        GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));
        _pageScroll = GUILayout.BeginScrollView(_pageScroll, false, true);

        // 顶部
        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>测试沙盒 V3</b>", RichLabel());
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭", GUILayout.Width(46))) _open = false;
        GUILayout.EndHorizontal();

        var app = AppRoot.Instance;
        if (app == null || app.DatabaseManager == null || app.DatabaseManager.NpcDatabase == null)
        {
            GUILayout.Space(8);
            GUILayout.Label("游戏数据还没加载（AppRoot 未就绪）。请先进入有 AppRoot 的场景。");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.contentColor = oldContentColor;
            GUI.backgroundColor = oldBackgroundColor;
            GUI.skin.label.fontSize = oldLabelFontSize;
            GUI.skin.button.fontSize = oldButtonFontSize;
            GUI.skin.textField.fontSize = oldTextFieldFontSize;
            GUI.skin.button.fixedHeight = oldButtonFixedHeight;
            GUI.skin.textField.fixedHeight = oldTextFieldFixedHeight;
            GUI.skin.button.padding = oldButtonPadding;
            GUI.skin.button.margin = oldButtonMargin;
            GUI.skin.textField.padding = oldTextFieldPadding;
            GUI.skin.textField.margin = oldTextFieldMargin;
            return;
        }

        // 选嫌疑人
        GUILayout.Space(2);
        GUILayout.Label("嫌疑人：");
        GUILayout.BeginHorizontal();
        int npcCount = Mathf.Max(1, app.DatabaseManager.NpcDatabase.NpcById.Count);
        float npcButtonWidth = Mathf.Max(54, (contentWidth - 12) / npcCount);
        foreach (var kv in app.DatabaseManager.NpcDatabase.NpcById)
        {
            string id = kv.Key;
            string name = kv.Value != null && !string.IsNullOrEmpty(kv.Value.displayName) ? kv.Value.displayName : id;
            bool isCur = id == _currentNpc;
            var style = isCur ? SelectedButton() : GUI.skin.button;
            if (GUILayout.Button(name, style, GUILayout.Width(npcButtonWidth)))
            {
                if (id != _currentNpc) _scroll = Vector2.zero;
                _currentNpc = id;
                app.NpcRuntimeManager?.RegisterNpc(id);
                app.ProgressManager?.RegisterSuspect(id);
            }
        }
        GUILayout.EndHorizontal();

        EnsureDefaultNpcSelected(app);

        GUILayout.Space(2);
        GUILayout.BeginHorizontal();
        GUILayout.Label("阶段", GUILayout.Width(28));
        var ph = CurrentPhase();
        if (GUILayout.Button(ph == GamePhase.Exploration ? "●探" : "探", GUILayout.Width(42))) GoExploration();
        if (GUILayout.Button(ph == GamePhase.Interrogation ? "●审" : "审", GUILayout.Width(42))) GoInterrogation();
        GUILayout.Space(4);
        if (GUILayout.Button("开场F2", GUILayout.Width(62))) TriggerOpening();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("证据", GUILayout.Width(28));
        _presentedEvidence = GUILayout.TextField(_presentedEvidence ?? "", GUILayout.Width(46));
        if (GUILayout.Button(_showEvidencePicker ? "收起" : "选择", GUILayout.Width(42))) _showEvidencePicker = !_showEvidencePicker;
        if (!compactLayout)
        {
            if (GUILayout.Button("收", GUILayout.Width(30))) CollectPresentedEvidence();
            if (GUILayout.Button("补齐F4", GUILayout.Width(58))) CollectCompletionEvidence();
        }
        GUILayout.EndHorizontal();
        if (compactLayout)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("收集", GUILayout.Width(52))) CollectPresentedEvidence();
            if (GUILayout.Button("补齐F4", GUILayout.Width(70))) CollectCompletionEvidence();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        DrawPresentedEvidenceInfo(app);
        if (_showEvidencePicker) DrawEvidencePicker(app, contentWidth);

        float historyHeight = Mathf.Clamp(panelRect.height - (_showEvidencePicker ? 455 : 350), 150, 320);
        DrawDialogueHistory(app, contentWidth, historyHeight);
        DrawConversationContext();

        // 输入
        GUILayout.BeginVertical(InfoBox());
        GUI.SetNextControlName("sandboxInput");
        _input = GUILayout.TextArea(
            _input ?? "",
            InputArea(),
            GUILayout.MinHeight(44),
            GUILayout.MaxHeight(72),
            GUILayout.ExpandWidth(true));
        bool sendShortcut = Event.current.type == EventType.KeyDown &&
                     Event.current.control &&
                     (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
        GUILayout.BeginHorizontal();
        GUILayout.Label("<size=9>Enter 换行，Ctrl+Enter 发送</size>", MutedLabel());
        GUILayout.FlexibleSpace();
        bool send = GUILayout.Button("发送", GUILayout.Width(54));
        GUILayout.EndHorizontal();
        if (send || (sendShortcut && GUI.GetNameOfFocusedControl() == "sandboxInput"))
        {
            Send();
            if (sendShortcut) Event.current.Use();
        }
        GUILayout.EndVertical();

        // 底部
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("复制", GUILayout.Width(44))) CopyTranscript(app);
        if (GUILayout.Button("清空", GUILayout.Width(44))) { HistoryOf(_currentNpc).Clear(); }
        if (GUILayout.Button("重置", GUILayout.Width(44))) ResetSelectedNpcSession();
        GUILayout.FlexibleSpace();
        GUILayout.Label("F1 开关 · 当前: " + (string.IsNullOrEmpty(_currentNpc) ? "未选" : NpcName(app, _currentNpc)) + (_waiting ? " · 回复中…" : ""));
        GUILayout.EndHorizontal();
        if (!string.IsNullOrWhiteSpace(_status))
        {
            GUILayout.Label("<color=#333333><size=11>" + _status + "</size></color>", RichLabel());
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.contentColor = oldContentColor;
        GUI.backgroundColor = oldBackgroundColor;
        GUI.skin.label.fontSize = oldLabelFontSize;
        GUI.skin.button.fontSize = oldButtonFontSize;
        GUI.skin.textField.fontSize = oldTextFieldFontSize;
        GUI.skin.button.fixedHeight = oldButtonFixedHeight;
        GUI.skin.textField.fixedHeight = oldTextFieldFixedHeight;
        GUI.skin.button.padding = oldButtonPadding;
        GUI.skin.button.margin = oldButtonMargin;
        GUI.skin.textField.padding = oldTextFieldPadding;
        GUI.skin.textField.margin = oldTextFieldMargin;
    }

    private void EnsureDefaultNpcSelected(AppRoot app)
    {
        if (!string.IsNullOrEmpty(_currentNpc) ||
            app == null ||
            app.DatabaseManager == null ||
            app.DatabaseManager.NpcDatabase == null)
        {
            return;
        }

        foreach (var kv in app.DatabaseManager.NpcDatabase.NpcById)
        {
            _currentNpc = kv.Key;
            app.NpcRuntimeManager?.RegisterNpc(_currentNpc);
            app.ProgressManager?.RegisterSuspect(_currentNpc);
            _status = "已默认选择 " + NpcName(app, _currentNpc) + "。";
            break;
        }
    }

    private void DrawDialogueHistory(AppRoot app, float width, float height)
    {
        GUILayout.Space(4);
        GUILayout.Label("<b>对话历史</b>", RichLabel());
        _scroll = GUILayout.BeginScrollView(
            _scroll,
            false,
            true,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            LightBox(),
            GUILayout.Width(width),
            GUILayout.Height(height));
        var hist = HistoryOf(_currentNpc);
        for (int i = 0; i < hist.Count; i++)
        {
            var t = hist[i];
            string fkey = _currentNpc + ":" + i;
            bool flagged = _flagged.Contains(fkey);

            GUILayout.BeginVertical(TurnBox());
            GUILayout.BeginHorizontal();
            GUILayout.Label((flagged ? "⚑ " : "") + "<b>玩家：</b>" + SafeText(t.PlayerText, "（开场白/无输入）"), RichLabel());
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(flagged ? "取消标记" : "标记", GUILayout.Width(70)))
            {
                if (flagged) _flagged.Remove(fkey); else _flagged.Add(fkey);
            }
            GUILayout.EndHorizontal();

            string npcName = NpcName(app, t.NpcId);
            GUILayout.Label("<b>" + npcName + "：</b>" + SafeText(t.NpcText, "（暂无 NPC 回复）"), RichLabel());
            DrawTurnUnlocks(app, t);
            GUILayout.Label("<size=11>" + TurnStateLine(app, t) + "</size>", MutedLabel());
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }
        GUILayout.EndScrollView();
    }

    private void DrawPresentedEvidenceInfo(AppRoot app)
    {
        string evidenceId = string.IsNullOrWhiteSpace(_presentedEvidence) ? string.Empty : _presentedEvidence.Trim();
        GUILayout.BeginVertical(InfoBox());
        if (string.IsNullOrEmpty(evidenceId))
        {
            GUILayout.Label("<b>当前出示证据：</b>无。需要用证据压 NPC 时，点“选证据”。", RichLabel());
        }
        else
        {
            GUILayout.Label("<b>当前出示证据：</b>" + DescribeEvidence(app, evidenceId), RichLabel());
        }
        GUILayout.EndVertical();
    }

    private void DrawEvidencePicker(AppRoot app, float width)
    {
        GUILayout.Label("<b>常用证据（点击后自动填入证据框）</b>", RichLabel());
        _evidenceScroll = GUILayout.BeginScrollView(
            _evidenceScroll,
            false,
            true,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            LightBox(),
            GUILayout.Height(92));
        int columns = Mathf.Max(1, Mathf.FloorToInt((width - 24) / 184f));
        int column = 0;
        GUILayout.BeginHorizontal();
        var evidenceIds = GetRuntimeEvidenceIds(app);
        foreach (var evidenceId in evidenceIds)
        {
            string buttonText = EvidenceButtonText(app, evidenceId);
            if (GUILayout.Button(buttonText, GUILayout.Width(176), GUILayout.Height(19)))
            {
                _presentedEvidence = evidenceId;
                _status = "已选择证据：" + DescribeEvidence(app, evidenceId);
            }

            column++;
            if (column % columns == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
    }

    private void DrawConversationContext()
    {
        var dm = DialogueManager.Instance;
        GUILayout.Space(4);
        GUILayout.BeginVertical(InfoBox());
        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>真实上下文 / 压缩状态</b>", RichLabel());
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(_showContextDetails ? "收起" : "展开", GUILayout.Width(55)))
        {
            _showContextDetails = !_showContextDetails;
        }
        GUILayout.EndHorizontal();

        if (dm == null || string.IsNullOrEmpty(_currentNpc) ||
            !dm.TryGetDebugConversationSession(_currentNpc, out DialogueConversationSession session) ||
            session == null)
        {
            GUILayout.Label("这个 NPC 还没有真实会话。先发送一句或点“让AI先开口”。", RichLabel());
            GUILayout.EndVertical();
            return;
        }

        int exchangeCount = session.Exchanges != null ? session.Exchanges.Count : 0;
        int recentCount = Mathf.Min(DialogueConversationConfig.RecentVerbatimExchangeCount, exchangeCount);
        GUILayout.Label("累计轮数: " + exchangeCount +
                        " | 已折叠进摘要: " + session.SummarizedExchangeCount +
                        " | 最近原文保留: " + recentCount +
                        " | 单次折叠批量: " + DialogueConversationConfig.TurnSummaryBatchSize,
            RichLabel());

        if (_showContextDetails)
        {
            GUILayout.Label("<size=11>快捷键：F2 开场白，F4 补齐关键证据，F5 重置当前 NPC。Pending 摘要会在下一轮开始时变成 Active。</size>", MutedLabel());
            GUILayout.Label("<b>Turn Active:</b> " + FormatSummary(session.ActiveTurnSummary), RichLabel());
            GUILayout.Label("<b>Turn Pending:</b> " + FormatSummary(session.PendingTurnSummary), RichLabel());
            GUILayout.Label("<b>Opening Active:</b> " + FormatSummary(session.ActiveOpeningSummary), RichLabel());
            GUILayout.Label("<b>Opening Pending:</b> " + FormatSummary(session.PendingOpeningSummary), RichLabel());
        }

        DrawCompleteEndingProgress(AppRoot.Instance);

        GUILayout.EndVertical();
    }

    private void ResetSelectedNpcSession()
    {
        if (string.IsNullOrEmpty(_currentNpc)) return;
        DialogueManager.Instance?.DebugClearConversationSession(_currentNpc);
        HistoryOf(_currentNpc).Clear();

        var toRemove = new List<string>();
        foreach (var key in _flagged)
        {
            if (key.StartsWith(_currentNpc + ":"))
            {
                toRemove.Add(key);
            }
        }

        foreach (var key in toRemove)
        {
            _flagged.Remove(key);
        }

        _waiting = false;
        _scroll = Vector2.zero;
        _status = "已重置 " + _currentNpc + " 的测试显示和真实会话。";
    }

    private void CollectPresentedEvidence()
    {
        string evidenceId = string.IsNullOrWhiteSpace(_presentedEvidence) ? string.Empty : _presentedEvidence.Trim();
        if (string.IsNullOrEmpty(evidenceId)) return;
        AppRoot.Instance?.ProgressManager?.AddEvidence(evidenceId);
        _status = "已收集证据：" + DescribeEvidence(AppRoot.Instance, evidenceId);
    }

    private void CollectCompletionEvidence()
    {
        var app = AppRoot.Instance;
        var progress = app != null ? app.ProgressManager : null;
        if (progress == null) return;

        var evidenceIds = GetRuntimeEvidenceIds(app);
        foreach (var evidenceId in evidenceIds)
        {
            progress.AddEvidence(evidenceId);
        }

        _status = "已补齐当前证据库中的全部证据（" + evidenceIds.Count + " 条）。";
    }

    private void Send()
    {
        if (string.IsNullOrWhiteSpace(_input)) return;
        EnsureDefaultNpcSelected(AppRoot.Instance);
        if (string.IsNullOrEmpty(_currentNpc))
        {
            _status = "请先选择一个 NPC。";
            return;
        }
        var dm = DialogueManager.Instance;
        if (dm == null)
        {
            _status = "DialogueManager 还没初始化。";
            return;
        }

        var app = AppRoot.Instance;
        app?.NpcRuntimeManager?.RegisterNpc(_currentNpc);
        app?.ProgressManager?.RegisterSuspect(_currentNpc);

        string evidence = string.IsNullOrWhiteSpace(_presentedEvidence) ? string.Empty : _presentedEvidence.Trim();
        if (!string.IsNullOrEmpty(evidence))
        {
            app?.ProgressManager?.AddEvidence(evidence);
        }
        _waiting = true;
        _status = "已发送给 " + NpcName(app, _currentNpc) +
                  (string.IsNullOrEmpty(evidence) ? "" : "，出示 " + EvidenceButtonText(app, evidence)) +
                  "，等待回复...";
        dm.SubmitAiDialogueTurn(_currentNpc, CurrentPhase(), _input.Trim(), evidence);
        _input = "";
    }

    private void TriggerOpening()
    {
        EnsureDefaultNpcSelected(AppRoot.Instance);
        if (string.IsNullOrEmpty(_currentNpc))
        {
            _status = "请先选择一个 NPC。";
            return;
        }
        var dm = DialogueManager.Instance;
        if (dm == null)
        {
            _status = "DialogueManager 还没初始化。";
            return;
        }
        var app = AppRoot.Instance;
        app?.NpcRuntimeManager?.RegisterNpc(_currentNpc);
        app?.ProgressManager?.RegisterSuspect(_currentNpc);
        _waiting = true;
        _status = "已触发 " + NpcName(app, _currentNpc) + " 的开场白，等待回复...";
        dm.RequestAiOpeningDialogue(_currentNpc, CurrentPhase());
    }

    private GamePhase CurrentPhase()
    {
        var gsm = AppRoot.Instance != null ? AppRoot.Instance.GameStateManager : null;
        return gsm != null ? gsm.CurrentPhase : GamePhase.Exploration;
    }

    private void GoExploration()
    {
        var gsm = AppRoot.Instance != null ? AppRoot.Instance.GameStateManager : null;
        if (gsm != null && gsm.CurrentPhase != GamePhase.Exploration)
            gsm.TrySetPhase(GamePhase.Exploration);
    }

    // 真实进审讯：注册嫌疑人 → 选为审讯对象 → 广播阶段事件（才会初始化审讯层、重置压力），与游戏一致
    private void GoInterrogation()
    {
        var app = AppRoot.Instance;
        if (app == null || app.GameStateManager == null || app.ProgressManager == null) return;
        if (!string.IsNullOrEmpty(_currentNpc))
        {
            app.NpcRuntimeManager?.RegisterNpc(_currentNpc);
            app.ProgressManager.RegisterSuspect(_currentNpc);
            app.ProgressManager.SelectSuspectForInterrogation(_currentNpc);
        }
        if (app.GameStateManager.CurrentPhase != GamePhase.Interrogation)
            app.GameStateManager.TrySetPhase(GamePhase.Interrogation);
    }

    private void CopyTranscript(AppRoot app)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【对话测试记录】NPC=" + NpcName(app, _currentNpc) + "  阶段=" + CurrentPhase());
        sb.AppendLine("--------------------------------");
        var hist = HistoryOf(_currentNpc);
        for (int i = 0; i < hist.Count; i++)
        {
            var t = hist[i];
            if (_flagged.Contains(_currentNpc + ":" + i)) sb.AppendLine(">>> 标记有问题 <<<");
            sb.AppendLine("你: " + t.PlayerText);
            sb.AppendLine(NpcName(app, t.NpcId) + ": " + t.NpcText);
            sb.AppendLine("   [" + TurnStateLine(app, t) + "]");
        }
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[TestSandbox] 已复制对话到剪贴板。");
    }

    private void DrawTurnUnlocks(AppRoot app, DialogueTurnDebugInfo t)
    {
        GUILayout.BeginVertical(UnlockBox());
        if (!t.Accepted)
        {
            GUILayout.Label("<b>本轮结果：</b>未通过裁判 / 被驳回：" + SafeText(t.RejectReason, "无具体原因"), RichLabel());
        }
        else if (t.UnlockedFactIds.Count == 0 && t.UnlockedLayerIds.Count == 0 && t.UnlockedStatementIds.Count == 0)
        {
            GUILayout.Label("<b>本轮解锁：</b>无。说明这句话只是普通回复，没有推进事实/审讯层。", RichLabel());
        }
        else
        {
            GUILayout.Label("<b>本轮解锁：</b>", RichLabel());
            foreach (var factId in t.UnlockedFactIds)
            {
                GUILayout.Label("fact: " + DescribeFact(app, factId), RichLabel());
            }
            foreach (var layerId in t.UnlockedLayerIds)
            {
                GUILayout.Label("审讯层: " + DescribeLayer(app, layerId), RichLabel());
            }
            foreach (var statementId in t.UnlockedStatementIds)
            {
                GUILayout.Label("陈述: " + statementId, RichLabel());
            }
        }
        GUILayout.EndVertical();
    }

    private void DrawCompleteEndingProgress(AppRoot app)
    {
        if (app == null || app.DatabaseManager == null || app.ProgressManager == null ||
            app.DatabaseManager.EndingDatabase == null ||
            !app.DatabaseManager.EndingDatabase.TryGetEnding("ending_4", out var ending) ||
            ending == null)
        {
            return;
        }

        GUILayout.Space(4);
        GUILayout.BeginVertical(UnlockBox());
        GUILayout.Label("<b>完整结局目标 / 期望看到：</b>" + ending.displayName, RichLabel());

        foreach (var factId in ending.requirements?.requiredFactIds ?? new List<string>())
        {
            GUILayout.Label(RequirementPrefix(app, factId) + " fact: " + DescribeFact(app, factId), RichLabel());
        }

        foreach (var layerId in ending.requirements?.requiredNpcLayerIds ?? new List<string>())
        {
            GUILayout.Label(RequirementPrefix(app, layerId) + " 审讯层: " + DescribeLayer(app, layerId), RichLabel());
        }

        GUILayout.EndVertical();
    }

    private static string RequirementPrefix(AppRoot app, string requirementId)
    {
        return IsRequirementUnlocked(app, requirementId) ? "[已达成]" : "[未达成]";
    }

    private static bool IsRequirementUnlocked(AppRoot app, string requirementId)
    {
        var progress = app != null ? app.ProgressManager : null;
        if (progress == null || string.IsNullOrWhiteSpace(requirementId))
        {
            return false;
        }

        return progress.IsEvidenceCollected(requirementId) ||
               progress.IsFactUnlocked(requirementId) ||
               progress.IsStatementUnlocked(requirementId) ||
               progress.IsInterrogationLayerUnlocked(requirementId) ||
               progress.IsProgressTokenUnlocked(requirementId);
    }

    private static string EvidenceButtonText(AppRoot app, string evidenceId)
    {
        var evidence = GetEvidence(app, evidenceId);
        string name = evidence != null && !string.IsNullOrWhiteSpace(evidence.displayName)
            ? evidence.displayName
            : "未知证据";
        return evidenceId + " " + name;
    }

    private static string DescribeEvidence(AppRoot app, string evidenceId)
    {
        var evidence = GetEvidence(app, evidenceId);
        if (evidence == null)
        {
            return evidenceId + "（未在证据库找到）";
        }

        string name = string.IsNullOrWhiteSpace(evidence.displayName) ? "未命名证据" : evidence.displayName;
        string summary = string.IsNullOrWhiteSpace(evidence.summary) ? "" : "：" + evidence.summary;
        return evidenceId + " " + name + summary;
    }

    private static EvidenceNodeData GetEvidence(AppRoot app, string evidenceId)
    {
        if (app != null &&
            app.DatabaseManager != null &&
            app.DatabaseManager.EvidenceDatabase != null &&
            app.DatabaseManager.EvidenceDatabase.TryGetEvidence(evidenceId, out var evidence))
        {
            return evidence;
        }

        return null;
    }

    private static List<string> GetRuntimeEvidenceIds(AppRoot app)
    {
        var ids = new List<string>();
        var evidenceDb = app != null && app.DatabaseManager != null
            ? app.DatabaseManager.EvidenceDatabase
            : null;
        if (evidenceDb == null || evidenceDb.EvidenceById == null)
        {
            return ids;
        }

        ids.AddRange(evidenceDb.EvidenceById.Keys);
        ids.Sort(CompareEvidenceIds);
        return ids;
    }

    private static int CompareEvidenceIds(string left, string right)
    {
        int leftTier = EvidenceTierRank(left);
        int rightTier = EvidenceTierRank(right);
        if (leftTier != rightTier) return leftTier.CompareTo(rightTier);

        int leftNumber = EvidenceNumber(left);
        int rightNumber = EvidenceNumber(right);
        if (leftNumber != rightNumber) return leftNumber.CompareTo(rightNumber);

        return string.Compare(left, right, StringComparison.Ordinal);
    }

    private static int EvidenceTierRank(string evidenceId)
    {
        if (string.IsNullOrWhiteSpace(evidenceId)) return 99;
        char tier = char.ToUpperInvariant(evidenceId.Trim()[0]);
        if (tier == 'A') return 0;
        if (tier == 'B') return 1;
        return 2;
    }

    private static int EvidenceNumber(string evidenceId)
    {
        if (string.IsNullOrWhiteSpace(evidenceId)) return int.MaxValue;
        int dash = evidenceId.IndexOf('-');
        if (dash < 0 || dash >= evidenceId.Length - 1) return int.MaxValue;
        return int.TryParse(evidenceId.Substring(dash + 1), out int value) ? value : int.MaxValue;
    }

    private static string DescribeFact(AppRoot app, string factId)
    {
        if (app != null &&
            app.DatabaseManager != null &&
            app.DatabaseManager.FactDatabase != null &&
            app.DatabaseManager.FactDatabase.TryGetFact(factId, out var fact) &&
            fact != null)
        {
            string name = string.IsNullOrWhiteSpace(fact.displayName) ? "未命名事实" : fact.displayName;
            return factId + " " + name;
        }

        return factId;
    }

    private static string DescribeLayer(AppRoot app, string layerId)
    {
        if (app != null &&
            app.DatabaseManager != null &&
            app.DatabaseManager.TruthDatabase != null &&
            app.DatabaseManager.TruthDatabase.TryGetInterrogationLayer(layerId, out var layer) &&
            layer != null)
        {
            string topic = string.IsNullOrWhiteSpace(layer.topic) ? "未命名审讯层" : layer.topic;
            return layerId + " " + topic;
        }

        return layerId;
    }

    private static string TurnStateLine(AppRoot app, DialogueTurnDebugInfo t)
    {
        var sb = new StringBuilder();
        sb.Append("命中话题:").Append(DescribeTopic(app, t.MatchedTopicId));
        sb.Append(" | 结果:").Append(DescribeResolution(t));
        sb.Append(" | 压力:").Append(t.Pressure).Append(" 烦躁:").Append(t.Annoyance);
        if (t.UnlockedFactIds.Count > 0) sb.Append(" | 解锁事实:").Append(string.Join(",", t.UnlockedFactIds));
        if (t.UnlockedLayerIds.Count > 0) sb.Append(" | 解锁审讯层:").Append(string.Join(",", t.UnlockedLayerIds));
        if (!t.Accepted) sb.Append(" | 驳回原因:").Append(DescribeRejectReason(t.RejectReason));
        return sb.ToString();
    }

    private static string DescribeTopic(AppRoot app, string topicId)
    {
        if (string.IsNullOrWhiteSpace(topicId))
        {
            return "无";
        }

        if (topicId == "opening" || topicId == "opening_beat")
        {
            return "开场白";
        }

        var database = app != null ? app.DatabaseManager : null;
        if (database != null &&
            database.StatementDatabase != null &&
            database.StatementDatabase.TryGetTopic(topicId, out var statementTopic) &&
            statementTopic != null &&
            !string.IsNullOrWhiteSpace(statementTopic.displayName))
        {
            return statementTopic.displayName + "（" + topicId + "）";
        }

        if (database != null &&
            database.DialogueBeatDatabase != null &&
            database.DialogueBeatDatabase.TryGetTopic(topicId, out var beatTopic) &&
            beatTopic != null &&
            !string.IsNullOrWhiteSpace(beatTopic.displayName))
        {
            return beatTopic.displayName + "（" + topicId + "）";
        }

        return topicId;
    }

    private static string DescribeResolution(DialogueTurnDebugInfo t)
    {
        if (t == null)
        {
            return "未知";
        }

        if (!t.Accepted)
        {
            return "被裁判驳回，不算推进";
        }

        switch (t.ResolutionType)
        {
            case "Opening":
                return "开场白";
            case "CompositeProgress":
                return "有推进：复合进展";
            case "InterrogationLayerUnlocked":
                return "关键推进：解锁审讯层";
            case "FactUnlocked":
                return "关键推进：解锁事实";
            case "StatementUnlocked":
                return "小推进：解锁陈述";
            case "TokenUnlocked":
                return "小推进：推进追问节点";
            case "PressureChanged":
                return "压力变化";
            case "Punished":
                return "惩罚/抗拒";
            case "NoProgress":
                return "没推进：普通回答";
            case "None":
            case "":
            case null:
                return "无";
            default:
                return t.ResolutionType;
        }
    }

    private static string DescribeRejectReason(string reason)
    {
        switch (reason)
        {
            case "used_statement_not_allowed":
                return "AI 提前说了当前不允许说的陈述";
            case "used_beat_not_allowed":
                return "AI 使用了当前不允许使用的剧情节点";
            case "used_reveal_not_allowed":
                return "AI 提前揭示了未解锁的真相";
            case "non_progress_fallback_used_statement":
                return "普通回答里夹带了陈述推进";
            case "non_progress_fallback_used_beat":
                return "普通回答里夹带了剧情节点";
            case "non_progress_fallback_used_reveal":
                return "普通回答里夹带了真相揭示";
            case "resolved_topic_repeated":
                return "重复追问已经解决的话题";
            case "":
            case null:
                return "未说明";
            default:
                return reason;
        }
    }

    private static string FormatSummary(string summary)
    {
        return string.IsNullOrWhiteSpace(summary) ? "<color=#777777>无</color>" : summary;
    }

    private static string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string NpcName(AppRoot app, string npcId)
    {
        if (app != null && app.DatabaseManager != null && app.DatabaseManager.NpcDatabase != null &&
            app.DatabaseManager.NpcDatabase.TryGetNpc(npcId, out var npc) && npc != null &&
            !string.IsNullOrEmpty(npc.displayName))
            return npc.displayName;
        return string.IsNullOrEmpty(npcId) ? "?" : npcId;
    }

    private Texture2D _bgTex;
    private Texture2D Bg()
    {
        if (_bgTex == null)
        {
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0.90f, 0.90f, 0.92f, 1f)); // 浅色实心底，配默认深色字最清晰
            _bgTex.Apply();
        }
        return _bgTex;
    }

    private Texture2D _lightTex;
    private Texture2D _turnTex;
    private Texture2D _infoTex;
    private Texture2D _unlockTex;

    private Texture2D SolidTex(ref Texture2D tex, Color color)
    {
        if (tex == null)
        {
            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
        }

        return tex;
    }

    private GUIStyle _rich;
    private GUIStyle RichLabel()
    {
        if (_rich == null)
        {
            _rich = new GUIStyle(GUI.skin.label) {
                richText = true,
                wordWrap = true,
                fontSize = 10,
                normal = { textColor = Color.black }
            };
        }
        return _rich;
    }

    private GUIStyle _muted;
    private GUIStyle MutedLabel()
    {
        if (_muted == null)
        {
            _muted = new GUIStyle(GUI.skin.label) {
                richText = true,
                wordWrap = true,
                fontSize = 9,
                normal = { textColor = new Color(0.18f, 0.18f, 0.18f, 1f) }
            };
        }
        return _muted;
    }

    private GUIStyle _inputArea;
    private GUIStyle InputArea()
    {
        if (_inputArea == null)
        {
            _inputArea = new GUIStyle(GUI.skin.textArea) {
                wordWrap = true,
                fontSize = 10,
                stretchHeight = true,
                normal = { textColor = Color.black },
                padding = new RectOffset(5, 5, 4, 4)
            };
        }
        return _inputArea;
    }

    private GUIStyle _lightBox;
    private GUIStyle LightBox()
    {
        if (_lightBox == null)
        {
            _lightBox = new GUIStyle(GUI.skin.box) {
                normal = { background = SolidTex(ref _lightTex, new Color(0.78f, 0.78f, 0.80f, 1f)), textColor = Color.black },
                padding = new RectOffset(6, 6, 6, 6)
            };
        }
        return _lightBox;
    }

    private GUIStyle _turnBox;
    private GUIStyle TurnBox()
    {
        if (_turnBox == null)
        {
            _turnBox = new GUIStyle(GUI.skin.box) {
                normal = { background = SolidTex(ref _turnTex, new Color(0.93f, 0.93f, 0.94f, 1f)), textColor = Color.black },
                padding = new RectOffset(8, 8, 6, 6)
            };
        }
        return _turnBox;
    }

    private GUIStyle _infoBox;
    private GUIStyle InfoBox()
    {
        if (_infoBox == null)
        {
            _infoBox = new GUIStyle(GUI.skin.box) {
                normal = { background = SolidTex(ref _infoTex, new Color(0.88f, 0.90f, 0.93f, 1f)), textColor = Color.black },
                padding = new RectOffset(6, 6, 5, 5)
            };
        }
        return _infoBox;
    }

    private GUIStyle _unlockBox;
    private GUIStyle UnlockBox()
    {
        if (_unlockBox == null)
        {
            _unlockBox = new GUIStyle(GUI.skin.box) {
                normal = { background = SolidTex(ref _unlockTex, new Color(0.96f, 0.91f, 0.70f, 1f)), textColor = Color.black },
                padding = new RectOffset(6, 6, 5, 5)
            };
        }
        return _unlockBox;
    }

    private GUIStyle _sel;
    private GUIStyle SelectedButton()
    {
        if (_sel == null)
        {
            _sel = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 10, fixedHeight = 18 };
            _sel.normal.textColor = new Color(0.75f, 0.1f, 0.1f); // 深红，浅底上醒目
        }
        return _sel;
    }
}
