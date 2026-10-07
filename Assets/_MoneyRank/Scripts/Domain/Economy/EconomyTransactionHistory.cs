using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyTransactionHistory
    {
        private readonly List<EconomyTransactionRecord> _records = new List<EconomyTransactionRecord>();
        private readonly ReadOnlyCollection<EconomyTransactionRecord> _readOnlyRecords;

        public EconomyTransactionHistory()
        {
            _readOnlyRecords = _records.AsReadOnly();
        }

        public IReadOnlyList<EconomyTransactionRecord> Records => _readOnlyRecords;

        public bool Contains(string transactionId) =>
            !string.IsNullOrWhiteSpace(transactionId) &&
            _records.Any(record => string.Equals(record.Id, transactionId, StringComparison.Ordinal));

        public OperationResult Append(EconomyTransactionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (Contains(record.Id))
            {
                return OperationResult.Failure(
                    "DUPLICATE_ECONOMY_TRANSACTION_ID",
                    $"Economy transaction '{record.Id}' already exists.");
            }

            _records.Add(record);
            return OperationResult.Success();
        }

        public void Restore(IEnumerable<EconomyTransactionRecord> records)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            var replacement = records.ToList();
            if (replacement.Any(record => record == null))
                throw new ArgumentException("Economy history cannot contain null records.", nameof(records));
            if (replacement.Select(record => record.Id).Distinct(StringComparer.Ordinal).Count() != replacement.Count)
                throw new ArgumentException("Economy history transaction IDs must be unique.", nameof(records));

            _records.Clear();
            _records.AddRange(replacement);
        }
    }
}
