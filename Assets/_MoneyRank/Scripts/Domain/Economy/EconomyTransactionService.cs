using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyTransactionService
    {
        private readonly IReadOnlyList<EconomyAccountDefinition> _accounts;
        private readonly Dictionary<string, EconomyAccountDefinition> _accountsById;

        public EconomyTransactionService(IEnumerable<EconomyAccountDefinition> accounts)
        {
            if (accounts == null)
                throw new ArgumentNullException(nameof(accounts));

            var accountList = accounts.ToList();
            if (accountList.Any(account => account == null))
                throw new ArgumentException("Economy accounts cannot contain null entries.", nameof(accounts));
            if (accountList.Select(account => account.Id).Distinct(StringComparer.Ordinal).Count() != accountList.Count)
                throw new ArgumentException("Economy account IDs must be unique.", nameof(accounts));
            _accounts = accountList.AsReadOnly();
            _accountsById = accountList.ToDictionary(account => account.Id, StringComparer.Ordinal);
        }

        public OperationResult<EconomyTransactionRecord> Apply(
            EconomyState state,
            EconomyTransactionHistory history,
            EconomyTransactionRequest request)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (history == null)
                throw new ArgumentNullException(nameof(history));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (history.Contains(request.Id))
            {
                return OperationResult<EconomyTransactionRecord>.Failure(
                    "DUPLICATE_ECONOMY_TRANSACTION_ID",
                    $"Economy transaction '{request.Id}' already exists.");
            }

            foreach (var entry in request.Entries)
            {
                if (!_accountsById.ContainsKey(entry.AccountId))
                {
                    return OperationResult<EconomyTransactionRecord>.Failure(
                        "ECONOMY_ACCOUNT_NOT_FOUND",
                        $"Account '{entry.AccountId}' was not found.");
                }
            }
            var unbalancedUnit = request.Entries
                .GroupBy(entry => _accountsById[entry.AccountId].UnitId, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Sum(entry => entry.Amount) != 0m);
            if (unbalancedUnit != null)
            {
                return OperationResult<EconomyTransactionRecord>.Failure(
                    "ECONOMY_UNIT_NOT_BALANCED",
                    $"Economy entries for unit '{unbalancedUnit.Key}' must sum to zero.");
            }

            var beforeState = state.CreateSnapshot();
            var workingState = EconomyState.FromSnapshot(_accounts, beforeState);
            foreach (var entry in request.Entries)
            {
                var result = workingState.ApplyDelta(entry.AccountId, entry.Amount);
                if (!result.Succeeded)
                {
                    return OperationResult<EconomyTransactionRecord>.Failure(
                        result.ErrorCode,
                        result.Message);
                }
            }

            var afterState = workingState.CreateSnapshot();
            var record = new EconomyTransactionRecord(request, beforeState, afterState);
            var historyResult = history.Append(record);
            if (!historyResult.Succeeded)
            {
                return OperationResult<EconomyTransactionRecord>.Failure(
                    historyResult.ErrorCode,
                    historyResult.Message);
            }

            state.Restore(afterState);
            return OperationResult<EconomyTransactionRecord>.Success(record);
        }
    }
}
