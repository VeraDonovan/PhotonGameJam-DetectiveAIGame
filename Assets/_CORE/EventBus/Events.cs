using System;
using UnityEngine;


// 场景切换前保存数据事件
public class BeforeSceneSaveEvent
{
    public string FromSceneName;
    public string ToSceneName;
    public DateTime SaveTime;

}


// 数据保存事件
public class DataSaveEvent
{
    public string SceneName;
    public GameSaveData SaveData;
    public bool IsSuccess;
    public string ErrorMessage;

}



// 切换场景事件
public class SceneSwitchEvent
{
    public string FromSceneName;
    public string ToSceneName;
    public float DelayTime; // 延迟切换时间（秒）
    public bool ShouldSaveData = true; // 是否保存数据
}


// 场景切换后加载事件
public class AfterSceneLoadEvent
{
    public string SceneName;
    public GameSaveData LoadedData;
    public bool IsFirstEnter; // 是否首次进入该场景
}



// 场景切换进度事件
public class SceneSwitchProgressEvent
{
    public float Progress; // 0-1
    public string StatusMessage;
}



// 场景数据准备就绪事件
public class SceneDataReadyEvent
{
    public string SceneName;
    public GameSaveData SceneData;
}
