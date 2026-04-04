using System;
using System.Collections.Generic;

namespace DetectiveGame.Core
{
    public sealed class EndingDatabase
    {
        private static readonly IReadOnlyList<string> EmptyIds = Array.Empty<string>();

        private readonly Dictionary<string, EndingData> endingById;
        private readonly Dictionary<string, List<string>> requiredFactIdsByEndingId;
        private readonly Dictionary<string, List<string>> requiredAnyFactIdsByEndingId;
        private readonly Dictionary<string, List<string>> requiredEvidenceIdsByEndingId;
        private readonly Dictionary<string, List<string>> requiredNpcLayerIdsByEndingId;

        internal EndingDatabase(
            Dictionary<string, EndingData> endingById,
            Dictionary<string, List<string>> requiredFactIdsByEndingId,
            Dictionary<string, List<string>> requiredAnyFactIdsByEndingId,
            Dictionary<string, List<string>> requiredEvidenceIdsByEndingId,
            Dictionary<string, List<string>> requiredNpcLayerIdsByEndingId)
        {
            this.endingById = endingById ?? new Dictionary<string, EndingData>(StringComparer.Ordinal);
            this.requiredFactIdsByEndingId = requiredFactIdsByEndingId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.requiredAnyFactIdsByEndingId = requiredAnyFactIdsByEndingId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.requiredEvidenceIdsByEndingId = requiredEvidenceIdsByEndingId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.requiredNpcLayerIdsByEndingId = requiredNpcLayerIdsByEndingId ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, EndingData> EndingById => endingById;
        public IReadOnlyDictionary<string, List<string>> RequiredFactIdsByEndingId => requiredFactIdsByEndingId;
        public IReadOnlyDictionary<string, List<string>> RequiredAnyFactIdsByEndingId => requiredAnyFactIdsByEndingId;
        public IReadOnlyDictionary<string, List<string>> RequiredEvidenceIdsByEndingId => requiredEvidenceIdsByEndingId;
        public IReadOnlyDictionary<string, List<string>> RequiredNpcLayerIdsByEndingId => requiredNpcLayerIdsByEndingId;

        public bool TryGetEnding(string endingId, out EndingData ending)
        {
            return endingById.TryGetValue(endingId, out ending);
        }

        public IReadOnlyList<string> GetRequiredFactIds(string endingId)
        {
            return TryGetList(requiredFactIdsByEndingId, endingId, EmptyIds);
        }

        public IReadOnlyList<string> GetRequiredAnyFactIds(string endingId)
        {
            return TryGetList(requiredAnyFactIdsByEndingId, endingId, EmptyIds);
        }

        public IReadOnlyList<string> GetRequiredEvidenceIds(string endingId)
        {
            return TryGetList(requiredEvidenceIdsByEndingId, endingId, EmptyIds);
        }

        public IReadOnlyList<string> GetRequiredNpcLayerIds(string endingId)
        {
            return TryGetList(requiredNpcLayerIdsByEndingId, endingId, EmptyIds);
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
