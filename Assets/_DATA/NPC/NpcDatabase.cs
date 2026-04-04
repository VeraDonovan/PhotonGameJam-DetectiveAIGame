using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class NpcDatabase
    {
        private static readonly IReadOnlyList<NpcInterrogationLayerData> EmptyLayers =
            Array.Empty<NpcInterrogationLayerData>();

        private static readonly IReadOnlyList<string> EmptyIds = Array.Empty<string>();

        private readonly Dictionary<string, NpcData> npcById;
        private readonly Dictionary<string, List<NpcInterrogationLayerData>> npcLayersByNpcId;
        private readonly Dictionary<string, TriggerDialogData> triggerDialogById;
        private readonly Dictionary<string, List<string>> triggerDialogIdsByNpcId;

        internal NpcDatabase(
            Dictionary<string, NpcData> npcById,
            Dictionary<string, List<NpcInterrogationLayerData>> npcLayersByNpcId,
            Dictionary<string, TriggerDialogData> triggerDialogById,
            Dictionary<string, List<string>> triggerDialogIdsByNpcId)
        {
            this.npcById = npcById ?? new Dictionary<string, NpcData>(StringComparer.Ordinal);
            this.npcLayersByNpcId = npcLayersByNpcId ?? new Dictionary<string, List<NpcInterrogationLayerData>>(StringComparer.Ordinal);
            this.triggerDialogById = triggerDialogById ?? new Dictionary<string, TriggerDialogData>(StringComparer.Ordinal);
            this.triggerDialogIdsByNpcId = triggerDialogIdsByNpcId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, NpcData> NpcById => npcById;
        public IReadOnlyDictionary<string, List<NpcInterrogationLayerData>> NpcLayersByNpcId => npcLayersByNpcId;
        public IReadOnlyDictionary<string, TriggerDialogData> TriggerDialogById => triggerDialogById;
        public IReadOnlyDictionary<string, List<string>> TriggerDialogIdsByNpcId => triggerDialogIdsByNpcId;

        public bool TryGetNpc(string npcId, out NpcData npc)
        {
            return npcById.TryGetValue(npcId, out npc);
        }

        public bool TryGetTriggerDialog(string dialogId, out TriggerDialogData triggerDialog)
        {
            return triggerDialogById.TryGetValue(dialogId, out triggerDialog);
        }

        public IReadOnlyList<NpcInterrogationLayerData> GetLayers(string npcId)
        {
            return TryGetList(npcLayersByNpcId, npcId, EmptyLayers);
        }

        public IReadOnlyList<string> GetTriggerDialogIdsByNpc(string npcId)
        {
            return TryGetList(triggerDialogIdsByNpcId, npcId, EmptyIds);
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
