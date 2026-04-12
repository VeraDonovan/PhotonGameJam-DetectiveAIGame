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
            }

            return new NpcDatabase(npcById);
        }
    }
}
