using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class EvidenceDatabase
    {
        private static readonly IReadOnlyList<string> EmptyEvidenceIds = Array.Empty<string>();
        private static readonly IReadOnlyList<EvidenceRelationshipData> EmptyRelationships = Array.Empty<EvidenceRelationshipData>();

        private readonly Dictionary<string, EvidenceNodeData> evidenceById;
        private readonly Dictionary<string, List<string>> evidenceIdsByLocationId;
        private readonly Dictionary<string, List<string>> evidenceRequirementsById;
        private readonly Dictionary<string, List<EvidenceRelationshipData>> relationshipsBySourceId;
        private readonly Dictionary<string, List<EvidenceRelationshipData>> relationshipsByTargetId;

        internal EvidenceDatabase(
            Dictionary<string, EvidenceNodeData> evidenceById,
            Dictionary<string, List<string>> evidenceIdsByLocationId,
            Dictionary<string, List<string>> evidenceRequirementsById,
            Dictionary<string, List<EvidenceRelationshipData>> relationshipsBySourceId,
            Dictionary<string, List<EvidenceRelationshipData>> relationshipsByTargetId)
        {
            this.evidenceById = evidenceById ?? new Dictionary<string, EvidenceNodeData>(StringComparer.Ordinal);
            this.evidenceIdsByLocationId = evidenceIdsByLocationId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.evidenceRequirementsById = evidenceRequirementsById ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.relationshipsBySourceId = relationshipsBySourceId ?? new Dictionary<string, List<EvidenceRelationshipData>>(StringComparer.Ordinal);
            this.relationshipsByTargetId = relationshipsByTargetId ?? new Dictionary<string, List<EvidenceRelationshipData>>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, EvidenceNodeData> EvidenceById => evidenceById;
        public IReadOnlyDictionary<string, List<string>> EvidenceIdsByLocationId => evidenceIdsByLocationId;
        public IReadOnlyDictionary<string, List<string>> EvidenceRequirementsById => evidenceRequirementsById;
        public IReadOnlyDictionary<string, List<EvidenceRelationshipData>> RelationshipsBySourceId => relationshipsBySourceId;
        public IReadOnlyDictionary<string, List<EvidenceRelationshipData>> RelationshipsByTargetId => relationshipsByTargetId;

        public bool TryGetEvidence(string evidenceId, out EvidenceNodeData evidence)
        {
            return evidenceById.TryGetValue(evidenceId, out evidence);
        }

        public IReadOnlyList<string> GetEvidenceIdsByLocation(string locationId)
        {
            return TryGetList(evidenceIdsByLocationId, locationId, EmptyEvidenceIds);
        }

        public IReadOnlyList<string> GetRequirements(string evidenceId)
        {
            return TryGetList(evidenceRequirementsById, evidenceId, EmptyEvidenceIds);
        }

        public IReadOnlyList<EvidenceRelationshipData> GetRelationshipsFrom(string evidenceId)
        {
            return TryGetList(relationshipsBySourceId, evidenceId, EmptyRelationships);
        }

        public IReadOnlyList<EvidenceRelationshipData> GetRelationshipsTo(string evidenceId)
        {
            return TryGetList(relationshipsByTargetId, evidenceId, EmptyRelationships);
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
