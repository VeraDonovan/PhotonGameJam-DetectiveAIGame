using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // 玩家
    public float smoothSpeed = 0.125f;
    public Vector3 offset;        // 摄像机与玩家的偏移量

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
