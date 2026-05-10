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
        private DatabaseManager databaseManager;
        private GamePhase currentPhase = GamePhase.Exploration;

        public IReadOnlyCollection<string> DiscoveredNpcIds => discoveredNpcIds;
        public IReadOnlyCollection<string> AvailableNpcIds => availableNpcIds;
        public IReadOnlyCollection<string> InterrogationReadyNpcIds => interrogationReadyNpcIds;
        public IReadOnlyDictionary<string, NpcDialogueRuntimeState> DialogueStateByNpcId => dialogueStateByNpcId;

        public void Initialize(EventManager sharedEventManager, DatabaseManager sharedDatabaseManager)
        {
            eventManager?.Unsubscribe<GamePhaseChangedEvent>(OnGamePhaseChanged);
            eventManager = sharedEventManager;
            databaseManager = sharedDatabaseManager;
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
            if (currentPhase == GamePhase.Interrogation)
            {
                SetInitialInterrogationLayer(state);
            }

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
            var oldAnnoyance = state.Annoyance;
            state.AddAnnoyance(amount);
            PublishAnnoyanceChangedIfNeeded(state.NpcId, oldAnnoyance, state.Annoyance);
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
            var oldPressure = state.Pressure;
            state.AddPressure(amount);
            PublishPressureChangedIfNeeded(state.NpcId, oldPressure, state.Pressure);
            return state.Pressure;
        }

        public void SetCurrentInterrogationLayer(string npcId, string layerId, int level)
        {
            var state = GetOrCreateDialogueState(npcId);
            var oldLayerId = state.CurrentInterrogationLayerId;
            var oldLevel = state.CurrentInterrogationLevel;

            state.SetCurrentInterrogationLayer(layerId, level);

            PublishInterrogationLevelChangedIfNeeded(
                state.NpcId,
                oldLevel,
                state.CurrentInterrogationLevel,
                oldLayerId,
                state.CurrentInterrogationLayerId);
        }

        public void SetInterrogationLevel(string npcId, int level)
        {
            SetInterrogationLevel(GetOrCreateDialogueState(npcId), level);
        }

        private void SetInterrogationLevel(NpcDialogueRuntimeState state, int level)
        {
            var oldLayerId = state.CurrentInterrogationLayerId;
            var oldLevel = state.CurrentInterrogationLevel;
            state.SetCurrentInterrogationLevel(level);

            PublishInterrogationLevelChangedIfNeeded(
                state.NpcId,
                oldLevel,
                state.CurrentInterrogationLevel,
                oldLayerId,
                state.CurrentInterrogationLayerId);
        }

        private void PublishInterrogationLevelChangedIfNeeded(
            string npcId,
            int oldLevel,
            int newLevel,
            string oldLayerId,
            string newLayerId)
        {
            if (newLevel > oldLevel)
            {
                eventManager?.Publish(new NpcInterrogationLevelChangedEvent(
                    npcId,
                    oldLevel,
                    newLevel,
                    oldLayerId,
                    newLayerId));
            }
        }

        private void PublishAnnoyanceChangedIfNeeded(string npcId, int oldValue, int newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            eventManager?.Publish(new NpcAnnoyanceChangedEvent(npcId, oldValue, newValue, currentPhase));
        }

        private void PublishPressureChangedIfNeeded(string npcId, int oldValue, int newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            eventManager?.Publish(new NpcPressureChangedEvent(npcId, oldValue, newValue, currentPhase));
        }

        public void ResetAllAnnoyance()
        {
            foreach (var state in dialogueStateByNpcId.Values)
            {
                var oldAnnoyance = state.Annoyance;
                state.ResetAnnoyance();
                PublishAnnoyanceChangedIfNeeded(state.NpcId, oldAnnoyance, state.Annoyance);
            }
        }

        public void ResetAllInterrogationPressure()
        {
            foreach (var state in dialogueStateByNpcId.Values)
            {
                var oldPressure = state.Pressure;
                state.ResetPressure();
                PublishPressureChangedIfNeeded(state.NpcId, oldPressure, state.Pressure);
            }
        }

        public void ResetRuntime()
        {
            discoveredNpcIds.Clear();
            availableNpcIds.Clear();
            interrogationReadyNpcIds.Clear();
            dialogueStateByNpcId.Clear();
            currentPhase = GamePhase.Exploration;
        }

        private void OnDestroy()
        {
            eventManager?.Unsubscribe<GamePhaseChangedEvent>(OnGamePhaseChanged);
        }

        private void OnGamePhaseChanged(GamePhaseChangedEvent gamePhaseChangedEvent)
        {
            currentPhase = gamePhaseChangedEvent.Phase;

            if (gamePhaseChangedEvent.Phase == GamePhase.Interrogation)
            {
                ResetAllAnnoyance();
                ResetAllInterrogationPressure();
                SetAllInitialInterrogationLayers();
                return;
            }

            if (gamePhaseChangedEvent.Phase == GamePhase.Exploration)
            {
                ResetAllInterrogationPressure();
            }
        }

        private void SetAllInitialInterrogationLayers()
        {
            foreach (var state in dialogueStateByNpcId.Values)
            {
                SetInitialInterrogationLayer(state);
            }
        }

        private void SetInitialInterrogationLayer(NpcDialogueRuntimeState state)
        {
            var initialLayerId = GetInterrogationLayerIdForState(state.NpcId, 1);
            if (string.IsNullOrWhiteSpace(initialLayerId))
            {
                SetInterrogationLevel(state, 1);
                return;
            }

            SetCurrentInterrogationLayer(state.NpcId, initialLayerId, 1);
        }

        private string GetInterrogationLayerIdForState(string npcId, int stateLevel)
        {
            var level = 0;
            foreach (var layer in databaseManager.TruthDatabase.GetInterrogationLayersByNpc(npcId))
            {
                level++;
                if (level == stateLevel && layer != null)
                {
                    return layer.layerId ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private void ValidateDependencies()
        {
            if (eventManager == null)
            {
                throw new InvalidOperationException("NpcRuntimeManager requires EventManager during initialization.");
            }

            if (databaseManager == null)
            {
                throw new InvalidOperationException("NpcRuntimeManager requires DatabaseManager during initialization.");
            }

            if (databaseManager.TruthDatabase == null)
            {
                throw new InvalidOperationException("NpcRuntimeManager requires DatabaseManager.TruthDatabase during initialization.");
            }
        }
    }
}
