using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public static class EndingDatabaseBuilder
    {
        public static EndingDatabase Build(EndingSetData endingSetData)
        {
            if (endingSetData == null)
            {
                throw new ArgumentNullException(nameof(endingSetData));
            }

            var endingById = new Dictionary<string, EndingData>(StringComparer.Ordinal);
            var requiredFactIdsByEndingId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var requiredAnyFactIdsByEndingId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var requiredEvidenceIdsByEndingId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var requiredNpcLayerIdsByEndingId = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var ending in endingSetData.endings ?? new List<EndingData>())
            {
                if (ending == null || string.IsNullOrWhiteSpace(ending.endingId))
                {
                    throw new InvalidOperationException("Ending entry is missing an endingId.");
                }

                if (!endingById.TryAdd(ending.endingId, ending))
                {
                    throw new InvalidOperationException($"Duplicate ending id '{ending.endingId}'.");
                }

                requiredFactIdsByEndingId[ending.endingId] =
                    new List<string>(ending.requirements?.requiredFactIds ?? new List<string>());
                requiredAnyFactIdsByEndingId[ending.endingId] =
                    new List<string>(ending.requirements?.requiredAnyFactIds ?? new List<string>());
                requiredEvidenceIdsByEndingId[ending.endingId] =
                    new List<string>(ending.requirements?.requiredEvidenceIds ?? new List<string>());
                requiredNpcLayerIdsByEndingId[ending.endingId] =
                    new List<string>(ending.requirements?.requiredNpcLayerIds ?? new List<string>());
            }

            return new EndingDatabase(
                endingById,
                requiredFactIdsByEndingId,
                requiredAnyFactIdsByEndingId,
                requiredEvidenceIdsByEndingId,
                requiredNpcLayerIdsByEndingId);
        }
    }
}
