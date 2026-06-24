using DetectiveGame.Core;              // 👉 添加：引用 UIManager 所在命名空间
using DetectiveGame.Gameplay.Npc;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRadius = 2f;

    private EventManager eventManager;
    private bool isInputBlocked;

    // 👉 修改：明确类型为 DetectiveGame.Core.UIManager
    private DetectiveGame.Core.UIManager uiManager;

    [SerializeField] private Animator animator;

    private float lastX = 0;
    private float lastY = 1;

    private void Start()
    {
        var appRoot = AppRoot.Instance;
        if (appRoot == null)
        {
            return;
        }

        eventManager = appRoot.EventManager;

        // 👉 初始化 UIManager
        uiManager = appRoot.UIManager;

        isInputBlocked = uiManager != null && uiManager.IsPlayerInputBlocked;
        eventManager?.Subscribe<UiBlockStateChangedEvent>(HandleUiBlockStateChanged);
    }

    private void OnDestroy()
    {
        eventManager?.Unsubscribe<UiBlockStateChangedEvent>(HandleUiBlockStateChanged);
    }

    private void Update()
    {
        if (isInputBlocked)
        {
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = new Vector3(h, v, 0f);
        transform.Translate(move * speed * Time.deltaTime);
        bool isMoving = move.magnitude > 0.01f;
        if (isMoving)
        {
            float absH = Mathf.Abs(h);
            float absV = Mathf.Abs(v);
            if (absH > absV)
            {
                lastX = Mathf.Sign(h);
                lastY = 0f;
            }
            else if (absV > absH)
            {
                lastX = 0f;
                lastY = Mathf.Sign(v);
            }
        }

        if (animator != null)
        {
            float animY = -lastY;
            animator.SetFloat("movex", isMoving ? lastX * 2f : lastX);
            animator.SetFloat("movey", isMoving ? animY * 2f : animY);
        }

        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
        GameplayNpc closestNpc = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            GameplayNpc npc = hit.GetComponentInParent<GameplayNpc>();
            if (npc == null) continue;

            float distance = Vector2.Distance(transform.position, npc.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNpc = npc;
            }
        }

        if (closestNpc != null)
        {
            // 👉 添加：互动时关闭 underPanel
            if (uiManager != null && uiManager.UnderPanel != null)
            {
                uiManager.UnderPanel.SetActive(false);
                UnityEngine.Debug.Log("[PlayerController] 与 NPC 互动，关闭 UnderPanel");
            }

            closestNpc.Interact();
        }
    }

    private void HandleUiBlockStateChanged(UiBlockStateChangedEvent eventData)
    {
        isInputBlocked = eventData.IsBlocked;
    }
}
