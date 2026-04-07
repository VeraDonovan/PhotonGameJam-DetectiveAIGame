using System;
using DetectiveGame.Core;
using UnityEngine;

namespace DetectiveGame.Gameplay.Tests
{
    public sealed class NpcDiscoveryDebugInput : MonoBehaviour
    {
        [SerializeField] private KeyCode addNextNpcKey = KeyCode.N;
        [SerializeField] private KeyCode addAllNpcsKey = KeyCode.M;
        [SerializeField] private string[] suspectNpcIds = { "npc_1", "npc_2", "npc_3" };

        private AppRoot appRoot;
        private int nextNpcIndex;

        private void Awake()
        {
            appRoot = AppRoot.Instance;

            if (appRoot == null)
            {
                throw new InvalidOperationException("NpcDiscoveryDebugInput requires AppRoot.Instance.");
            }

            if (suspectNpcIds == null || suspectNpcIds.Length == 0)
            {
                throw new InvalidOperationException("NpcDiscoveryDebugInput requires at least one configured npc id.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(addNextNpcKey))
            {
                RegisterNextNpc();
            }

            if (Input.GetKeyDown(addAllNpcsKey))
            {
                RegisterAllNpcs();
            }
        }

        private void RegisterNextNpc()
        {
            if (nextNpcIndex >= suspectNpcIds.Length)
            {
                Debug.Log($"[NpcDiscoveryDebugInput] Key '{addNextNpcKey}' pressed but all configured NPCs are already registered.");
                return;
            }

            RegisterNpc(suspectNpcIds[nextNpcIndex], addNextNpcKey);
            nextNpcIndex++;
        }

        private void RegisterAllNpcs()
        {
            for (; nextNpcIndex < suspectNpcIds.Length; nextNpcIndex++)
            {
                RegisterNpc(suspectNpcIds[nextNpcIndex], addAllNpcsKey);
            }
        }

        private void RegisterNpc(string npcId, KeyCode sourceKey)
        {
            if (!appRoot.DatabaseManager.NpcDatabase.TryGetNpc(npcId, out var npc) || npc == null)
            {
                Debug.LogWarning($"[NpcDiscoveryDebugInput] Key '{sourceKey}' pressed but NpcId '{npcId}' was not found in NpcDatabase.");
                return;
            }

            var runtimeRegistered = appRoot.NpcRuntimeManager.RegisterNpc(npcId);
            var progressRegistered = appRoot.ProgressManager.RegisterSuspect(npcId);

            Debug.Log(
                $"[NpcDiscoveryDebugInput] Key '{sourceKey}' pressed. RegisterNpc '{npcId}' ({npc.displayName}). RuntimeAdded={runtimeRegistered}, ProgressAdded={progressRegistered}.");
        }
    }
}
