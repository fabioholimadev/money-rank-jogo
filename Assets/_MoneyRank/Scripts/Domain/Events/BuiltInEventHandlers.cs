using System;

namespace MoneyRank.Domain.Events
{
    public sealed class StateEqualsRequirementHandler : IRequirementHandler
    {
        public const string HandlerTypeId = "state.equals";
        public string TypeId => HandlerTypeId;

        public RequirementCheckResult Evaluate(
            RequirementDefinition requirement,
            RequirementEvaluationContext context)
        {
            if (requirement == null) throw new ArgumentNullException(nameof(requirement));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!requirement.Parameters.TryGetValue("key", out var key) || string.IsNullOrWhiteSpace(key) ||
                !requirement.Parameters.TryGetValue("value", out var expected))
            {
                return RequirementCheckResult.Fail(
                    "REQUIREMENT_PARAMETER_MISSING",
                    "The state.equals requirement needs non-empty 'key' and present 'value' parameters.");
            }

            return context.StateValues.TryGetValue(key, out var current) &&
                   string.Equals(current, expected, StringComparison.Ordinal)
                ? RequirementCheckResult.Pass()
                : RequirementCheckResult.Fail(
                    "STATE_VALUE_MISMATCH",
                    $"State '{key}' must equal '{expected}'.");
        }
    }

    public sealed class StateExistsRequirementHandler : IRequirementHandler
    {
        public const string HandlerTypeId = "state.exists";
        public string TypeId => HandlerTypeId;

        public RequirementCheckResult Evaluate(
            RequirementDefinition requirement,
            RequirementEvaluationContext context)
        {
            if (requirement == null) throw new ArgumentNullException(nameof(requirement));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!requirement.Parameters.TryGetValue("key", out var key) || string.IsNullOrWhiteSpace(key))
            {
                return RequirementCheckResult.Fail(
                    "REQUIREMENT_PARAMETER_MISSING",
                    "The state.exists requirement needs a non-empty 'key' parameter.");
            }

            return context.StateValues.ContainsKey(key)
                ? RequirementCheckResult.Pass()
                : RequirementCheckResult.Fail(
                    "STATE_VALUE_NOT_FOUND",
                    $"State '{key}' does not exist.");
        }
    }

    public sealed class SetEventStateEffectHandler : IEffectHandler
    {
        public const string HandlerOperationId = "state.set";
        public string OperationId => HandlerOperationId;

        public EffectApplicationResult Apply(GameEffectDefinition effect, EventState state)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (state == null) throw new ArgumentNullException(nameof(state));

            state.SetValue(effect.TargetId, effect.Value);
            return EffectApplicationResult.Success();
        }
    }

    public sealed class RemoveEventStateEffectHandler : IEffectHandler
    {
        public const string HandlerOperationId = "state.remove";
        public string OperationId => HandlerOperationId;

        public EffectApplicationResult Apply(GameEffectDefinition effect, EventState state)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (state == null) throw new ArgumentNullException(nameof(state));

            return state.RemoveValue(effect.TargetId)
                ? EffectApplicationResult.Success()
                : EffectApplicationResult.Failure(
                    "STATE_VALUE_NOT_FOUND",
                    $"State '{effect.TargetId}' cannot be removed because it does not exist.");
        }
    }
}
