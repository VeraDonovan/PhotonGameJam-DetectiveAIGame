using System;
using DetectiveGame.Core;
using UnityEngine;
using CoreNpcData = DetectiveGame.Core.NpcData;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class DbNpcDialogueTestInteraction : MonoBehaviour
    {
        [Header("NPC")]
        [SerializeField] private string npcId = "npc_1";

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private string playerTag = "Player";

        [Header("Dialogue")]
        [SerializeField] private DialogueController dialogueController;

        private CoreNpcData databaseNpcData;
        private NPCData dialogueNpcData;
        private bool playerInRange;

        private void Start()
        {
            LoadNpcData();
            dialogueNpcData = BuildDialogueNpcData(databaseNpcData);
            ResolveDialogueController();
        }

        private void Update()
        {
            UpdateInteraction();
        }

        private void LoadNpcData()
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                throw new InvalidOperationException("DbNpcDialogueTestInteraction requires an npcId.");
            }

            var appRoot = AppRoot.Instance;
            if (appRoot == null || appRoot.DatabaseManager == null || appRoot.DatabaseManager.NpcDatabase == null)
            {
                throw new InvalidOperationException("DbNpcDialogueTestInteraction requires AppRoot.DatabaseManager.NpcDatabase.");
            }

            if (!appRoot.DatabaseManager.NpcDatabase.TryGetNpc(npcId, out databaseNpcData) || databaseNpcData == null)
            {
                throw new InvalidOperationException($"DbNpcDialogueTestInteraction could not find npcId '{npcId}' in NpcDatabase.");
            }
        }

        private void UpdateInteraction()
        {
            if (!playerInRange || dialogueNpcData == null)
            {
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                StartDialogue();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInRange = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInRange = false;
            }
        }

        private void StartDialogue()
        {
            ResolveDialogueController();

            var appRoot = AppRoot.Instance;
            appRoot?.NpcRuntimeManager?.RegisterNpc(npcId);
            appRoot?.ProgressManager?.RegisterSuspect(npcId);

            if (dialogueController != null)
            {
                dialogueController.SetCurrentNPC(dialogueNpcData);
            }

            dialogueController?.StartNpcOpeningDialogue();
        }

        private void ResolveDialogueController()
        {
            if (dialogueController == null)
            {
                dialogueController = DialogueController.Instance;
            }
        }

        private static NPCData BuildDialogueNpcData(CoreNpcData source)
        {
            return new NPCData
            {
                npcId = source.npcId,
                roleType = source.roleType,
                displayName = source.displayName,
                age = source.age,
                locationId = source.locationId,
                relationshipToVictim = source.relationshipToVictim,
                backstory = source.profileText,
                initialStatement = source.profileText
            };
        }
    }
}
