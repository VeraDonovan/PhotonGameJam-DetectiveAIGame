using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class NpcAiProfileDatabaseBuilder
    {
        public static NpcAiProfileDatabase Build(IEnumerable<NpcAiProfileData> profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            var profileByNpcId = new Dictionary<string, NpcAiProfileData>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                if (profile == null || string.IsNullOrWhiteSpace(profile.npcId))
                {
                    throw new InvalidOperationException("NPC AI profile entry is missing an npcId.");
                }

                if (!profileByNpcId.TryAdd(profile.npcId, profile))
                {
                    throw new InvalidOperationException($"Duplicate NPC AI profile id '{profile.npcId}'.");
                }
            }

            return new NpcAiProfileDatabase(profileByNpcId);
        }
    }
}
