using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class NpcAiProfileDatabase
    {
        private readonly Dictionary<string, NpcAiProfileData> profileByNpcId;

        internal NpcAiProfileDatabase(Dictionary<string, NpcAiProfileData> profileByNpcId)
        {
            this.profileByNpcId = profileByNpcId ?? new Dictionary<string, NpcAiProfileData>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, NpcAiProfileData> ProfileByNpcId => profileByNpcId;

        public bool TryGetProfile(string npcId, out NpcAiProfileData profile)
        {
            return profileByNpcId.TryGetValue(npcId, out profile);
        }
    }
}
