using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class TaxCalculationResult
    {
        private readonly ReadOnlyCollection<EconomyTransactionEntry> _entries;

        public TaxCalculationResult(
            string ruleId,
            decimal taxableAmount,
            decimal taxAmount,
            IEnumerable<EconomyTransactionEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                throw new ArgumentException("Tax rule ID cannot be empty.", nameof(ruleId));
            if (taxableAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(taxableAmount));
            if (taxAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(taxAmount));
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            RuleId = ruleId;
            TaxableAmount = taxableAmount;
            TaxAmount = taxAmount;
            _entries = entries.ToList().AsReadOnly();
        }

        public string RuleId { get; }
        public decimal TaxableAmount { get; }
        public decimal TaxAmount { get; }
        public IReadOnlyList<EconomyTransactionEntry> Entries => _entries;
    }
}
