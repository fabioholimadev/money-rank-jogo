using System;
using System.Collections.Generic;

namespace MoneyRank.Domain.Events
{
    public sealed class RequirementEvaluator
    {
        private readonly Dictionary<string, IRequirementHandler> _handlers =
            new Dictionary<string, IRequirementHandler>(StringComparer.Ordinal);

        public RequirementEvaluator(IEnumerable<IRequirementHandler> handlers)
        {
            if (handlers == null)
                throw new ArgumentNullException(nameof(handlers));

            foreach (var handler in handlers)
            {
                if (handler == null)
                    throw new ArgumentException("Requirement handlers cannot contain null entries.", nameof(handlers));
                if (string.IsNullOrWhiteSpace(handler.TypeId))
                    throw new ArgumentException("Requirement handler type ID cannot be empty.", nameof(handlers));
                if (_handlers.ContainsKey(handler.TypeId))
                    throw new ArgumentException($"Requirement handler '{handler.TypeId}' is registered more than once.", nameof(handlers));

                _handlers.Add(handler.TypeId, handler);
            }
        }

        public RequirementEvaluation Evaluate(
            RequirementDefinition requirement,
            RequirementEvaluationContext context)
        {
            if (requirement == null)
                throw new ArgumentNullException(nameof(requirement));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!_handlers.TryGetValue(requirement.TypeId, out var handler))
            {
                return new RequirementEvaluation(
                    requirement,
                    RequirementCheckResult.Fail(
                        "REQUIREMENT_HANDLER_NOT_FOUND",
                        $"No handler is registered for requirement type '{requirement.TypeId}'."));
            }

            var result = handler.Evaluate(requirement, context);
            if (result == null)
                throw new InvalidOperationException($"Requirement handler '{handler.TypeId}' returned no result.");

            return new RequirementEvaluation(requirement, result);
        }
    }

    public sealed class RequirementEvaluation
    {
        public RequirementEvaluation(
            RequirementDefinition requirement,
            RequirementCheckResult result)
        {
            Requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public RequirementDefinition Requirement { get; }
        public RequirementCheckResult Result { get; }
        public bool IsSatisfied => Result.IsSatisfied;
    }
}
