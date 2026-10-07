using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventResolutionHistory
    {
        private readonly List<EventResolutionRecord> _records = new List<EventResolutionRecord>();
        private readonly ReadOnlyCollection<EventResolutionRecord> _readOnlyRecords;

        public EventResolutionHistory()
        {
            _readOnlyRecords = _records.AsReadOnly();
        }

        public IReadOnlyList<EventResolutionRecord> Records => _readOnlyRecords;

        public bool Contains(string resolutionId) =>
            _records.Any(record => string.Equals(record.Id, resolutionId, StringComparison.Ordinal));

        public OperationResult Append(EventResolutionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (Contains(record.Id))
            {
                return OperationResult.Failure(
                    "DUPLICATE_RESOLUTION_ID",
                    $"Resolution '{record.Id}' already exists in history.");
            }

            _records.Add(record);
            return OperationResult.Success();
        }

        public void Restore(IEnumerable<EventResolutionRecord> records)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            var replacement = records.ToList();
            if (replacement.Any(record => record == null))
                throw new ArgumentException("Event history cannot contain null records.", nameof(records));
            if (replacement.Select(record => record.Id).Distinct(StringComparer.Ordinal).Count() != replacement.Count)
                throw new ArgumentException("Event history resolution IDs must be unique.", nameof(records));

            _records.Clear();
            _records.AddRange(replacement);
        }
    }
}
