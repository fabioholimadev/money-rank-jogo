using System;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyBalanceDefinition
    {
        public EconomyBalanceDefinition(string accountId, decimal balance)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Economy account ID cannot be empty.", nameof(accountId));

            AccountId = accountId;
            Balance = balance;
        }

        public string AccountId { get; }
        public decimal Balance { get; }
    }
}
