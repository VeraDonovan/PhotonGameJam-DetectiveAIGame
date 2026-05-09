using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DetectiveGame.Core; // 引入 GameStateManager 所在命名空间

public class TransitionUI : MonoBehaviour
{
    public Image blackScreen;
    public TextMeshProUGUI dialogueText;
    public float fadeDuration = 1f;
    public float charDelay = 0.05f;
    [TextArea] public string[] texts;

    public void StartTransition()
    {
        StartCoroutine(PlayTransition());
    }

    private IEnumerator PlayTransition()
    {
        // 黑屏淡入
        float t = 0;
        Color c = blackScreen.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeDuration);
            blackScreen.color = c;
            yield return null;
        }

        // 打字机效果
        Debug.Log("开始打字机效果");
        dialogueText.gameObject.SetActive(true);
        foreach (string line in texts)
        {
            dialogueText.text = "";
            foreach (char ch in line)
        {
            dialogueText.text += ch;
            Debug.Log("当前文字: " + dialogueText.text);
            yield return new WaitForSeconds(charDelay);
        }
        yield return new WaitForSeconds(1f);
        }

        Debug.Log("打字机效果结束，尝试开始游戏");
      
        
    }
}
