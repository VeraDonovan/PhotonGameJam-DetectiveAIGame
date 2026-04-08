using UnityEngine;

public class PlayerController : MonoBehaviour {
    public float speed = 5f;

    void Update() {
        float h = Input.GetAxis("Horizontal"); // A/D 或 左右方向键
        float v = Input.GetAxis("Vertical");   // W/S 或 上下方向键
        Vector3 move = new Vector3(h, v, 0);   // 2D移动，Z轴固定为0
        transform.Translate(move * speed * Time.deltaTime);
    }
}
