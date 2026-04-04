using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    [Serializable]
    public sealed class SceneLayoutData
    {
        public List<RoomData> rooms = new List<RoomData>();
    }

    [Serializable]
    public sealed class RoomData
    {
        public string roomId;
        public string displayName;
        public int objectCount;
        public List<SceneObjectData> objects = new List<SceneObjectData>();
    }

    [Serializable]
    public sealed class SceneObjectData
    {
        public string objectId;
        public string displayName;
        public string objectType;
        public string placementSlotId;
        public string placementPosition;
        public string placementRotation;
        public bool interactive;
        public bool canHideEvidence;
        public List<string> hiddenEvidenceIds = new List<string>();
        public string notes;
    }
}
