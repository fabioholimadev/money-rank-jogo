using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomicResourceCommand
    {
        private readonly ReadOnlyCollection<DefinitionParameter> _parameters;

        public EconomicResourceCommand(
            string id,
            PlayerId playerId,
            string targetAccountId,
            decimal quantityDelta,
            IEnumerable<DefinitionParameter> parameters,
            DateTimeOffset createdAt,
            string operatorId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Resource command ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(targetAccountId)) throw new ArgumentException("Target account ID cannot be empty.", nameof(targetAccountId));
            if (quantityDelta == 0m) throw new ArgumentOutOfRangeException(nameof(quantityDelta));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (string.IsNullOrWhiteSpace(operatorId)) throw new ArgumentException("Resource operator ID cannot be empty.", nameof(operatorId));
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Resource command reason cannot be empty.", nameof(reason));

            var values = parameters.ToList();
            if (values.Any(value => value == null)) throw new ArgumentException("Resource parameters cannot contain null entries.", nameof(parameters));
            if (values.Select(value => value.Key).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Resource parameter keys must be unique.", nameof(parameters));

            Id = id;
            PlayerId = playerId;
            TargetAccountId = targetAccountId;
            QuantityDelta = quantityDelta;
            CreatedAt = createdAt;
            OperatorId = operatorId;
            Reason = reason;
            _parameters = values.AsReadOnly();
        }

        public string Id { get; }
        public PlayerId PlayerId { get; }
        public string TargetAccountId { get; }
        public decimal QuantityDelta { get; }
        public IReadOnlyList<DefinitionParameter> Parameters => _parameters;
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
        public string Reason { get; }
    }
}
