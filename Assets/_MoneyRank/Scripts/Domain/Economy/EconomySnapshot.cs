using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomySnapshot
    {
        private readonly ReadOnlyDictionary<string, decimal> _balances;

        public EconomySnapshot(IEnumerable<EconomyBalanceDefinition> balances)
        {
            if (balances == null)
                throw new ArgumentNullException(nameof(balances));

            var map = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var balance in balances)
            {
                if (balance == null)
                    throw new ArgumentException("Economy balances cannot contain null entries.", nameof(balances));
                if (map.ContainsKey(balance.AccountId))
                    throw new ArgumentException("Economy balance account IDs must be unique.", nameof(balances));
                map.Add(balance.AccountId, balance.Balance);
            }

            _balances = new ReadOnlyDictionary<string, decimal>(map);
        }

        public IReadOnlyDictionary<string, decimal> Balances => _balances;
    }
}
