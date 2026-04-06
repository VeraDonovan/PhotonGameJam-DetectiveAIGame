using UnityEngine;

namespace DetectiveGame.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private EventManager eventManager;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private CaseRuntimeManager caseRuntimeManager;
        [SerializeField] private NpcRuntimeManager npcRuntimeManager;
        [SerializeField] private UIManager uiManager;

        public static AppRoot Instance { get; private set; }

        public EventManager EventManager => eventManager;
        public GameStateManager GameStateManager => gameStateManager;
        public CaseRuntimeManager CaseRuntimeManager => caseRuntimeManager;
        public NpcRuntimeManager NpcRuntimeManager => npcRuntimeManager;
        public UIManager UIManager => uiManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureCoreServices();
            InitializeCoreServices();

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void EnsureCoreServices()
        {
            eventManager = ResolveOrCreate(eventManager);
            gameStateManager = ResolveOrCreate(gameStateManager);
            caseRuntimeManager = ResolveOrCreate(caseRuntimeManager);
            npcRuntimeManager = ResolveOrCreate(npcRuntimeManager);
            uiManager = ResolveOrCreate(uiManager);
        }

        private void InitializeCoreServices()
        {
            eventManager.Initialize();
            gameStateManager.Initialize(eventManager);
            caseRuntimeManager.Initialize(eventManager);
            npcRuntimeManager.Initialize(eventManager);
            uiManager.Initialize();
        }

        private T ResolveOrCreate<T>(T current) where T : Component
        {
            if (current != null)
            {
                return current;
            }

            var existing = GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            return gameObject.AddComponent<T>();
        }
    }
}
