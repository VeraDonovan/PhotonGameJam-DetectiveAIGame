using UnityEngine;

namespace DetectiveGame.Gameplay.Npc
{
    public sealed class GameplayNpc : MonoBehaviour
    {
        [SerializeField] private string npcId = string.Empty;

        public string NpcId => npcId;

        public void Interact()
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                Debug.LogError("[GameplayNpc] Cannot interact because npcId is empty.", this);
                return;
            }

            DialogueController dialogueController = DialogueController.Instance;
            if (dialogueController == null)
            {
                Debug.LogError("[GameplayNpc] Cannot interact because DialogueController.Instance is missing.", this);
                return;
            }
            
            dialogueController.SetCurrentNpcId(npcId);
            dialogueController.StartNpcOpeningDialogue();
            
        }
    }
}
