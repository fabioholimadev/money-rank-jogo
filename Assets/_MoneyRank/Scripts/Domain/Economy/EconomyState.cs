using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyState
    {
        private readonly Dictionary<string, EconomyAccountDefinition> _accounts;
        private readonly Dictionary<string, decimal> _balances;

        public EconomyState(
            IEnumerable<EconomyAccountDefinition> accounts,
            IEnumerable<EconomyBalanceDefinition> initialBalances)
        {
            if (accounts == null)
                throw new ArgumentNullException(nameof(accounts));
            if (initialBalances == null)
                throw new ArgumentNullException(nameof(initialBalances));

            _accounts = new Dictionary<string, EconomyAccountDefinition>(StringComparer.Ordinal);
            foreach (var account in accounts)
            {
                if (account == null)
                    throw new ArgumentException("Economy accounts cannot contain null entries.", nameof(accounts));
                if (_accounts.ContainsKey(account.Id))
                    throw new ArgumentException($"Duplicate economy account '{account.Id}'.", nameof(accounts));
                _accounts.Add(account.Id, account);
            }

            _balances = _accounts.Keys.ToDictionary(id => id, _ => 0m, StringComparer.Ordinal);
            foreach (var initialBalance in initialBalances)
            {
                if (initialBalance == null)
                    throw new ArgumentException("Initial balances cannot contain null entries.", nameof(initialBalances));
                if (!_accounts.TryGetValue(initialBalance.AccountId, out var account))
                    throw new ArgumentException($"Initial balance references unknown account '{initialBalance.AccountId}'.", nameof(initialBalances));
                if (!account.Accepts(initialBalance.Balance))
                    throw new ArgumentException($"Initial balance violates limits for account '{account.Id}'.", nameof(initialBalances));
                _balances[initialBalance.AccountId] = initialBalance.Balance;
            }
        }

        public bool TryGetBalance(string accountId, out decimal balance) =>
            _balances.TryGetValue(accountId, out balance);

        public EconomySnapshot CreateSnapshot() =>
            new EconomySnapshot(
                _balances.Select(pair => new EconomyBalanceDefinition(pair.Key, pair.Value)));

        public void Restore(EconomySnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Balances.Count != _accounts.Count || snapshot.Balances.Keys.Any(id => !_accounts.ContainsKey(id)))
                throw new ArgumentException("Economy snapshot does not match the configured accounts.", nameof(snapshot));

            foreach (var pair in snapshot.Balances)
            {
                if (!_accounts[pair.Key].Accepts(pair.Value))
                    throw new ArgumentException($"Snapshot balance violates limits for account '{pair.Key}'.", nameof(snapshot));
            }
            foreach (var pair in snapshot.Balances)
                _balances[pair.Key] = pair.Value;
        }

        public OperationResult ApplyDelta(string accountId, decimal delta)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Economy account ID cannot be empty.", nameof(accountId));
            if (!_accounts.TryGetValue(accountId, out var account))
                return OperationResult.Failure("ECONOMY_ACCOUNT_NOT_FOUND", $"Account '{accountId}' was not found.");

            var nextBalance = _balances[accountId] + delta;
            if (!account.Accepts(nextBalance))
            {
                return OperationResult.Failure(
                    "ECONOMY_ACCOUNT_LIMIT_EXCEEDED",
                    $"Transaction would violate limits for account '{accountId}'.");
            }

            _balances[accountId] = nextBalance;
            return OperationResult.Success();
        }

        public static EconomyState FromSnapshot(
            IEnumerable<EconomyAccountDefinition> accounts,
            EconomySnapshot snapshot) =>
            new EconomyState(
                accounts,
                snapshot.Balances.Select(pair => new EconomyBalanceDefinition(pair.Key, pair.Value)));
    }
}
