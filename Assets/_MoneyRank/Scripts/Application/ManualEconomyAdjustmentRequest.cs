using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Economy;

namespace MoneyRank.Application
{
    public sealed class ManualEconomyAdjustmentRequest
    {
        private readonly ReadOnlyCollection<EconomyTransactionEntry> _entries;

        public ManualEconomyAdjustmentRequest(
            string id,
            IEnumerable<EconomyTransactionEntry> entries,
            string reason,
            DateTimeOffset createdAt,
            string operatorId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Manual adjustment ID cannot be empty.", nameof(id));
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Manual adjustment reason cannot be empty.", nameof(reason));
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Manual adjustment operator ID cannot be empty.", nameof(operatorId));

            var values = entries.ToList();
            if (values.Any(entry => entry == null))
                throw new ArgumentException("Manual adjustment entries cannot contain null values.", nameof(entries));

            // Reuse the domain transaction invariant without retaining a duplicate object.
            new EconomyTransactionRequest(
                id,
                values,
                AdministrativeEconomyAdjustmentService.OperationId,
                createdAt,
                operatorId,
                reason);

            Id = id;
            Reason = reason;
            CreatedAt = createdAt;
            OperatorId = operatorId;
            _entries = values.AsReadOnly();
        }

        public string Id { get; }
        public IReadOnlyList<EconomyTransactionEntry> Entries => _entries;
        public string Reason { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
    }
}
