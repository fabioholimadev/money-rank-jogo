using System;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyTransactionEntry
    {
        public EconomyTransactionEntry(string accountId, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Economy account ID cannot be empty.", nameof(accountId));
            if (amount == 0m)
                throw new ArgumentOutOfRangeException(nameof(amount), "Economy entry amount cannot be zero.");

            AccountId = accountId;
            Amount = amount;
        }

        public string AccountId { get; }
        public decimal Amount { get; }
    }
}
