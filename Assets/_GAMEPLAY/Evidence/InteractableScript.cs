using DetectiveGame.Core;
using TMPro;
using UnityEngine;

public class InteractableScript : MonoBehaviour
{
    public string dialogueText = "这是物品的描述";
    public string evidenceId = string.Empty;
    public float interactDistance = 1.5f;

    private AppRoot appRoot;
    private Transform player;
    private bool isPlayerNear = false;

    public Canvas hintCanvas;
    public TMP_Text hintText;

    void Start()
    {
        appRoot = AppRoot.Instance;
        player = GameObject.FindWithTag("Player").transform;
        hintCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= interactDistance)
        {
            if (!isPlayerNear)
            {
                isPlayerNear = true;
                hintText.text = "按 F 互动";
                hintCanvas.gameObject.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                InteractionUI.Instance.ShowDialogue(dialogueText);

                if (!string.IsNullOrWhiteSpace(evidenceId))
                {
                    appRoot.ProgressManager.AddEvidence(evidenceId);
                }
            }
        }
        else
        {
            if (isPlayerNear)
            {
                isPlayerNear = false;
                hintCanvas.gameObject.SetActive(false);
            }
        }
    }
}
