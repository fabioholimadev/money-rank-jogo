using System;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class GameEventService
    {
        private readonly IGameSessionRepository _repository;
        private readonly EventPreviewService _previewService;
        private readonly EventConfirmationService _confirmationService;
        private readonly SessionActionRecorder _actionRecorder;

        public GameEventService(
            IGameSessionRepository repository,
            EventPreviewService previewService,
            EventConfirmationService confirmationService,
            SessionActionRecorder actionRecorder = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
            _confirmationService = confirmationService ?? throw new ArgumentNullException(nameof(confirmationService));
            _actionRecorder = actionRecorder;
        }

        public OperationResult<EventResolutionPreview> PreviewOption(
            SessionId sessionId,
            EventDefinition eventDefinition,
            string optionId)
        {
            var session = RequireSession(sessionId);
            if (session.Phase != GamePhase.ResolveSpace && session.Phase != GamePhase.AwaitChoice)
            {
                return OperationResult<EventResolutionPreview>.Failure(
                    "EVENT_PREVIEW_NOT_ALLOWED",
                    "An event option can only be previewed during ResolveSpace or AwaitChoice.");
            }

            if (session.CurrentPlayer == null ||
                !session.TryGetEventState(session.CurrentPlayer.Id, out var eventState))
            {
                return OperationResult<EventResolutionPreview>.Failure(
                    "PLAYER_EVENT_STATE_NOT_FOUND",
                    "The current player does not have an event state.");
            }
            if (session.Phase == GamePhase.AwaitChoice &&
                (session.ActiveEvent == null || !session.ActiveEvent.Matches(eventDefinition)))
            {
                return OperationResult<EventResolutionPreview>.Failure(
                    "EVENT_PREVIEW_NOT_ACTIVE",
                    "The requested event is not the active selection.");
            }

            var snapshot = eventState.CreateSnapshot();
            var context = new RequirementEvaluationContext(
                sessionId,
                session.CurrentPlayer.Id,
                snapshot.Values.Select(pair => new DefinitionParameter(pair.Key, pair.Value)));
            return _previewService.Preview(eventDefinition, optionId, context);
        }

        public OperationResult<EventResolutionRecord> ConfirmOption(
            SessionId sessionId,
            EventResolutionPreview preview,
            EventConfirmationRequest request)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            if (session.Phase != GamePhase.AwaitChoice)
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "EVENT_CONFIRMATION_NOT_ALLOWED",
                    "An event option can only be confirmed during AwaitChoice.");
            }

            if (preview == null)
                throw new ArgumentNullException(nameof(preview));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (preview.SessionId != sessionId)
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "EVENT_PREVIEW_SESSION_MISMATCH",
                    "The preview belongs to a different session.");
            }
            if (session.CurrentPlayer == null || preview.PlayerId != session.CurrentPlayer.Id)
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "EVENT_PREVIEW_PLAYER_MISMATCH",
                    "The preview belongs to a different player.");
            }
            if (!session.TryGetEventState(session.CurrentPlayer.Id, out var eventState))
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "PLAYER_EVENT_STATE_NOT_FOUND",
                    "The current player does not have an event state.");
            }
            if (session.ActiveEvent == null || !session.ActiveEvent.Matches(preview.EventDefinition))
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "EVENT_CONFIRMATION_NOT_ACTIVE",
                    "The preview does not belong to the active event selection.");
            }
            if (!string.Equals(session.ActiveEvent.SpaceId, request.SpaceId, StringComparison.Ordinal))
            {
                return OperationResult<EventResolutionRecord>.Failure(
                    "EVENT_CONFIRMATION_SPACE_MISMATCH",
                    "The confirmation space does not match the active event selection.");
            }

            var confirmation = _confirmationService.Confirm(
                preview,
                eventState,
                session.EventHistory,
                request);
            if (!confirmation.Succeeded)
            {
                return confirmation;
            }

            var transition = session.CompleteEventChoice(preview.EventDefinition, request.SpaceId);
            if (!transition.Succeeded)
                throw new InvalidOperationException(transition.Message);

            _actionRecorder?.Record(
                session,
                "event.confirmed",
                preview.PlayerId,
                phaseBefore,
                new[]
                {
                    new DefinitionParameter("resolutionId", request.ResolutionId),
                    new DefinitionParameter("eventId", preview.EventDefinition.Id),
                    new DefinitionParameter("eventVersion", preview.EventDefinition.Version.ToString()),
                    new DefinitionParameter("optionId", preview.Option.Id),
                    new DefinitionParameter("spaceId", request.SpaceId)
                });
            _repository.Save(session);
            return confirmation;
        }

        private GameSession RequireSession(SessionId sessionId) =>
            _repository.Get(sessionId) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }
}
