using System.Collections.Generic;
using System.Text;
using DetectiveGame.Core;
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
    private bool _waiting;
    private Vector2 _scroll;

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
        if (info.NpcId == _currentNpc) _scroll.y = float.MaxValue; // 当前NPC才滚到底
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) _open = !_open;
    }

    private void OnGUI()
    {
        if (!_open)
        {
            if (GUI.Button(new Rect(10, 10, 110, 28), "测试模式 (F1)")) _open = true;
            return;
        }

        float w = Mathf.Min(720, Screen.width - 20);
        float h = Mathf.Min(640, Screen.height - 20);
        var panelRect = new Rect(10, 10, w, h);
        GUI.DrawTexture(panelRect, Bg()); // 实心不透明背景，避免游戏画面透出来看不清
        GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));

        // 顶部
        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>对话测试沙盒</b>（策划用 · 跑的是游戏真代码）", RichLabel());
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭", GUILayout.Width(60))) _open = false;
        GUILayout.EndHorizontal();

        var app = AppRoot.Instance;
        if (app == null || app.DatabaseManager == null || app.DatabaseManager.NpcDatabase == null)
        {
            GUILayout.Space(8);
            GUILayout.Label("游戏数据还没加载（AppRoot 未就绪）。请先进入有 AppRoot 的场景。");
            GUILayout.EndArea();
            return;
        }

        // 选嫌疑人
        GUILayout.Space(4);
        GUILayout.Label("选嫌疑人：");
        GUILayout.BeginHorizontal();
        foreach (var kv in app.DatabaseManager.NpcDatabase.NpcById)
        {
            string id = kv.Key;
            string name = kv.Value != null && !string.IsNullOrEmpty(kv.Value.displayName) ? kv.Value.displayName : id;
            bool isCur = id == _currentNpc;
            var style = isCur ? SelectedButton() : GUI.skin.button;
            if (GUILayout.Button(name, style))
            {
                if (id != _currentNpc) _scroll = Vector2.zero;
                _currentNpc = id;
                app.NpcRuntimeManager?.RegisterNpc(id);
                app.ProgressManager?.RegisterSuspect(id);
            }
        }
        GUILayout.EndHorizontal();

        // 阶段
        GUILayout.BeginHorizontal();
        GUILayout.Label("阶段：", GUILayout.Width(40));
        var ph = CurrentPhase();
        if (GUILayout.Button(ph == GamePhase.Exploration ? "● 探索" : "探索", GUILayout.Width(80))) GoExploration();
        if (GUILayout.Button(ph == GamePhase.Interrogation ? "● 审讯" : "审讯", GUILayout.Width(80))) GoInterrogation();
        GUILayout.Space(10);
        if (GUILayout.Button("▶ 让TA先开口", GUILayout.Width(110))) TriggerOpening();
        GUILayout.FlexibleSpace();
        GUILayout.Label("出示证据(可空，如 A-4)：", GUILayout.Width(150));
        _presentedEvidence = GUILayout.TextField(_presentedEvidence ?? "", GUILayout.Width(80));
        GUILayout.EndHorizontal();

        // 对话历史
        GUILayout.Space(4);
        GUILayout.Label("<b>对话历史</b>", RichLabel());
        _scroll = GUILayout.BeginScrollView(_scroll, GUI.skin.box, GUILayout.ExpandHeight(true));
        var hist = HistoryOf(_currentNpc);
        for (int i = 0; i < hist.Count; i++)
        {
            var t = hist[i];
            string fkey = _currentNpc + ":" + i;
            bool flagged = _flagged.Contains(fkey);

            GUILayout.BeginHorizontal();
            GUILayout.Label((flagged ? "⚑ " : "") + "你: " + t.PlayerText, RichLabel());
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(flagged ? "取消标记" : "⚑标记", GUILayout.Width(70)))
            {
                if (flagged) _flagged.Remove(fkey); else _flagged.Add(fkey);
            }
            GUILayout.EndHorizontal();

            string npcName = NpcName(app, t.NpcId);
            GUILayout.Label("<b>" + npcName + ": " + t.NpcText + "</b>", RichLabel());
            GUILayout.Label("<color=#444444><size=11>" + TurnStateLine(t) + "</size></color>", RichLabel());
            GUILayout.Space(6);
        }
        GUILayout.EndScrollView();

        // 输入
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("sandboxInput");
        _input = GUILayout.TextField(_input ?? "", GUILayout.ExpandWidth(true));
        bool enter = Event.current.type == EventType.KeyDown &&
                     (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
        bool send = GUILayout.Button("发送", GUILayout.Width(70));
        if ((send || (enter && GUI.GetNameOfFocusedControl() == "sandboxInput")))
        {
            Send();
            if (enter) Event.current.Use();
        }
        GUILayout.EndHorizontal();

        // 底部
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 复制对话+状态")) CopyTranscript(app);
        if (GUILayout.Button("清空显示", GUILayout.Width(90))) { HistoryOf(_currentNpc).Clear(); }
        GUILayout.FlexibleSpace();
        GUILayout.Label("F1 开关 · 当前: " + (string.IsNullOrEmpty(_currentNpc) ? "未选" : NpcName(app, _currentNpc)) + (_waiting ? " · 回复中…" : ""));
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void Send()
    {
        if (string.IsNullOrWhiteSpace(_input)) return;
        if (string.IsNullOrEmpty(_currentNpc)) return;
        var dm = DialogueManager.Instance;
        if (dm == null) return;

        var app = AppRoot.Instance;
        app?.NpcRuntimeManager?.RegisterNpc(_currentNpc);
        app?.ProgressManager?.RegisterSuspect(_currentNpc);

        string evidence = string.IsNullOrWhiteSpace(_presentedEvidence) ? string.Empty : _presentedEvidence.Trim();
        _waiting = true;
        dm.SubmitAiDialogueTurn(_currentNpc, CurrentPhase(), _input.Trim(), evidence);
        _input = "";
    }

    private void TriggerOpening()
    {
        if (string.IsNullOrEmpty(_currentNpc)) return;
        var dm = DialogueManager.Instance;
        if (dm == null) return;
        var app = AppRoot.Instance;
        app?.NpcRuntimeManager?.RegisterNpc(_currentNpc);
        app?.ProgressManager?.RegisterSuspect(_currentNpc);
        _waiting = true;
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
            sb.AppendLine("   [" + TurnStateLine(t) + "]");
        }
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[TestSandbox] 已复制对话到剪贴板。");
    }

    private static string TurnStateLine(DialogueTurnDebugInfo t)
    {
        var sb = new StringBuilder();
        sb.Append("命中:").Append(string.IsNullOrEmpty(t.MatchedTopicId) ? "-" : t.MatchedTopicId);
        sb.Append(" | 类型:").Append(string.IsNullOrEmpty(t.ResolutionType) ? "-" : t.ResolutionType);
        sb.Append(" | 压力:").Append(t.Pressure).Append(" 烦躁:").Append(t.Annoyance);
        if (t.UnlockedFactIds.Count > 0) sb.Append(" | 解锁fact:").Append(string.Join(",", t.UnlockedFactIds));
        if (t.UnlockedLayerIds.Count > 0) sb.Append(" | 解锁层:").Append(string.Join(",", t.UnlockedLayerIds));
        if (!t.Accepted) sb.Append(" | 驳回:").Append(string.IsNullOrEmpty(t.RejectReason) ? "yes" : t.RejectReason);
        return sb.ToString();
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

    private GUIStyle _rich;
    private GUIStyle RichLabel()
    {
        if (_rich == null) _rich = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        return _rich;
    }

    private GUIStyle _sel;
    private GUIStyle SelectedButton()
    {
        if (_sel == null)
        {
            _sel = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            _sel.normal.textColor = new Color(0.75f, 0.1f, 0.1f); // 深红，浅底上醒目
        }
        return _sel;
    }
}
