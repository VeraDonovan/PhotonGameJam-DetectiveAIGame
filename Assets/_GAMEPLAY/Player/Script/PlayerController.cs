using System.Diagnostics;
using DetectiveGame.Core;
using DetectiveGame.Gameplay.Npc;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRadius = 2f;

    private EventManager eventManager;
    private bool isInputBlocked;

    // 在 Inspector 里拖子对象的 Animator 进来
    [SerializeField] private Animator animator;

    private float lastX = 0;
    private float lastY = -1;

    private void Start()
    {
        var appRoot = AppRoot.Instance;
        if (appRoot == null)
        {
            return;
        }

        eventManager = appRoot.EventManager;
        isInputBlocked = appRoot.UIManager != null && appRoot.UIManager.IsPlayerInputBlocked;
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
            lastX = h;
            lastY = v;
        }
        // 更新 Animator 参数
        if (animator != null)
        {
            // animator.SetFloat("Speed", move.magnitude);
            animator.SetFloat("movex", isMoving? lastX*2:lastX*1);
            animator.SetFloat("movey", isMoving? lastY*2:lastY*1);
            // animator.SetBool("is_walking", isMoving);
            // UnityEngine.Debug.Log($"h: {animator.GetFloat("movex")}, v: {animator.GetFloat("movey") }, lastX: {lastX}, lastY: {lastY}, isMoving: {isMoving}");
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
        // UnityEngine.Debug.Log($"检测到 {hits.Length} 个碰撞体在交互范围内");
        foreach (Collider2D hit in hits)
        {   
            UnityEngine.Debug.Log($"命中对象: {hit.gameObject.name}, Layer: {hit.gameObject.layer}");
            GameplayNpc npc = hit.GetComponentInParent<GameplayNpc>();
            if (npc == null)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, npc.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNpc = npc;
            }
        }

        closestNpc?.Interact();
    }

    private void HandleUiBlockStateChanged(UiBlockStateChangedEvent eventData)
    {
        isInputBlocked = eventData.IsBlocked;
    }
}
