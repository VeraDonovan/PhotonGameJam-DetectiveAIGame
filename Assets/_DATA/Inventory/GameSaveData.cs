using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public string CurrentSceneName;
    public DateTime SaveTime;
    public Dictionary<string, object> SceneSpecificData = new Dictionary<string, object>();
    public Dictionary<string, object> GlobalGameData = new Dictionary<string, object>();


    // 侦探游戏特定数据
    public List<string> CollectedClues = new List<string>();
    public Dictionary<string, bool> CompletedDialogues = new Dictionary<string, bool>();
    public Dictionary<string, int> PlayerInventory = new Dictionary<string, int>();
    public Dictionary<string, string> NPCStates = new Dictionary<string, string>();
    public Dictionary<string, bool> CaseProgress = new Dictionary<string, bool>();



    // 添加或获取场景特定数据
    public void SetSceneData(string key, object value)
    {
        if (SceneSpecificData.ContainsKey(key))
            SceneSpecificData[key] = value;
        else
            SceneSpecificData.Add(key, value);
    }


    public T GetSceneData<T>(string key, T defaultValue = default)
    {
        if (SceneSpecificData.TryGetValue(key, out var value))
            return (T)value;
        return defaultValue;
    }


    // 添加或获取全局数据
    public void SetGlobalData(string key, object value)
    {
        if (GlobalGameData.ContainsKey(key))
            GlobalGameData[key] = value;
        else
            GlobalGameData.Add(key, value);
    }


    public T GetGlobalData<T>(string key, T defaultValue = default)
    {
        if (GlobalGameData.TryGetValue(key, out var value))
            return (T)value;
        return defaultValue;
    }


    // 线索管理
    public void AddClue(string clueId)
    {
        if (!CollectedClues.Contains(clueId))
            CollectedClues.Add(clueId);
    }


    public bool HasClue(string clueId) => CollectedClues.Contains(clueId);


    // 对话管理
    public void CompleteDialogue(string dialogueId)
    {
        if (!CompletedDialogues.ContainsKey(dialogueId))
            CompletedDialogues[dialogueId] = true;
    }


    public bool IsDialogueCompleted(string dialogueId) =>
     CompletedDialogues.TryGetValue(dialogueId, out var completed) && completed;


    // 物品管理
    public void AddItem(string itemId, int count = 1)
    {
        if (PlayerInventory.ContainsKey(itemId))
            PlayerInventory[itemId] += count;
        else
            PlayerInventory[itemId] = count;
    }


    public bool RemoveItem(string itemId, int count = 1)
    {
        if (!PlayerInventory.TryGetValue(itemId, out var currentCount) || currentCount < count)
            return false;

        PlayerInventory[itemId] = currentCount - count;
        if (PlayerInventory[itemId] <= 0)
            PlayerInventory.Remove(itemId);

        return true;
    }
    public int GetItemCount(string itemId) =>
        PlayerInventory.TryGetValue(itemId, out var count) ? count : 0;


    // 案件进度
    public void SetCaseProgress(string caseId, bool completed)
    {
        if (CaseProgress.ContainsKey(caseId))
            CaseProgress[caseId] = completed;
        else
            CaseProgress.Add(caseId, completed);



    }

    public bool IsCaseCompleted(string caseId) =>
           CaseProgress.TryGetValue(caseId, out var completed) && completed;


}
