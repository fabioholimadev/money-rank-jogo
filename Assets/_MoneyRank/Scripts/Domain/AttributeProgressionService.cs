using System;
using System.Collections.Generic;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain
{
    public sealed class AttributeProgressionService
    {
        private readonly Dictionary<string, IAttributeOverflowHandler> _handlers =
            new Dictionary<string, IAttributeOverflowHandler>(StringComparer.Ordinal);

        public AttributeProgressionService(IEnumerable<IAttributeOverflowHandler> handlers)
        {
            if (handlers == null)
                throw new ArgumentNullException(nameof(handlers));

            foreach (var handler in handlers)
            {
                if (handler == null)
                    throw new ArgumentException("Attribute overflow handlers cannot contain null entries.", nameof(handlers));
                if (string.IsNullOrWhiteSpace(handler.BehaviorId))
                    throw new ArgumentException("Attribute overflow behavior ID cannot be empty.", nameof(handlers));
                if (_handlers.ContainsKey(handler.BehaviorId))
                    throw new ArgumentException(
                        $"Attribute overflow handler '{handler.BehaviorId}' is registered more than once.",
                        nameof(handlers));

                _handlers.Add(handler.BehaviorId, handler);
            }
        }

        public OperationResult<AttributeProgressionResult> Apply(
            AttributeDefinition definition,
            AttributeState state,
            int delta)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!string.Equals(definition.Id, state.AttributeId, StringComparison.Ordinal))
            {
                return OperationResult<AttributeProgressionResult>.Failure(
                    "ATTRIBUTE_STATE_MISMATCH",
                    $"State '{state.AttributeId}' does not belong to attribute '{definition.Id}'.");
            }
            if (!_handlers.TryGetValue(definition.OverflowBehaviorId, out var handler))
            {
                return OperationResult<AttributeProgressionResult>.Failure(
                    "ATTRIBUTE_OVERFLOW_HANDLER_NOT_FOUND",
                    $"No handler is registered for overflow behavior '{definition.OverflowBehaviorId}'.");
            }

            var beforeProgress = state.CurrentProgress;
            var calculation = handler.Calculate(beforeProgress, delta, definition.Threshold);
            if (!calculation.Succeeded)
            {
                return OperationResult<AttributeProgressionResult>.Failure(
                    calculation.ErrorCode,
                    calculation.Message);
            }

            state.Apply(calculation.Value);
            var plannedEffects = new List<GameEffectDefinition>();
            for (var cycle = 0; cycle < calculation.Value.CompletedCycles; cycle++)
                plannedEffects.AddRange(definition.CompletionEffects);

            return OperationResult<AttributeProgressionResult>.Success(
                new AttributeProgressionResult(
                    definition.Id,
                    beforeProgress,
                    state.CurrentProgress,
                    calculation.Value.CompletedCycles,
                    state.CompletedCycles,
                    plannedEffects));
        }
    }
}
