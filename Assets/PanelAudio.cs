using UnityEngine;

public class PanelAudio : MonoBehaviour
{
    public GameObject panel;       // 在 Inspector 里拖 Panel 进来
    public AudioSource audioSource;

    void Update()
    {
        if (panel.activeSelf)      // Panel 打开时
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else                       // Panel 关闭时
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
