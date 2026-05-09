using UnityEngine;
using DetectiveGame.Gameplay.Npc;  

public class NPCInteraction_F : MonoBehaviour
{
    public float interactDistance = 2.5f;   // 玩家与 NPC 的交互距离
    public Transform player;                // 玩家对象引用
    public GameObject fPrompt;              // “按 F 互动”的提示 UI

    void Start()
    {
        fPrompt.SetActive(false); // 默认隐藏提示 UI
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        // 玩家靠近时显示提示
        if (distance <= interactDistance)
        {
            fPrompt.SetActive(true);

            // 按下 F 键时触发交互
            if (Input.GetKeyDown(KeyCode.F))
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, interactDistance);
                foreach (var col in colliders)
                {
                    GameplayNpc npc = col.GetComponent<GameplayNpc>();
                    if (npc != null)
                    {
                        npc.Interact(); // 统一调用 GameplayNpc 的交互逻辑
                        break;
                    }
                }
            }
        }
        else
        {
            fPrompt.SetActive(false); // 玩家离开时隐藏提示
        }
    }
}
