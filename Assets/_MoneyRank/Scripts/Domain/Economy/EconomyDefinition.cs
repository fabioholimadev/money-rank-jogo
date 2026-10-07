using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyDefinition
    {
        private readonly ReadOnlyCollection<EconomyAccountDefinition> _accounts;
        private readonly ReadOnlyCollection<EconomyBalanceDefinition> _initialBalances;

        public EconomyDefinition(
            IEnumerable<EconomyAccountDefinition> accounts,
            IEnumerable<EconomyBalanceDefinition> initialBalances)
        {
            if (accounts == null)
                throw new ArgumentNullException(nameof(accounts));
            if (initialBalances == null)
                throw new ArgumentNullException(nameof(initialBalances));

            var accountList = accounts.ToList();
            var balanceList = initialBalances.ToList();
            if (accountList.Any(account => account == null))
                throw new ArgumentException("Economy accounts cannot contain null entries.", nameof(accounts));
            if (accountList.Select(account => account.Id).Distinct(StringComparer.Ordinal).Count() != accountList.Count)
                throw new ArgumentException("Economy account IDs must be unique.", nameof(accounts));
            if (balanceList.Any(balance => balance == null))
                throw new ArgumentException("Initial balances cannot contain null entries.", nameof(initialBalances));

            // EconomyState is also the single validator for account references and limits.
            new EconomyState(accountList, balanceList);
            _accounts = accountList.AsReadOnly();
            _initialBalances = balanceList.AsReadOnly();
        }

        public IReadOnlyList<EconomyAccountDefinition> Accounts => _accounts;
        public IReadOnlyList<EconomyBalanceDefinition> InitialBalances => _initialBalances;

        public EconomyState CreateState() => new EconomyState(_accounts, _initialBalances);
        public EconomyTransactionService CreateTransactionService() =>
            new EconomyTransactionService(_accounts);
    }
}
