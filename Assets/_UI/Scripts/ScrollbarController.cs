using UnityEngine;
using UnityEngine.UI;

public class ScrollbarController : MonoBehaviour
{
    public ScrollRect targetScrollRect;
    public float scrollStep = 0.1f; // 每次点击移动的量

    public void ScrollUp()
    {
        if (targetScrollRect != null)
        {
            // Move toward 1.0 (Top)
            targetScrollRect.verticalNormalizedPosition = Mathf.Clamp01(targetScrollRect.verticalNormalizedPosition + scrollStep);
        }
    }

    public void ScrollDown()
    {
        if (targetScrollRect != null)
        {
            // Move toward 0.0 (Bottom)
            targetScrollRect.verticalNormalizedPosition = Mathf.Clamp01(targetScrollRect.verticalNormalizedPosition - scrollStep);
        }
    }
}
