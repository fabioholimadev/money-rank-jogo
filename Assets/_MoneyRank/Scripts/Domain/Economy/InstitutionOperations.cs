using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Economy
{
    public sealed class InstitutionOperationDefinition
    {
        public InstitutionOperationDefinition(
            string id,
            string displayName,
            string categoryId,
            string institutionOwnerId,
            string policyId)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Institution operation ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Institution operation display name cannot be empty.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(categoryId)) throw new ArgumentException("Institution operation category ID cannot be empty.", nameof(categoryId));
            if (string.IsNullOrWhiteSpace(institutionOwnerId)) throw new ArgumentException("Institution owner ID cannot be empty.", nameof(institutionOwnerId));
            if (string.IsNullOrWhiteSpace(policyId)) throw new ArgumentException("Institution operation policy ID cannot be empty.", nameof(policyId));

            Id = id;
            DisplayName = displayName;
            CategoryId = categoryId;
            InstitutionOwnerId = institutionOwnerId;
            PolicyId = policyId;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string CategoryId { get; }
        public string InstitutionOwnerId { get; }
        public string PolicyId { get; }
    }

    public sealed class InstitutionOperationCommand
    {
        private readonly ReadOnlyCollection<DefinitionParameter> _parameters;

        public InstitutionOperationCommand(
            string id,
            PlayerId playerId,
            IEnumerable<DefinitionParameter> parameters,
            DateTimeOffset createdAt,
            string operatorId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Institution operation command ID cannot be empty.", nameof(id));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (string.IsNullOrWhiteSpace(operatorId)) throw new ArgumentException("Institution operation operator ID cannot be empty.", nameof(operatorId));
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Institution operation reason cannot be empty.", nameof(reason));

            var values = parameters.ToList();
            if (values.Any(value => value == null)) throw new ArgumentException("Institution operation parameters cannot contain null entries.", nameof(parameters));
            if (values.Select(value => value.Key).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Institution operation parameter keys must be unique.", nameof(parameters));

            Id = id;
            PlayerId = playerId;
            CreatedAt = createdAt;
            OperatorId = operatorId;
            Reason = reason;
            _parameters = values.AsReadOnly();
        }

        public string Id { get; }
        public PlayerId PlayerId { get; }
        public IReadOnlyList<DefinitionParameter> Parameters => _parameters;
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
        public string Reason { get; }
    }

    public interface IInstitutionOperationPolicy
    {
        string PolicyId { get; }
        OperationResult<EconomyTransactionRequest> CreateTransaction(
            InstitutionOperationDefinition definition,
            InstitutionOperationCommand command,
            EconomySnapshot economy);
    }

    public sealed class InstitutionOperationResult
    {
        public InstitutionOperationResult(
            string definitionId,
            PlayerId playerId,
            EconomyTransactionRecord transaction)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId)
                ? throw new ArgumentException("Institution operation definition ID cannot be empty.", nameof(definitionId))
                : definitionId;
            PlayerId = playerId;
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public string DefinitionId { get; }
        public PlayerId PlayerId { get; }
        public EconomyTransactionRecord Transaction { get; }
    }

    public sealed class InstitutionOperationService
    {
        private readonly Dictionary<string, EconomyAccountDefinition> _accounts;
        private readonly Dictionary<string, IInstitutionOperationPolicy> _policies =
            new Dictionary<string, IInstitutionOperationPolicy>(StringComparer.Ordinal);
        private readonly EconomyTransactionService _transactionService;

        public InstitutionOperationService(
            EconomyDefinition economyDefinition,
            IEnumerable<IInstitutionOperationPolicy> policies)
        {
            if (economyDefinition == null) throw new ArgumentNullException(nameof(economyDefinition));
            if (policies == null) throw new ArgumentNullException(nameof(policies));

            _accounts = economyDefinition.Accounts.ToDictionary(account => account.Id, StringComparer.Ordinal);
            _transactionService = economyDefinition.CreateTransactionService();
            foreach (var policy in policies)
            {
                if (policy == null) throw new ArgumentException("Institution operation policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId)) throw new ArgumentException("Institution operation policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId)) throw new ArgumentException($"Institution operation policy '{policy.PolicyId}' is registered more than once.", nameof(policies));
                _policies.Add(policy.PolicyId, policy);
            }
        }

        public OperationResult<InstitutionOperationResult> Execute(
            InstitutionOperationDefinition definition,
            EconomyState state,
            EconomyTransactionHistory history,
            InstitutionOperationCommand command)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (history == null) throw new ArgumentNullException(nameof(history));
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_policies.TryGetValue(definition.PolicyId, out var policy))
            {
                return OperationResult<InstitutionOperationResult>.Failure(
                    "INSTITUTION_POLICY_NOT_FOUND",
                    $"No institution operation policy is registered for '{definition.PolicyId}'.");
            }

            var planned = policy.CreateTransaction(definition, command, state.CreateSnapshot());
            if (!planned.Succeeded)
                return OperationResult<InstitutionOperationResult>.Failure(planned.ErrorCode, planned.Message);
            if (planned.Value == null || !string.Equals(planned.Value.Id, command.Id, StringComparison.Ordinal))
            {
                return OperationResult<InstitutionOperationResult>.Failure(
                    "INSTITUTION_TRANSACTION_INVALID",
                    "The institution policy produced an invalid transaction.");
            }
            if (!string.Equals(planned.Value.SourceId, definition.Id, StringComparison.Ordinal) ||
                planned.Value.CreatedAt != command.CreatedAt ||
                !string.Equals(planned.Value.OperatorId, command.OperatorId, StringComparison.Ordinal) ||
                !string.Equals(planned.Value.Description, command.Reason, StringComparison.Ordinal))
            {
                return OperationResult<InstitutionOperationResult>.Failure(
                    "INSTITUTION_TRANSACTION_METADATA_MISMATCH",
                    "The institution policy changed the command audit metadata.");
            }

            foreach (var entry in planned.Value.Entries)
            {
                if (!_accounts.ContainsKey(entry.AccountId))
                {
                    return OperationResult<InstitutionOperationResult>.Failure(
                        "ECONOMY_ACCOUNT_NOT_FOUND",
                        $"Account '{entry.AccountId}' was not found.");
                }
            }
            var owners = planned.Value.Entries
                .Select(entry => _accounts[entry.AccountId].OwnerId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (!owners.Contains(definition.InstitutionOwnerId, StringComparer.Ordinal) ||
                !owners.Contains(command.PlayerId.Value, StringComparer.Ordinal))
            {
                return OperationResult<InstitutionOperationResult>.Failure(
                    "INSTITUTION_TRANSACTION_PARTICIPANTS_INVALID",
                    "The transaction must include both the configured institution and requesting player.");
            }

            var applied = _transactionService.Apply(state, history, planned.Value);
            if (!applied.Succeeded)
                return OperationResult<InstitutionOperationResult>.Failure(applied.ErrorCode, applied.Message);

            return OperationResult<InstitutionOperationResult>.Success(
                new InstitutionOperationResult(definition.Id, command.PlayerId, applied.Value));
        }
    }
}
