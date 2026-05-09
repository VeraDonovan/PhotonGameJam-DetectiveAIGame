using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TransitionUI : MonoBehaviour
{
    public Image blackScreen;
    public TextMeshProUGUI dialogueText;
    public float fadeDuration = 1f;
    public float charDelay = 0.05f;
    [TextArea] public string[] texts;

    private Coroutine transitionCoroutine;
    private Action onTransitionFinished;

    public void StartTransition(Action onFinished)
    {
        onTransitionFinished = onFinished;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(PlayTransition());
    }

    private IEnumerator PlayTransition()
    {
        var color = blackScreen.color;
        color.a = 0f;
        blackScreen.color = color;

        dialogueText.text = string.Empty;
        dialogueText.gameObject.SetActive(false);

        var time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, time / fadeDuration);
            blackScreen.color = color;
            yield return null;
        }

        dialogueText.gameObject.SetActive(true);

        foreach (var line in texts)
        {
            dialogueText.text = string.Empty;

            foreach (var character in line)
            {
                dialogueText.text += character;
                yield return new WaitForSeconds(charDelay);
            }

            yield return new WaitForSeconds(1f);
        }

        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

        transitionCoroutine = null;
        onTransitionFinished?.Invoke();
    }
}
