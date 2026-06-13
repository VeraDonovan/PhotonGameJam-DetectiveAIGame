using UnityEngine;

public class playermovement : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Animator animator;

    private float lastX = 0;
    private float lastY = -1; // 默认面向下

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(moveX, moveY);
        rb.velocity = movement * speed;

        bool isWalking = movement.magnitude > 0;

        // 如果在走动，更新最后方向
        if (isWalking)
        {
            lastX = moveX;
            lastY = moveY;
        }

        // 根据是否走动来决定参数点落在 Idle 区域还是 Walk 区域
        animator.SetFloat("movex", (isWalking ? lastX * 2 : lastX * 1));
        animator.SetFloat("movey", (isWalking ? lastY * 2 : lastY * 1));
        animator.SetBool("is_walking", isWalking);

        Debug.Log($"moveX: {animator.GetFloat("movex")}, moveY: {animator.GetFloat("movey")}, isWalking: {isWalking}");
    }
}
