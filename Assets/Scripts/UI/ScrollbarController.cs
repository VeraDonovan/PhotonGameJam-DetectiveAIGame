using UnityEngine;
using UnityEngine.UI;

public class ScrollbarController : MonoBehaviour
{
    public Scrollbar scrollbar;
    public float step = 0.1f; // 每次点击移动的量

    public void ScrollUp()
    {
        scrollbar.value = Mathf.Clamp01(scrollbar.value + step);
    }

    public void ScrollDown()
    {
        scrollbar.value = Mathf.Clamp01(scrollbar.value - step);
    }
}
