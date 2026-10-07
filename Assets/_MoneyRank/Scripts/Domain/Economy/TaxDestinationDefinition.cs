using System;

namespace MoneyRank.Domain.Economy
{
    public sealed class TaxDestinationDefinition
    {
        public TaxDestinationDefinition(string accountId, decimal share)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Tax destination account ID cannot be empty.", nameof(accountId));
            if (share <= 0m || share > 1m)
                throw new ArgumentOutOfRangeException(nameof(share), "Tax destination share must be greater than zero and at most one.");

            AccountId = accountId;
            Share = share;
        }

        public string AccountId { get; }
        public decimal Share { get; }
    }
}
