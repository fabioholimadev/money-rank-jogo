using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomicResourceService
    {
        private readonly Dictionary<string, IEconomicResourceTransactionPolicy> _policies =
            new Dictionary<string, IEconomicResourceTransactionPolicy>(StringComparer.Ordinal);
        private readonly EconomyDefinition _economyDefinition;
        private readonly EconomyTransactionService _transactionService;

        public EconomicResourceService(
            EconomyDefinition economyDefinition,
            IEnumerable<IEconomicResourceTransactionPolicy> policies)
        {
            _economyDefinition = economyDefinition ?? throw new ArgumentNullException(nameof(economyDefinition));
            if (policies == null) throw new ArgumentNullException(nameof(policies));
            foreach (var policy in policies)
            {
                if (policy == null) throw new ArgumentException("Resource policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId)) throw new ArgumentException("Resource policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId)) throw new ArgumentException($"Resource policy '{policy.PolicyId}' is registered more than once.", nameof(policies));
                _policies.Add(policy.PolicyId, policy);
            }
            _transactionService = economyDefinition.CreateTransactionService();
        }

        public OperationResult<EconomicResourceTransactionResult> Execute(
            EconomicResourceDefinition definition,
            EconomyState state,
            EconomyTransactionHistory history,
            EconomicResourceCommand command)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (history == null) throw new ArgumentNullException(nameof(history));
            if (command == null) throw new ArgumentNullException(nameof(command));

            var account = _economyDefinition.Accounts.FirstOrDefault(item =>
                string.Equals(item.Id, command.TargetAccountId, StringComparison.Ordinal));
            if (account == null ||
                !string.Equals(account.OwnerId, command.PlayerId.Value, StringComparison.Ordinal) ||
                !string.Equals(account.ResourceTypeId, definition.Id, StringComparison.Ordinal))
            {
                return OperationResult<EconomicResourceTransactionResult>.Failure(
                    "RESOURCE_TARGET_ACCOUNT_INVALID",
                    "The target account does not belong to this player and resource definition.");
            }
            if (!_policies.TryGetValue(definition.TransactionPolicyId, out var policy))
            {
                return OperationResult<EconomicResourceTransactionResult>.Failure(
                    "RESOURCE_POLICY_NOT_FOUND",
                    $"No resource transaction policy is registered for '{definition.TransactionPolicyId}'.");
            }

            var before = state.CreateSnapshot();
            var ownedBefore = GetOwnedQuantity(before, command.PlayerId, definition.Id);
            var ownedAfter = ownedBefore + command.QuantityDelta;
            if (ownedAfter < 0m ||
                (definition.MaximumPerOwner.HasValue && ownedAfter > definition.MaximumPerOwner.Value))
            {
                return OperationResult<EconomicResourceTransactionResult>.Failure(
                    "RESOURCE_OWNER_LIMIT_EXCEEDED",
                    "The resource change would violate the configured owner limits.");
            }

            var planned = policy.CreateTransaction(definition, command, before);
            if (!planned.Succeeded)
                return OperationResult<EconomicResourceTransactionResult>.Failure(planned.ErrorCode, planned.Message);
            if (planned.Value == null || !string.Equals(planned.Value.Id, command.Id, StringComparison.Ordinal))
                return OperationResult<EconomicResourceTransactionResult>.Failure("RESOURCE_TRANSACTION_INVALID", "The resource policy produced an invalid transaction.");
            if (!string.Equals(planned.Value.SourceId, definition.Id, StringComparison.Ordinal) ||
                planned.Value.CreatedAt != command.CreatedAt ||
                !string.Equals(planned.Value.OperatorId, command.OperatorId, StringComparison.Ordinal) ||
                !string.Equals(planned.Value.Description, command.Reason, StringComparison.Ordinal))
            {
                return OperationResult<EconomicResourceTransactionResult>.Failure(
                    "RESOURCE_TRANSACTION_METADATA_MISMATCH",
                    "The resource policy changed the command audit metadata.");
            }

            var targetDelta = planned.Value.Entries
                .Where(entry => string.Equals(entry.AccountId, command.TargetAccountId, StringComparison.Ordinal))
                .Sum(entry => entry.Amount);
            if (targetDelta != command.QuantityDelta)
            {
                return OperationResult<EconomicResourceTransactionResult>.Failure(
                    "RESOURCE_QUANTITY_MISMATCH",
                    "The resource transaction does not match the requested quantity change.");
            }

            var applied = _transactionService.Apply(state, history, planned.Value);
            if (!applied.Succeeded)
                return OperationResult<EconomicResourceTransactionResult>.Failure(applied.ErrorCode, applied.Message);

            return OperationResult<EconomicResourceTransactionResult>.Success(
                new EconomicResourceTransactionResult(
                    definition.Id,
                    command.PlayerId,
                    ownedBefore,
                    ownedAfter,
                    applied.Value));
        }

        private decimal GetOwnedQuantity(EconomySnapshot snapshot, PlayerId playerId, string resourceId) =>
            _economyDefinition.Accounts
                .Where(account =>
                    string.Equals(account.OwnerId, playerId.Value, StringComparison.Ordinal) &&
                    string.Equals(account.ResourceTypeId, resourceId, StringComparison.Ordinal))
                .Sum(account => snapshot.Balances[account.Id]);
    }
}
