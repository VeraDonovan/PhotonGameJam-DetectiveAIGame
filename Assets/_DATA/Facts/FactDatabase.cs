using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class FactDatabase
    {
        private static readonly IReadOnlyList<string> EmptyIds = Array.Empty<string>();
        private static readonly IReadOnlyList<FactRelationshipData> EmptyRelationships = Array.Empty<FactRelationshipData>();

        private readonly Dictionary<string, FactData> factById;
        private readonly Dictionary<string, List<string>> requirementsAllByFactId;
        private readonly Dictionary<string, List<string>> requirementsAnyByFactId;
        private readonly Dictionary<string, List<string>> factsByNpcId;
        private readonly Dictionary<string, List<string>> factsByLocationId;
        private readonly Dictionary<string, List<FactRelationshipData>> relationshipsBySourceFactId;
        private readonly Dictionary<string, List<FactRelationshipData>> relationshipsByTargetFactId;

        internal FactDatabase(
            Dictionary<string, FactData> factById,
            Dictionary<string, List<string>> requirementsAllByFactId,
            Dictionary<string, List<string>> requirementsAnyByFactId,
            Dictionary<string, List<string>> factsByNpcId,
            Dictionary<string, List<string>> factsByLocationId,
            Dictionary<string, List<FactRelationshipData>> relationshipsBySourceFactId,
            Dictionary<string, List<FactRelationshipData>> relationshipsByTargetFactId)
        {
            this.factById = factById ?? new Dictionary<string, FactData>(StringComparer.Ordinal);
            this.requirementsAllByFactId = requirementsAllByFactId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.requirementsAnyByFactId = requirementsAnyByFactId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.factsByNpcId = factsByNpcId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.factsByLocationId = factsByLocationId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.relationshipsBySourceFactId = relationshipsBySourceFactId ?? new Dictionary<string, List<FactRelationshipData>>(StringComparer.Ordinal);
            this.relationshipsByTargetFactId = relationshipsByTargetFactId ?? new Dictionary<string, List<FactRelationshipData>>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, FactData> FactById => factById;
        public IReadOnlyDictionary<string, List<string>> RequirementsAllByFactId => requirementsAllByFactId;
        public IReadOnlyDictionary<string, List<string>> RequirementsAnyByFactId => requirementsAnyByFactId;
        public IReadOnlyDictionary<string, List<string>> FactsByNpcId => factsByNpcId;
        public IReadOnlyDictionary<string, List<string>> FactsByLocationId => factsByLocationId;
        public IReadOnlyDictionary<string, List<FactRelationshipData>> RelationshipsBySourceFactId => relationshipsBySourceFactId;
        public IReadOnlyDictionary<string, List<FactRelationshipData>> RelationshipsByTargetFactId => relationshipsByTargetFactId;

        public bool TryGetFact(string factId, out FactData fact)
        {
            return factById.TryGetValue(factId, out fact);
        }

        public IReadOnlyList<string> GetRequirementsAll(string factId)
        {
            return TryGetList(requirementsAllByFactId, factId, EmptyIds);
        }

        public IReadOnlyList<string> GetRequirementsAny(string factId)
        {
            return TryGetList(requirementsAnyByFactId, factId, EmptyIds);
        }

        public IReadOnlyList<string> GetFactIdsByNpc(string npcId)
        {
            return TryGetList(factsByNpcId, npcId, EmptyIds);
        }

        public IReadOnlyList<string> GetFactIdsByLocation(string locationId)
        {
            return TryGetList(factsByLocationId, locationId, EmptyIds);
        }

        public IReadOnlyList<FactRelationshipData> GetRelationshipsFrom(string factId)
        {
            return TryGetList(relationshipsBySourceFactId, factId, EmptyRelationships);
        }

        public IReadOnlyList<FactRelationshipData> GetRelationshipsTo(string factId)
        {
            return TryGetList(relationshipsByTargetFactId, factId, EmptyRelationships);
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
