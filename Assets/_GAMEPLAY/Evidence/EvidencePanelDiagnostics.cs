using UnityEngine;

public class EvidencePanelDiagnostics : MonoBehaviour
{
    void Start()
    {
        var managers = FindObjectsOfType<DetectiveGame.UI.EvidencePanelManager>(true);
        if (managers.Length == 0)
        {
            Debug.LogWarning("[Diagnostics] 场景里没有找到任何 EvidencePanelManager 实例！");
        }
        else
        {
            Debug.Log($"[Diagnostics] 找到 {managers.Length} 个 EvidencePanelManager 实例：");
            foreach (var mgr in managers)
            {
                Debug.Log($" - GameObject={mgr.gameObject.name}, ActiveSelf={mgr.gameObject.activeSelf}, Enabled={mgr.enabled}");
            }
        }
    }
}
