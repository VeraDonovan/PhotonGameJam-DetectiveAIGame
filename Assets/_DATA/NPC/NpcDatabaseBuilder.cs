using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class NpcDatabaseBuilder
    {
        public static NpcDatabase Build(NpcSetData npcSetData)
        {
            if (npcSetData == null)
            {
                throw new ArgumentNullException(nameof(npcSetData));
            }

            var npcById = new Dictionary<string, NpcData>(StringComparer.Ordinal);
            var npcLayersByNpcId = new Dictionary<string, List<NpcInterrogationLayerData>>(StringComparer.Ordinal);
            var triggerDialogById = new Dictionary<string, TriggerDialogData>(StringComparer.Ordinal);
            var triggerDialogIdsByNpcId = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var npc in npcSetData.npcs ?? new List<NpcData>())
            {
                if (npc == null || string.IsNullOrWhiteSpace(npc.npcId))
                {
                    throw new InvalidOperationException("NPC entry is missing an npcId.");
                }

                if (!npcById.TryAdd(npc.npcId, npc))
                {
                    throw new InvalidOperationException($"Duplicate npc id '{npc.npcId}'.");
                }

                npcLayersByNpcId[npc.npcId] =
                    new List<NpcInterrogationLayerData>(npc.interrogationLayers ?? new List<NpcInterrogationLayerData>());
            }

            foreach (var triggerDialog in npcSetData.triggerDialogs ?? new List<TriggerDialogData>())
            {
                if (triggerDialog == null || string.IsNullOrWhiteSpace(triggerDialog.dialogId))
                {
                    throw new InvalidOperationException("Trigger dialog entry is missing a dialogId.");
                }

                if (!triggerDialogById.TryAdd(triggerDialog.dialogId, triggerDialog))
                {
                    throw new InvalidOperationException($"Duplicate trigger dialog id '{triggerDialog.dialogId}'.");
                }

                foreach (var participantNpcId in triggerDialog.participants ?? new List<string>())
                {
                    if (!string.IsNullOrWhiteSpace(participantNpcId))
                    {
                        AddValue(triggerDialogIdsByNpcId, participantNpcId, triggerDialog.dialogId);
                    }
                }
            }

            return new NpcDatabase(
                npcById,
                npcLayersByNpcId,
                triggerDialogById,
                triggerDialogIdsByNpcId);
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
