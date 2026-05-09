using UnityEngine;

public class NPCInteraction_F : MonoBehaviour
{
    public float interactDistance = 2.5f;
    public Transform player;
    public GameObject fPrompt; 

    private NPCAssembler assembler;

    void Start()
    {
        assembler = GetComponent<NPCAssembler>();
        fPrompt.SetActive(false);
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        // 玩家靠近
        if (distance <= interactDistance)
        {
            fPrompt.SetActive(true);

            // 按下 F 键
            if (Input.GetKeyDown(KeyCode.F))
            {
                DialogueManager.Instance.ShowDialogue(assembler.dialogueConfig.initialStatement);
            }
        }
        else
        {
            fPrompt.SetActive(false);
        }
    }
}
