using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventSelectionContext
    {
        private readonly ReadOnlyCollection<DefinitionParameter> _parameters;

        public EventSelectionContext(
            SessionId sessionId,
            PlayerId playerId,
            string spaceId,
            int drawIndex,
            IEnumerable<DefinitionParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(spaceId))
                throw new ArgumentException("Event selection space ID cannot be empty.", nameof(spaceId));
            if (drawIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(drawIndex));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var values = parameters.ToList();
            if (values.Any(value => value == null))
                throw new ArgumentException("Event selection parameters cannot contain null entries.", nameof(parameters));
            if (values.Select(value => value.Key).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Event selection parameter keys must be unique.", nameof(parameters));

            SessionId = sessionId;
            PlayerId = playerId;
            SpaceId = spaceId;
            DrawIndex = drawIndex;
            _parameters = values.AsReadOnly();
        }

        public SessionId SessionId { get; }
        public PlayerId PlayerId { get; }
        public string SpaceId { get; }
        public int DrawIndex { get; }
        public IReadOnlyList<DefinitionParameter> Parameters => _parameters;
    }

    public interface IEventSelectionPolicy
    {
        string PolicyId { get; }
        OperationResult<EventDefinitionKey> Select(
            EventTableDefinition table,
            EventSelectionContext context);
    }

    public sealed class EventSelectionResult
    {
        public EventSelectionResult(
            EventDefinition definition,
            string tableId,
            string policyId,
            int drawIndex)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            TableId = string.IsNullOrWhiteSpace(tableId)
                ? throw new ArgumentException("Event table ID cannot be empty.", nameof(tableId))
                : tableId;
            PolicyId = string.IsNullOrWhiteSpace(policyId)
                ? throw new ArgumentException("Event selection policy ID cannot be empty.", nameof(policyId))
                : policyId;
            if (drawIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(drawIndex));
            DrawIndex = drawIndex;
        }

        public EventDefinition Definition { get; }
        public string TableId { get; }
        public string PolicyId { get; }
        public int DrawIndex { get; }
    }

    public sealed class EventSelectionService
    {
        private readonly EventContentCatalog _catalog;
        private readonly Dictionary<string, IEventSelectionPolicy> _policies =
            new Dictionary<string, IEventSelectionPolicy>(StringComparer.Ordinal);

        public EventSelectionService(
            EventContentCatalog catalog,
            IEnumerable<IEventSelectionPolicy> policies)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (policies == null)
                throw new ArgumentNullException(nameof(policies));

            foreach (var policy in policies)
            {
                if (policy == null)
                    throw new ArgumentException("Event selection policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId))
                    throw new ArgumentException("Event selection policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId))
                    throw new ArgumentException($"Event selection policy '{policy.PolicyId}' is registered more than once.", nameof(policies));
                _policies.Add(policy.PolicyId, policy);
            }
        }

        public OperationResult<EventSelectionResult> Select(
            string tableId,
            string policyId,
            EventSelectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(policyId))
                throw new ArgumentException("Event selection policy ID cannot be empty.", nameof(policyId));

            var table = _catalog.GetTable(tableId);
            if (table == null)
            {
                return OperationResult<EventSelectionResult>.Failure(
                    "EVENT_TABLE_NOT_FOUND",
                    $"Event table '{tableId}' was not found.");
            }
            if (!_policies.TryGetValue(policyId, out var policy))
            {
                return OperationResult<EventSelectionResult>.Failure(
                    "EVENT_SELECTION_POLICY_NOT_FOUND",
                    $"Event selection policy '{policyId}' was not found.");
            }

            var selected = policy.Select(table, context);
            if (!selected.Succeeded)
                return OperationResult<EventSelectionResult>.Failure(selected.ErrorCode, selected.Message);
            if (!table.Events.Contains(selected.Value))
            {
                return OperationResult<EventSelectionResult>.Failure(
                    "EVENT_SELECTION_OUTSIDE_TABLE",
                    $"Policy '{policyId}' selected event '{selected.Value}' outside table '{tableId}'.");
            }

            var definition = _catalog.GetEvent(selected.Value);
            if (definition == null)
            {
                return OperationResult<EventSelectionResult>.Failure(
                    "EVENT_SELECTION_DEFINITION_NOT_FOUND",
                    $"Selected event definition '{selected.Value}' was not found.");
            }

            return OperationResult<EventSelectionResult>.Success(
                new EventSelectionResult(definition, table.Id, policy.PolicyId, context.DrawIndex));
        }
    }
}
