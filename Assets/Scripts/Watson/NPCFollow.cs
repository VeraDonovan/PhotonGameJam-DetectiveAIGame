using UnityEngine;

public class NPCFollow : MonoBehaviour {
    public Transform player;
    public float followSpeed = 3f;
    public float stopDistance = 2f;

    void Update() {
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > stopDistance) {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                followSpeed * Time.deltaTime
            );
        }
    }
}
