using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject chatNPCPanel;
    public GameObject introducePanel;
    public GameObject savePanel;
    public GameObject arrestPanel;
    public GameObject endPanel;
    public GameObject underPanel;

    // 字典：名字 -> Panel
    private Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 注册所有 Panel 到字典
        panels.Add("Start", startPanel);
        panels.Add("ChatNPC", chatNPCPanel);
        panels.Add("Introduce", introducePanel);
        panels.Add("Save", savePanel);
        panels.Add("Arrest", arrestPanel);
        panels.Add("End", endPanel);
        panels.Add("Under", underPanel);
    }

    /// <summary>
    /// 打开指定名字的 Panel（先关闭所有，再打开目标）
    /// </summary>
    public void OpenPanel(string panelName)
    {
        CloseAllPanels();
        if (panels.ContainsKey(panelName))
        {
            panels[panelName].SetActive(true);
        }
        else
        {
            Debug.LogWarning("没有找到名字为 " + panelName + " 的 Panel！");
        }
    }

    /// <summary>
    /// 关闭所有 Panel
    /// </summary>
    public void CloseAllPanels()
    {
        foreach (var panel in panels.Values)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 单独关闭某个 Panel
    /// </summary>
    public void ClosePanel(string panelName)
    {
        if (panels.ContainsKey(panelName))
        {
            panels[panelName].SetActive(false);
        }
    }

    /// <summary>
    /// 切换某个 Panel（如果开着就关掉，如果关着就打开）
    /// </summary>
    public void TogglePanel(string panelName)
    {
        if (panels.ContainsKey(panelName))
        {
            bool isActive = panels[panelName].activeSelf;
            panels[panelName].SetActive(!isActive);
        }
    }

     // === 桥接方法：给按钮调用 ===
    public void OpenStartPanel()     { OpenPanel("Start"); }
    public void OpenChatNPCPanel()   { OpenPanel("ChatNPC"); }
    public void OpenIntroducePanel() { OpenPanel("Introduce"); }
    public void OpenSavePanel()      { OpenPanel("Save"); }
    public void OpenArrestPanel()    { OpenPanel("Arrest"); }
    public void OpenEndPanel()       { OpenPanel("End"); }
    public void OpenUnderPanel()     { OpenPanel("Under"); }
}
