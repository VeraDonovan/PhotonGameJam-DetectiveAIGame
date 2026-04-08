using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class FactDatabaseBuilder
    {
        public static FactDatabase Build(FactGraphData graphData)
        {
            if (graphData == null)
            {
                throw new ArgumentNullException(nameof(graphData));
            }

            var factById = new Dictionary<string, FactData>(StringComparer.Ordinal);
            var requirementsAllByFactId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var requirementsAnyByFactId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var factsByNpcId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var factsByLocationId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var relationshipsBySourceFactId = new Dictionary<string, List<FactRelationshipData>>(StringComparer.Ordinal);
            var relationshipsByTargetFactId = new Dictionary<string, List<FactRelationshipData>>(StringComparer.Ordinal);

            foreach (var fact in graphData.facts ?? new List<FactData>())
            {
                if (fact == null || string.IsNullOrWhiteSpace(fact.factId))
                {
                    throw new InvalidOperationException("Fact entry is missing a factId.");
                }

                if (!factById.TryAdd(fact.factId, fact))
                {
                    throw new InvalidOperationException($"Duplicate fact id '{fact.factId}'.");
                }

                requirementsAllByFactId[fact.factId] =
                    new List<string>(fact.unlock?.requirementsAll ?? new List<string>());
                requirementsAnyByFactId[fact.factId] =
                    new List<string>(fact.unlock?.requirementsAny ?? new List<string>());

                foreach (var npcId in fact.scope?.relatedNpcIds ?? new List<string>())
                {
                    if (!string.IsNullOrWhiteSpace(npcId))
                    {
                        AddValue(factsByNpcId, npcId, fact.factId);
                    }
                }

                foreach (var locationId in fact.scope?.relatedLocationIds ?? new List<string>())
                {
                    if (!string.IsNullOrWhiteSpace(locationId))
                    {
                        AddValue(factsByLocationId, locationId, fact.factId);
                    }
                }
            }

            foreach (var relationship in graphData.factRelationships ?? new List<FactRelationshipData>())
            {
                if (relationship == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(relationship.fromFactId) ||
                    string.IsNullOrWhiteSpace(relationship.toFactId))
                {
                    throw new InvalidOperationException("Fact relationship is missing fromFactId or toFactId.");
                }

                AddValue(relationshipsBySourceFactId, relationship.fromFactId, relationship);
                AddValue(relationshipsByTargetFactId, relationship.toFactId, relationship);
            }

            return new FactDatabase(
                factById,
                requirementsAllByFactId,
                requirementsAnyByFactId,
                factsByNpcId,
                factsByLocationId,
                relationshipsBySourceFactId,
                relationshipsByTargetFactId);
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
