using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class NpcSetData
    {
        public List<NpcData> npcs = new List<NpcData>();
    }

    [Serializable]
    public sealed class NpcData
    {
        public string npcId;
        public string roleType;
        public string displayName;
        public string gender;
        public int age;
        public string occupation;
        public string locationId;
        public string relationshipToVictim;
        public string profileText;
    }
}
