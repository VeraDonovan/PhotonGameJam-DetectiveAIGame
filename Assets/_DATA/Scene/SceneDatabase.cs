using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class SceneDatabase
    {
        private static readonly IReadOnlyList<string> EmptyIds = Array.Empty<string>();

        private readonly Dictionary<string, RoomData> roomById;
        private readonly Dictionary<string, SceneObjectData> objectById;
        private readonly Dictionary<string, List<string>> objectIdsByRoomId;
        private readonly Dictionary<string, List<string>> hiddenEvidenceIdsByObjectId;

        internal SceneDatabase(
            Dictionary<string, RoomData> roomById,
            Dictionary<string, SceneObjectData> objectById,
            Dictionary<string, List<string>> objectIdsByRoomId,
            Dictionary<string, List<string>> hiddenEvidenceIdsByObjectId)
        {
            this.roomById = roomById ?? new Dictionary<string, RoomData>(StringComparer.Ordinal);
            this.objectById = objectById ?? new Dictionary<string, SceneObjectData>(StringComparer.Ordinal);
            this.objectIdsByRoomId = objectIdsByRoomId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.hiddenEvidenceIdsByObjectId = hiddenEvidenceIdsByObjectId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, RoomData> RoomById => roomById;
        public IReadOnlyDictionary<string, SceneObjectData> ObjectById => objectById;
        public IReadOnlyDictionary<string, List<string>> ObjectIdsByRoomId => objectIdsByRoomId;
        public IReadOnlyDictionary<string, List<string>> HiddenEvidenceIdsByObjectId => hiddenEvidenceIdsByObjectId;

        public bool TryGetRoom(string roomId, out RoomData room)
        {
            return roomById.TryGetValue(roomId, out room);
        }

        public bool TryGetObject(string objectId, out SceneObjectData sceneObject)
        {
            return objectById.TryGetValue(objectId, out sceneObject);
        }

        public IReadOnlyList<string> GetObjectIdsByRoom(string roomId)
        {
            return TryGetList(objectIdsByRoomId, roomId, EmptyIds);
        }

        public IReadOnlyList<string> GetHiddenEvidenceIds(string objectId)
        {
            return TryGetList(hiddenEvidenceIdsByObjectId, objectId, EmptyIds);
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
