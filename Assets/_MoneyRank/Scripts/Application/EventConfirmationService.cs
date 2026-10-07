using System;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class EventConfirmationService
    {
        private readonly EffectExecutor _effectExecutor;

        public EventConfirmationService(EffectExecutor effectExecutor)
        {
            _effectExecutor = effectExecutor ?? throw new ArgumentNullException(nameof(effectExecutor));
        }

        public OperationResult<EventResolutionRecord> Confirm(
            EventResolutionPreview preview,
            EventState state,
            EventResolutionHistory history,
            EventConfirmationRequest request)
        {
            if (preview == null)
                throw new ArgumentNullException(nameof(preview));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (history == null)
                throw new ArgumentNullException(nameof(history));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!preview.CanConfirm)
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "EVENT_REQUIREMENTS_NOT_SATISFIED",
                    "The event option cannot be confirmed while requirements are not satisfied.");
            }

            if (history.Contains(request.ResolutionId))
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "DUPLICATE_RESOLUTION_ID",
                    $"Resolution '{request.ResolutionId}' already exists in history.");
            }

            if (!state.CreateSnapshot().ContentEquals(preview.EvaluatedState))
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "EVENT_PREVIEW_STALE",
                    "The event state changed after the preview was generated.");
            }

            var beforeState = state.CreateSnapshot();
            var workingState = EventState.FromSnapshot(beforeState);
            foreach (var effect in preview.PlannedEffects)
            {
                var effectResult = _effectExecutor.Execute(effect, workingState);
                if (!effectResult.Succeeded)
                {
                    return OperationResult<EventResolutionRecord>.Failure(
                        effectResult.ErrorCode,
                        effectResult.Message);
                }
            }

            var afterState = workingState.CreateSnapshot();
            var record = new EventResolutionRecord(
                request.ResolutionId,
                preview.EventDefinition.Id,
                preview.EventDefinition.Version,
                preview.PlayerId,
                request.SpaceId,
                preview.Option.Id,
                beforeState,
                afterState,
                request.SourceId,
                request.CreatedAt,
                request.OperatorId);
            var historyResult = history.Append(record);
            if (!historyResult.Succeeded)
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    historyResult.ErrorCode,
                    historyResult.Message);
            }

            state.Restore(afterState);
            return OperationResult<EventResolutionRecord>.Success(record);
        }
    }
}
