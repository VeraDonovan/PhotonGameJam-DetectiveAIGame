using DetectiveGame.UI;
using System;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private KeyCode inventoryToggleKey = KeyCode.Tab;
        [SerializeField] private GameObject menuPanelRoot;
    

        [SerializeField] private GameObject inventoryRoot;
        [SerializeField] private InventoryPanelManager inventoryPanelManager;
        [SerializeField] private bool menuOpenOnStart = true;
        [SerializeField] private bool inventoryOpenOnStart;

        [SerializeField] private TransitionUI transitionUI; 

        // [SerializeField] private GameObject introducePanelRoot;
        public bool IsMenuOpen => menuPanelRoot != null && menuPanelRoot.activeSelf;
        public bool IsInventoryOpen => inventoryRoot.activeSelf;
        
        // public bool IsIntroduceOpen => introducePanelRoot != null && introducePanelRoot.activeSelf;
        public void Initialize()
        {
            ValidateConfiguration();

            SetMenuOpen(menuOpenOnStart);
            inventoryRoot.SetActive(inventoryOpenOnStart);

            if (inventoryOpenOnStart)
            {
                inventoryPanelManager.RefreshActiveTab();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(inventoryToggleKey))
            {
                SetInventoryOpen(!IsInventoryOpen);
            }
        }

        public void SetInventoryOpen(bool isOpen)
        {
            inventoryRoot.SetActive(isOpen);

            if (isOpen)
            {
                inventoryPanelManager.RefreshActiveTab();
            }
        }

        public void SetMenuOpen(bool isOpen)
        {
            if (menuPanelRoot != null)
            {
                menuPanelRoot.SetActive(isOpen);
            }
        }

         public void OnStartGameButton()
        {
        // 关闭菜单面板
            SetMenuOpen(false);

        // 打开过渡面板并开始动画
            transitionUI.gameObject.SetActive(true);
            transitionUI.StartTransition();
            StartGame();
           
        }
        // public void SetIntroduceOpen(bool isOpen)
        // {   
        //     Debug.Log("SetIntroduceOpen: " + isOpen);
        //     if (introducePanelRoot != null)
        //     {
        // introducePanelRoot.SetActive(isOpen);
        //     }
        // }

        // public void CloseIntroduce()
        // {   
        //     Debug.Log("CloseIntroduce called");
        //     if (introducePanelRoot != null)
        //     {
        //     introducePanelRoot.SetActive(false);
        //     }
        // }

        public bool StartGame()
        {   
            Debug.Log("开始游戏界面转换");
            var appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameStateManager.TryStartGame())
            {
                return false;
            }

            SetMenuOpen(false);
            return true;
        }

        public bool EnterInterrogationPhase()
        {
            var appRoot = AppRoot.Instance;
            return appRoot != null && appRoot.GameStateManager.TryBeginInterrogation();
        }

        public bool ConfirmSuspectForFinalPhase(string suspectId)
        {
            var appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                return false;
            }

            appRoot.ProgressManager.SubmitAccusation(suspectId);
            return appRoot.GameStateManager.TryOpenAccusation();
        }

        public bool EnterResultPhase()
        {
            var appRoot = AppRoot.Instance;
            return appRoot != null && appRoot.GameStateManager.TryShowResult();
        }

        private void ValidateConfiguration()
        {
            if (inventoryRoot == null)
            {
                throw new InvalidOperationException("UIManager requires inventoryRoot to be assigned.");
            }

            if (inventoryPanelManager == null)
            {
                throw new InvalidOperationException("UIManager requires inventoryPanelManager to be assigned.");
            }
        }
    }
}
