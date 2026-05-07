using System.Collections.Generic;
using UnityEngine;

public sealed class ChoiceManager : MonoBehaviour
{
    [SerializeField] private ChoiceEntry choicePrefab;   // 选项Prefab
    [SerializeField] private Transform contentRoot;      // 容器
    [SerializeField] private int maxSelections = 2;      // 最多选两个

    private int currentSelections = 0;

    public void InitializeChoices(List<(string id, string name)> choices)
    {
        // 清空旧的
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }
        currentSelections = 0;

        // 生成新的选项
        foreach (var data in choices)
        {
            var entry = Instantiate(choicePrefab, contentRoot);
            entry.Initialize(data.id, data.name, OnChoiceSelected);
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
        // AppRoot.Instance.GameStateManager.TryStartNextPhase();
    }
}
