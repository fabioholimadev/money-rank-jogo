using System;
using System.Collections.Generic;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class GameEventSelectionService
    {
        private readonly IGameSessionRepository _repository;
        private readonly EventContentCatalog _catalog;
        private readonly EventSelectionService _selectionService;
        private readonly SessionActionRecorder _actionRecorder;

        public GameEventSelectionService(
            IGameSessionRepository repository,
            EventContentCatalog catalog,
            IEnumerable<IEventSelectionPolicy> policies,
            SessionActionRecorder actionRecorder = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _selectionService = new EventSelectionService(catalog, policies);
            _actionRecorder = actionRecorder;
        }

        public OperationResult<EventSelectionResult> SelectCurrentSpace(
            SessionId sessionId,
            string policyId,
            int drawIndex,
            IEnumerable<DefinitionParameter> parameters)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            if (session.Phase != GamePhase.ResolveSpace)
            {
                return OperationResult<EventSelectionResult>.Failure(
                    "EVENT_SELECTION_NOT_ALLOWED",
                    "An event can only be selected during ResolveSpace.");
            }
            if (session.CurrentPlayer == null || session.Board == null ||
                !session.TryGetBoardState(session.CurrentPlayer.Id, out var boardState))
            {
                return OperationResult<EventSelectionResult>.Failure(
                    "EVENT_SELECTION_BOARD_STATE_NOT_FOUND",
                    "The current player does not have a board state for event selection.");
            }

            var space = session.Board.GetSpace(boardState.Position);
            if (string.IsNullOrWhiteSpace(space.EventTableId))
            {
                return OperationResult<EventSelectionResult>.Failure(
                    "EVENT_TABLE_NOT_CONFIGURED",
                    "The current board space does not configure an event table.");
            }

            var context = new EventSelectionContext(
                session.Id,
                session.CurrentPlayer.Id,
                space.Id,
                drawIndex,
                parameters ?? Array.Empty<DefinitionParameter>());
            var selection = _selectionService.Select(space.EventTableId, policyId, context);
            if (!selection.Succeeded)
                return selection;

            var beginChoice = session.BeginEventChoice(selection.Value, space.Id);
            if (!beginChoice.Succeeded)
            {
                return OperationResult<EventSelectionResult>.Failure(
                    beginChoice.ErrorCode,
                    beginChoice.Message);
            }

            _actionRecorder?.Record(
                session,
                "event.selected",
                session.CurrentPlayer.Id,
                phaseBefore,
                new[]
                {
                    new DefinitionParameter("eventId", selection.Value.Definition.Id),
                    new DefinitionParameter("eventVersion", selection.Value.Definition.Version.ToString()),
                    new DefinitionParameter("spaceId", space.Id),
                    new DefinitionParameter("tableId", selection.Value.TableId),
                    new DefinitionParameter("policyId", selection.Value.PolicyId),
                    new DefinitionParameter("drawIndex", selection.Value.DrawIndex.ToString())
                });
            _repository.Save(session);
            return selection;
        }

        public OperationResult<EventDefinition> GetActiveDefinition(SessionId sessionId)
        {
            var session = RequireSession(sessionId);
            if (session.ActiveEvent == null)
            {
                return OperationResult<EventDefinition>.Failure(
                    "ACTIVE_EVENT_NOT_FOUND",
                    "The session does not have an active event selection.");
            }

            var definition = _catalog.GetEvent(session.ActiveEvent.EventKey);
            return definition == null
                ? OperationResult<EventDefinition>.Failure(
                    "ACTIVE_EVENT_DEFINITION_NOT_FOUND",
                    $"Active event definition '{session.ActiveEvent.EventKey}' was not found.")
                : OperationResult<EventDefinition>.Success(definition);
        }

        private GameSession RequireSession(SessionId sessionId) =>
            _repository.Get(sessionId) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }
}
