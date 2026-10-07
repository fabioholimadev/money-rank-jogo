using System;
using System.Collections.Generic;
using MoneyRank.Domain;

namespace MoneyRank.Domain.Events
{
    public sealed class EffectExecutor
    {
        private readonly Dictionary<string, IEffectHandler> _handlers =
            new Dictionary<string, IEffectHandler>(StringComparer.Ordinal);

        public EffectExecutor(IEnumerable<IEffectHandler> handlers)
        {
            if (handlers == null)
                throw new ArgumentNullException(nameof(handlers));

            foreach (var handler in handlers)
            {
                if (handler == null)
                    throw new ArgumentException("Effect handlers cannot contain null entries.", nameof(handlers));
                if (string.IsNullOrWhiteSpace(handler.OperationId))
                    throw new ArgumentException("Effect handler operation ID cannot be empty.", nameof(handlers));
                if (_handlers.ContainsKey(handler.OperationId))
                    throw new ArgumentException($"Effect handler '{handler.OperationId}' is registered more than once.", nameof(handlers));

                _handlers.Add(handler.OperationId, handler);
            }
        }

        public EffectApplicationResult Execute(GameEffectDefinition effect, EventState state)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!_handlers.TryGetValue(effect.OperationId, out var handler))
            {
                return EffectApplicationResult.Failure(
                    "EFFECT_HANDLER_NOT_FOUND",
                    $"No handler is registered for effect operation '{effect.OperationId}'.");
            }

            var result = handler.Apply(effect, state);
            if (result == null)
                throw new InvalidOperationException($"Effect handler '{handler.OperationId}' returned no result.");

            return result;
        }

        public OperationResult<EventStateSnapshot> Preview(
            IEnumerable<GameEffectDefinition> effects,
            EventStateSnapshot initialState)
        {
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));
            if (initialState == null)
                throw new ArgumentNullException(nameof(initialState));

            var workingState = EventState.FromSnapshot(initialState);
            foreach (var effect in effects)
            {
                if (effect == null)
                    throw new ArgumentException("Effects cannot contain null entries.", nameof(effects));

                var result = Execute(effect, workingState);
                if (!result.Succeeded)
                {
                    return OperationResult<EventStateSnapshot>.Failure(
                        result.ErrorCode,
                        result.Message);
                }
            }

            return OperationResult<EventStateSnapshot>.Success(workingState.CreateSnapshot());
        }
    }
}
