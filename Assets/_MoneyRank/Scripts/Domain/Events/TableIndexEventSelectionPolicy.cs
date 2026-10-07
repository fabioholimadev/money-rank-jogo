using System;

namespace MoneyRank.Domain.Events
{
    public sealed class TableIndexEventSelectionPolicy : IEventSelectionPolicy
    {
        public TableIndexEventSelectionPolicy(string policyId)
        {
            PolicyId = string.IsNullOrWhiteSpace(policyId)
                ? throw new ArgumentException("Event selection policy ID cannot be empty.", nameof(policyId))
                : policyId;
        }

        public string PolicyId { get; }

        public OperationResult<EventDefinitionKey> Select(
            EventTableDefinition table,
            EventSelectionContext context)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return OperationResult<EventDefinitionKey>.Success(
                table.Events[context.DrawIndex % table.Events.Count]);
        }
    }
}
