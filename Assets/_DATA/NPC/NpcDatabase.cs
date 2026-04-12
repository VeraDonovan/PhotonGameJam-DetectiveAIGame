using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class NpcDatabase
    {
        private readonly Dictionary<string, NpcData> npcById;

        internal NpcDatabase(Dictionary<string, NpcData> npcById)
        {
            this.npcById = npcById ?? new Dictionary<string, NpcData>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, NpcData> NpcById => npcById;

        public bool TryGetNpc(string npcId, out NpcData npc)
        {
            return npcById.TryGetValue(npcId, out npc);
        }
    }
}
