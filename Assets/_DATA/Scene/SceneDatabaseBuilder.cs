using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class SceneDatabaseBuilder
    {
        public static SceneDatabase Build(SceneLayoutData sceneLayoutData)
        {
            if (sceneLayoutData == null)
            {
                throw new ArgumentNullException(nameof(sceneLayoutData));
            }

            var roomById = new Dictionary<string, RoomData>(StringComparer.Ordinal);
            var objectById = new Dictionary<string, SceneObjectData>(StringComparer.Ordinal);
            var objectIdsByRoomId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var hiddenEvidenceIdsByObjectId = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var room in sceneLayoutData.rooms ?? new List<RoomData>())
            {
                if (room == null || string.IsNullOrWhiteSpace(room.roomId))
                {
                    throw new InvalidOperationException("Room entry is missing a roomId.");
                }

                if (!roomById.TryAdd(room.roomId, room))
                {
                    throw new InvalidOperationException($"Duplicate room id '{room.roomId}'.");
                }

                foreach (var sceneObject in room.objects ?? new List<SceneObjectData>())
                {
                    if (sceneObject == null || string.IsNullOrWhiteSpace(sceneObject.objectId))
                    {
                        throw new InvalidOperationException($"Room '{room.roomId}' contains an object without an objectId.");
                    }

                    if (!objectById.TryAdd(sceneObject.objectId, sceneObject))
                    {
                        throw new InvalidOperationException($"Duplicate object id '{sceneObject.objectId}'.");
                    }

                    AddValue(objectIdsByRoomId, room.roomId, sceneObject.objectId);
                    hiddenEvidenceIdsByObjectId[sceneObject.objectId] =
                        new List<string>(sceneObject.hiddenEvidenceIds ?? new List<string>());
                }
            }

            return new SceneDatabase(
                roomById,
                objectById,
                objectIdsByRoomId,
                hiddenEvidenceIdsByObjectId);
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
