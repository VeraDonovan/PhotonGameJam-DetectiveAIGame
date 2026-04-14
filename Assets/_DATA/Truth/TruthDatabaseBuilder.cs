using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class TruthDatabaseBuilder
    {
        public static TruthDatabase Build(TruthData truthData)
        {
            if (truthData == null)
            {
                throw new ArgumentNullException(nameof(truthData));
            }

            var npcTruthByNpcId = new Dictionary<string, NpcTruthData>(StringComparer.Ordinal);
            var dialogueTriggerById = new Dictionary<string, DialogueTriggerData>(StringComparer.Ordinal);
            var dialogueTriggerIdsByNpcId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var dialogueTriggersByNpcId = new Dictionary<string, List<DialogueTriggerData>>(StringComparer.Ordinal);
            var interrogationLayerById = new Dictionary<string, TruthInterrogationLayerData>(StringComparer.Ordinal);
            var interrogationLayerIdsByNpcId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var interrogationLayersByNpcId = new Dictionary<string, List<TruthInterrogationLayerData>>(StringComparer.Ordinal);
            var deductionTruthById = new Dictionary<string, DeductionTruthData>(StringComparer.Ordinal);

            foreach (var npcTruth in truthData.npcTruths ?? new List<NpcTruthData>())
            {
                if (npcTruth == null || string.IsNullOrWhiteSpace(npcTruth.npcId))
                {
                    throw new InvalidOperationException("NPC truth entry is missing an npcId.");
                }

                if (!npcTruthByNpcId.TryAdd(npcTruth.npcId, npcTruth))
                {
                    throw new InvalidOperationException($"Duplicate NPC truth id '{npcTruth.npcId}'.");
                }

                foreach (var dialogueTrigger in npcTruth.dialogueTriggers ?? new List<DialogueTriggerData>())
                {
                    if (dialogueTrigger == null || string.IsNullOrWhiteSpace(dialogueTrigger.triggerId))
                    {
                        throw new InvalidOperationException($"NPC truth '{npcTruth.npcId}' contains a dialogue trigger without a triggerId.");
                    }

                    if (!dialogueTriggerById.TryAdd(dialogueTrigger.triggerId, dialogueTrigger))
                    {
                        throw new InvalidOperationException($"Duplicate dialogue trigger id '{dialogueTrigger.triggerId}'.");
                    }

                    AddValue(dialogueTriggerIdsByNpcId, npcTruth.npcId, dialogueTrigger.triggerId);
                    AddValue(dialogueTriggersByNpcId, npcTruth.npcId, dialogueTrigger);
                }

                foreach (var layer in npcTruth.interrogationLayers ?? new List<TruthInterrogationLayerData>())
                {
                    if (layer == null || string.IsNullOrWhiteSpace(layer.layerId))
                    {
                        throw new InvalidOperationException($"NPC truth '{npcTruth.npcId}' contains an interrogation layer without a layerId.");
                    }

                    if (!interrogationLayerById.TryAdd(layer.layerId, layer))
                    {
                        throw new InvalidOperationException($"Duplicate interrogation layer id '{layer.layerId}'.");
                    }

                    AddValue(interrogationLayerIdsByNpcId, npcTruth.npcId, layer.layerId);
                    AddValue(interrogationLayersByNpcId, npcTruth.npcId, layer);
                }
            }

            foreach (var deductionTruth in truthData.deductionTruths ?? new List<DeductionTruthData>())
            {
                if (deductionTruth == null || string.IsNullOrWhiteSpace(deductionTruth.truthId))
                {
                    throw new InvalidOperationException("Deduction truth entry is missing a truthId.");
                }

                if (!deductionTruthById.TryAdd(deductionTruth.truthId, deductionTruth))
                {
                    throw new InvalidOperationException($"Duplicate deduction truth id '{deductionTruth.truthId}'.");
                }
            }

            return new TruthDatabase(
                truthData,
                npcTruthByNpcId,
                dialogueTriggerById,
                dialogueTriggerIdsByNpcId,
                dialogueTriggersByNpcId,
                interrogationLayerById,
                interrogationLayerIdsByNpcId,
                interrogationLayersByNpcId,
                deductionTruthById);
        }

        private static void AddValue<T>(Dictionary<string, List<T>> source, string key, T value)
        {
            if (!source.TryGetValue(key, out var values))
            {
                values = new List<T>();
                source[key] = values;
            }

            values.Add(value);
        }
    }
}
