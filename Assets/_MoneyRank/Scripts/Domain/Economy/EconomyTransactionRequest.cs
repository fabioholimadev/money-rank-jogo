using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyTransactionRequest
    {
        private readonly ReadOnlyCollection<EconomyTransactionEntry> _entries;

        public EconomyTransactionRequest(
            string id,
            IEnumerable<EconomyTransactionEntry> entries,
            string sourceId,
            DateTimeOffset createdAt,
            string operatorId,
            string description)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Economy transaction ID cannot be empty.", nameof(id));
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Economy transaction source ID cannot be empty.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Economy transaction operator ID cannot be empty.", nameof(operatorId));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Economy transaction description cannot be empty.", nameof(description));

            var entryList = entries.ToList();
            if (entryList.Count < 2)
                throw new ArgumentException("A balanced economy transaction requires at least two entries.", nameof(entries));
            if (entryList.Any(entry => entry == null))
                throw new ArgumentException("Economy transaction entries cannot contain null values.", nameof(entries));
            if (entryList.Sum(entry => entry.Amount) != 0m)
                throw new ArgumentException("Economy transaction entries must sum to zero.", nameof(entries));

            Id = id;
            SourceId = sourceId;
            CreatedAt = createdAt;
            OperatorId = operatorId;
            Description = description;
            _entries = entryList.AsReadOnly();
        }

        public string Id { get; }
        public string SourceId { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
        public string Description { get; }
        public IReadOnlyList<EconomyTransactionEntry> Entries => _entries;
    }
}
