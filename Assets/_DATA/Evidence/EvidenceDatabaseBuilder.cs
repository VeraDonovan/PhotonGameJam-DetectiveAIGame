using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class EvidenceDatabaseBuilder
    {
        public static EvidenceDatabase Build(EvidenceGraphData graphData)
        {
            if (graphData == null)
            {
                throw new ArgumentNullException(nameof(graphData));
            }

            var evidenceById = new Dictionary<string, EvidenceNodeData>(StringComparer.Ordinal);
            var evidenceIdsByLocationId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var evidenceRequirementsById = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var relationshipsBySourceId = new Dictionary<string, List<EvidenceRelationshipData>>(StringComparer.Ordinal);
            var relationshipsByTargetId = new Dictionary<string, List<EvidenceRelationshipData>>(StringComparer.Ordinal);

            foreach (var evidenceNode in graphData.evidenceNodes ?? new List<EvidenceNodeData>())
            {
                if (evidenceNode == null || string.IsNullOrWhiteSpace(evidenceNode.evidenceId))
                {
                    throw new InvalidOperationException("Evidence node is missing an evidenceId.");
                }

                if (!evidenceById.TryAdd(evidenceNode.evidenceId, evidenceNode))
                {
                    throw new InvalidOperationException($"Duplicate evidence id '{evidenceNode.evidenceId}'.");
                }

                evidenceRequirementsById[evidenceNode.evidenceId] =
                    new List<string>(evidenceNode.requirements ?? new List<string>());

                if (!string.IsNullOrWhiteSpace(evidenceNode.locationId))
                {
                    AddValue(evidenceIdsByLocationId, evidenceNode.locationId, evidenceNode.evidenceId);
                }
            }

            foreach (var relationship in graphData.relationships ?? new List<EvidenceRelationshipData>())
            {
                if (relationship == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(relationship.fromId) || string.IsNullOrWhiteSpace(relationship.toId))
                {
                    throw new InvalidOperationException("Evidence relationship is missing fromId or toId.");
                }

                AddValue(relationshipsBySourceId, relationship.fromId, relationship);
                AddValue(relationshipsByTargetId, relationship.toId, relationship);
            }

            return new EvidenceDatabase(
                evidenceById,
                evidenceIdsByLocationId,
                evidenceRequirementsById,
                relationshipsBySourceId,
                relationshipsByTargetId);
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
