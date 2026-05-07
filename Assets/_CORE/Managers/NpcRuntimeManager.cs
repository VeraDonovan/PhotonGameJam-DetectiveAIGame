using System.Collections.Generic;
using System;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class NpcRuntimeManager : MonoBehaviour
    {
        private readonly HashSet<string> discoveredNpcIds = new HashSet<string>();
        private readonly HashSet<string> availableNpcIds = new HashSet<string>();
        private readonly HashSet<string> interrogationReadyNpcIds = new HashSet<string>();
        private readonly Dictionary<string, NpcDialogueRuntimeState> dialogueStateByNpcId =
            new Dictionary<string, NpcDialogueRuntimeState>();

        private EventManager eventManager;

        public IReadOnlyCollection<string> DiscoveredNpcIds => discoveredNpcIds;
        public IReadOnlyCollection<string> AvailableNpcIds => availableNpcIds;
        public IReadOnlyCollection<string> InterrogationReadyNpcIds => interrogationReadyNpcIds;
        public IReadOnlyDictionary<string, NpcDialogueRuntimeState> DialogueStateByNpcId => dialogueStateByNpcId;

        public void Initialize(EventManager sharedEventManager)
        {
            eventManager?.Unsubscribe<GamePhaseChangedEvent>(OnGamePhaseChanged);
            eventManager = sharedEventManager;
            ValidateDependencies();
            ResetRuntime();
            eventManager.Subscribe<GamePhaseChangedEvent>(OnGamePhaseChanged);
        }

        public bool RegisterNpc(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return false;
            }

            if (!discoveredNpcIds.Add(npcId))
            {
                return false;
            }

            GetOrCreateDialogueState(npcId);
            eventManager?.Publish(new NpcDiscoveredEvent(npcId));
            return true;
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

        public NpcDialogueRuntimeState GetOrCreateDialogueState(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                throw new ArgumentException("NPC dialogue runtime state requires an npcId.", nameof(npcId));
            }

            if (dialogueStateByNpcId.TryGetValue(npcId, out var state))
            {
                return state;
            }

            state = new NpcDialogueRuntimeState(npcId);
            dialogueStateByNpcId.Add(npcId, state);
            return state;
        }

        public bool TryGetDialogueState(string npcId, out NpcDialogueRuntimeState state)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                state = null;
                return false;
            }

            return dialogueStateByNpcId.TryGetValue(npcId, out state);
        }

        public int AddAnnoyance(string npcId, int amount)
        {
            var state = GetOrCreateDialogueState(npcId);
            state.AddAnnoyance(amount);
            return state.Annoyance;
        }

        public void MarkTopicDiscussed(string npcId, string topicId)
        {
            GetOrCreateDialogueState(npcId).MarkTopicDiscussed(topicId);
        }

        public void MarkTopicResolved(string npcId, string topicId)
        {
            GetOrCreateDialogueState(npcId).MarkTopicResolved(topicId);
        }

        public int AddInterrogationPressure(string npcId, int amount)
        {
            var state = GetOrCreateDialogueState(npcId);
            state.AddPressure(amount);
            return state.Pressure;
        }

        public void SetCurrentInterrogationLayer(string npcId, string layerId)
        {
            GetOrCreateDialogueState(npcId).SetCurrentInterrogationLayer(layerId);
        }

        public void ResetAllAnnoyance()
        {
            foreach (var state in dialogueStateByNpcId.Values)
            {
                state.ResetAnnoyance();
            }
        }

        public void ResetAllInterrogationPressure()
        {
            foreach (var state in dialogueStateByNpcId.Values)
            {
                state.ResetPressure();
            }
        }

        public void ResetRuntime()
        {
            discoveredNpcIds.Clear();
            availableNpcIds.Clear();
            interrogationReadyNpcIds.Clear();
            dialogueStateByNpcId.Clear();
        }

        private void OnDestroy()
        {
            eventManager?.Unsubscribe<GamePhaseChangedEvent>(OnGamePhaseChanged);
        }

        private void OnGamePhaseChanged(GamePhaseChangedEvent gamePhaseChangedEvent)
        {
            if (gamePhaseChangedEvent.Phase == GamePhase.Interrogation)
            {
                ResetAllAnnoyance();
                ResetAllInterrogationPressure();
            }
        }

        private void ValidateDependencies()
        {
            if (eventManager == null)
            {
                throw new InvalidOperationException("NpcRuntimeManager requires EventManager during initialization.");
            }
        }
    }
}
