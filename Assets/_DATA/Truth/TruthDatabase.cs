using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class TruthDatabase
    {
        private static readonly IReadOnlyList<string> EmptyIds = Array.Empty<string>();
        private static readonly IReadOnlyList<DialogueTriggerData> EmptyDialogueTriggers =
            Array.Empty<DialogueTriggerData>();
        private static readonly IReadOnlyList<TruthInterrogationLayerData> EmptyInterrogationLayers =
            Array.Empty<TruthInterrogationLayerData>();

        private readonly TruthData truthData;
        private readonly Dictionary<string, NpcTruthData> npcTruthByNpcId;
        private readonly Dictionary<string, DialogueTriggerData> dialogueTriggerById;
        private readonly Dictionary<string, List<string>> dialogueTriggerIdsByNpcId;
        private readonly Dictionary<string, List<DialogueTriggerData>> dialogueTriggersByNpcId;
        private readonly Dictionary<string, TruthInterrogationLayerData> interrogationLayerById;
        private readonly Dictionary<string, List<string>> interrogationLayerIdsByNpcId;
        private readonly Dictionary<string, List<TruthInterrogationLayerData>> interrogationLayersByNpcId;
        private readonly Dictionary<string, DeductionTruthData> deductionTruthById;

        internal TruthDatabase(
            TruthData truthData,
            Dictionary<string, NpcTruthData> npcTruthByNpcId,
            Dictionary<string, DialogueTriggerData> dialogueTriggerById,
            Dictionary<string, List<string>> dialogueTriggerIdsByNpcId,
            Dictionary<string, List<DialogueTriggerData>> dialogueTriggersByNpcId,
            Dictionary<string, TruthInterrogationLayerData> interrogationLayerById,
            Dictionary<string, List<string>> interrogationLayerIdsByNpcId,
            Dictionary<string, List<TruthInterrogationLayerData>> interrogationLayersByNpcId,
            Dictionary<string, DeductionTruthData> deductionTruthById)
        {
            this.truthData = truthData;
            this.npcTruthByNpcId = npcTruthByNpcId ?? new Dictionary<string, NpcTruthData>(StringComparer.Ordinal);
            this.dialogueTriggerById = dialogueTriggerById ?? new Dictionary<string, DialogueTriggerData>(StringComparer.Ordinal);
            this.dialogueTriggerIdsByNpcId = dialogueTriggerIdsByNpcId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.dialogueTriggersByNpcId = dialogueTriggersByNpcId ?? new Dictionary<string, List<DialogueTriggerData>>(StringComparer.Ordinal);
            this.interrogationLayerById = interrogationLayerById ?? new Dictionary<string, TruthInterrogationLayerData>(StringComparer.Ordinal);
            this.interrogationLayerIdsByNpcId = interrogationLayerIdsByNpcId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.interrogationLayersByNpcId = interrogationLayersByNpcId ?? new Dictionary<string, List<TruthInterrogationLayerData>>(StringComparer.Ordinal);
            this.deductionTruthById = deductionTruthById ?? new Dictionary<string, DeductionTruthData>(StringComparer.Ordinal);
        }

        public TruthData TruthData => truthData;
        public CaseTruthData CaseTruth => truthData?.caseTruth;
        public IReadOnlyDictionary<string, NpcTruthData> NpcTruthByNpcId => npcTruthByNpcId;
        public IReadOnlyDictionary<string, DialogueTriggerData> DialogueTriggerById => dialogueTriggerById;
        public IReadOnlyDictionary<string, List<string>> DialogueTriggerIdsByNpcId => dialogueTriggerIdsByNpcId;
        public IReadOnlyDictionary<string, List<DialogueTriggerData>> DialogueTriggersByNpcId => dialogueTriggersByNpcId;
        public IReadOnlyDictionary<string, TruthInterrogationLayerData> InterrogationLayerById => interrogationLayerById;
        public IReadOnlyDictionary<string, List<string>> InterrogationLayerIdsByNpcId => interrogationLayerIdsByNpcId;
        public IReadOnlyDictionary<string, List<TruthInterrogationLayerData>> InterrogationLayersByNpcId => interrogationLayersByNpcId;
        public IReadOnlyDictionary<string, DeductionTruthData> DeductionTruthById => deductionTruthById;

        public bool TryGetNpcTruth(string npcId, out NpcTruthData npcTruth)
        {
            return npcTruthByNpcId.TryGetValue(npcId, out npcTruth);
        }

        public bool TryGetDialogueTrigger(string triggerId, out DialogueTriggerData dialogueTrigger)
        {
            return dialogueTriggerById.TryGetValue(triggerId, out dialogueTrigger);
        }

        public bool TryGetInterrogationLayer(string layerId, out TruthInterrogationLayerData layer)
        {
            return interrogationLayerById.TryGetValue(layerId, out layer);
        }

        public bool TryGetDeductionTruth(string truthId, out DeductionTruthData deductionTruth)
        {
            return deductionTruthById.TryGetValue(truthId, out deductionTruth);
        }

        public IReadOnlyList<string> GetDialogueTriggerIdsByNpc(string npcId)
        {
            return TryGetList(dialogueTriggerIdsByNpcId, npcId, EmptyIds);
        }

        public IReadOnlyList<DialogueTriggerData> GetDialogueTriggersByNpc(string npcId)
        {
            return TryGetList(dialogueTriggersByNpcId, npcId, EmptyDialogueTriggers);
        }

        public IReadOnlyList<string> GetInterrogationLayerIdsByNpc(string npcId)
        {
            return TryGetList(interrogationLayerIdsByNpcId, npcId, EmptyIds);
        }

        public IReadOnlyList<TruthInterrogationLayerData> GetInterrogationLayersByNpc(string npcId)
        {
            return TryGetList(interrogationLayersByNpcId, npcId, EmptyInterrogationLayers);
        }

        private static IReadOnlyList<T> TryGetList<T>(
            IReadOnlyDictionary<string, List<T>> source,
            string key,
            IReadOnlyList<T> emptyValue)
        {
            if (string.IsNullOrWhiteSpace(key) || !source.TryGetValue(key, out var values))
            {
                return emptyValue;
            }

            return values;
        }
    }
}
