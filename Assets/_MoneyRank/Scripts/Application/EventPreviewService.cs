using System;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class EventPreviewService
    {
        private readonly RequirementEvaluator _requirementEvaluator;
        private readonly EffectExecutor _effectExecutor;

        public EventPreviewService(
            RequirementEvaluator requirementEvaluator,
            EffectExecutor effectExecutor)
        {
            _requirementEvaluator = requirementEvaluator ?? throw new ArgumentNullException(nameof(requirementEvaluator));
            _effectExecutor = effectExecutor ?? throw new ArgumentNullException(nameof(effectExecutor));
        }

        public OperationResult<EventResolutionPreview> Preview(
            EventDefinition eventDefinition,
            string optionId,
            RequirementEvaluationContext context)
        {
            if (eventDefinition == null)
                throw new ArgumentNullException(nameof(eventDefinition));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var option = eventDefinition.GetOption(optionId);
            if (option == null)
            {
                return OperationResult<EventResolutionPreview>.Failure(
                    "EVENT_OPTION_NOT_FOUND",
                    $"Option '{optionId}' was not found in event '{eventDefinition.Id}'.");
            }

            var conditionResults = eventDefinition.Conditions
                .Select(condition => _requirementEvaluator.Evaluate(condition, context));
            var requirementResults = option.Requirements
                .Select(requirement => _requirementEvaluator.Evaluate(requirement, context));
            var evaluatedState = context.CreateStateSnapshot();
            var effectPreview = _effectExecutor.Preview(option.Effects, evaluatedState);
            var preview = new EventResolutionPreview(
                eventDefinition,
                option,
                context.SessionId,
                context.PlayerId,
                evaluatedState,
                conditionResults,
                requirementResults,
                effectPreview.Succeeded ? effectPreview.Value : null,
                effectPreview.ErrorCode,
                effectPreview.Message);

            return OperationResult<EventResolutionPreview>.Success(preview);
        }
    }
}
