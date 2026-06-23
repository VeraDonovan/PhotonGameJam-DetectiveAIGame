using System.Collections.Generic;
using UnityEngine;
using DetectiveGame.Core;

public sealed class ChoiceManager : MonoBehaviour
{
    [SerializeField] private ChoiceEntry choicePrefab;   // Prefab，里面要有 Image 和 Button
    [SerializeField] private Transform contentRoot;      // 容器
    [SerializeField] private int maxSelections = 2;      // 最多选两个
    [SerializeField] private Sprite defaultSuspectIcon;  // 默认图标

    private int currentSelections = 0;
    private NpcDatabase npcDatabase;

    private void Awake()
    {   Debug.Log("ChoiceManager Awake: 初始化 NPC 数据库...");
        npcDatabase = AppRoot.Instance.DatabaseManager.NpcDatabase;
        InitializeSuspectChoices();
        Debug.Log($"contentRoot 是否为空: {contentRoot == null}");
        Debug.Log($"NpcDatabase 中 NPC 数量: {npcDatabase.NpcById.Count}");

    }

    public void InitializeSuspectChoices()
    {
        Debug.Log("初始化嫌疑人选择界面...");
        // 清空旧的
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }
        currentSelections = 0;

        // 遍历嫌疑人
        foreach (var npc in npcDatabase.NpcById.Values)
        {
            if (npc == null || !string.Equals(npc.roleType, "suspect", System.StringComparison.OrdinalIgnoreCase))
                continue;
            Debug.Log($"初始化嫌疑人: {npc.displayName}");
            var entry = Instantiate(choicePrefab, contentRoot);

            // 根据 npcId 去 Resources 文件夹加载图标
            Sprite npcIcon = Resources.Load<Sprite>("NpcIcons/" + npc.npcId);
            if (npcIcon == null)
            {
                npcIcon = defaultSuspectIcon;
            }

            // 初始化 ChoiceEntry（只显示图标和名字）
            entry.Initialize(npc.npcId, npc.displayName, OnChoiceSelected, npcIcon);
        }
    }

    private void OnChoiceSelected(ChoiceEntry entry)
    {
        if (currentSelections >= maxSelections)
        {
            Debug.Log("已经选满，不能再选。");
            return;
        }

        entry.SetSelected(true);
        currentSelections++;

        Debug.Log($"选中了 {entry.ChoiceId}, 当前数量: {currentSelections}");

        if (currentSelections >= maxSelections)
        {
            ProceedToNextPhase();
        }
    }

    private void ProceedToNextPhase()
    {
        Debug.Log("进入下一个游戏阶段！");
        // 在这里调用你的 GameStateManager 或 UIManager
        AppRoot.Instance.GameStateManager.TryBeginInterrogation();
    }
}
