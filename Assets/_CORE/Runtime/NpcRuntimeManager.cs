using System.Collections.Generic;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class NpcRuntimeManager : MonoBehaviour
    {
        private readonly HashSet<string> discoveredNpcIds = new HashSet<string>();
        private readonly HashSet<string> availableNpcIds = new HashSet<string>();
        private readonly HashSet<string> interrogationReadyNpcIds = new HashSet<string>();

        private EventManager eventManager;

        public IReadOnlyCollection<string> DiscoveredNpcIds => discoveredNpcIds;
        public IReadOnlyCollection<string> AvailableNpcIds => availableNpcIds;
        public IReadOnlyCollection<string> InterrogationReadyNpcIds => interrogationReadyNpcIds;

        public void Initialize(EventManager sharedEventManager)
        {
            eventManager = sharedEventManager;
            ResetRuntime();
        }

        public bool RegisterNpc(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return false;
            }

            return discoveredNpcIds.Add(npcId);
        }

        public bool SetNpcAvailability(string npcId, bool isAvailable)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return false;
            }

            if (isAvailable)
            {
                return availableNpcIds.Add(npcId);
            }

            return availableNpcIds.Remove(npcId);
        }

        public bool SetInterrogationReady(string npcId, bool isReady)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return false;
            }

            if (isReady)
            {
                return interrogationReadyNpcIds.Add(npcId);
            }

            return interrogationReadyNpcIds.Remove(npcId);
        }

        public void ResetRuntime()
        {
            discoveredNpcIds.Clear();
            availableNpcIds.Clear();
            interrogationReadyNpcIds.Clear();
        }
    }
}
