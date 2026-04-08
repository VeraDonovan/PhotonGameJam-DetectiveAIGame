using DetectiveGame.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitchManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private float _defaultDelayTime = 0.5f;
    [SerializeField] private string _loadingSceneName = "LoadingScene";
    [SerializeField] private bool _useLoadingScene = false;


    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;


    private EventManager _eventManager;
    private bool _isSwitching = false;
    private string _currentSceneName;
    private GameSaveData _currentSaveData;


    private void Awake()
    {
        // 保持管理器持久化
        DontDestroyOnLoad(gameObject);


        // 获取事件管理器
        _eventManager = GetComponent<EventManager>();
        if (_eventManager == null)
        {
            Debug.LogError("EventManager not found! SceneSwitchManager requires EventManager component.");
            return;
        }


        _currentSceneName = SceneManager.GetActiveScene().name;
        _currentSaveData = new GameSaveData();
        _currentSaveData.CurrentSceneName = _currentSceneName;

        // 订阅场景切换请求
        _eventManager.Subscribe<SceneSwitchEvent>(OnSceneSwitchRequest);

    }


    private void OnDestroy()
    {
        if (_eventManager != null)
        {
            _eventManager.Unsubscribe<SceneSwitchEvent>(OnSceneSwitchRequest);
        }
    }


    private void OnSceneSwitchRequest(SceneSwitchEvent evt)
    {
        if (_isSwitching)
        {
            if (_showDebugLogs)
                Debug.LogWarning($"Scene switch in progress, ignoring request to {evt.ToSceneName}");
            return;
        }


        if (_showDebugLogs)
            Debug.Log($"Starting scene switch from {evt.FromSceneName} to {evt.ToSceneName}");

        StartCoroutine(SwitchSceneCoroutine(evt));
    }


    private IEnumerator SwitchSceneCoroutine(SceneSwitchEvent evt)
    {
        yield return evt;
    }
}
