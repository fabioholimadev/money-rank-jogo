using System;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class GameProgressionService
    {
        private readonly IGameSessionRepository _repository;
        private readonly AttributeProgressionService _attributeProgression;
        private readonly InnerBoardProgressionService _innerBoardProgression;
        private readonly InnerBoardCaptureService _captureService;
        private readonly InnerBoardVictoryService _victoryService;
        private readonly SessionActionRecorder _actionRecorder;

        public GameProgressionService(
            IGameSessionRepository repository,
            AttributeProgressionService attributeProgression,
            InnerBoardProgressionService innerBoardProgression,
            InnerBoardCaptureService captureService,
            InnerBoardVictoryService victoryService,
            SessionActionRecorder actionRecorder = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _attributeProgression = attributeProgression ?? throw new ArgumentNullException(nameof(attributeProgression));
            _innerBoardProgression = innerBoardProgression ?? throw new ArgumentNullException(nameof(innerBoardProgression));
            _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
            _victoryService = victoryService ?? throw new ArgumentNullException(nameof(victoryService));
            _actionRecorder = actionRecorder;
        }

        public OperationResult<AttributeProgressionResult> ApplyAttributeProgress(
            SessionId sessionId,
            string attributeId,
            int delta)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            if (session.Phase != GamePhase.CheckProgress)
            {
                return OperationResult<AttributeProgressionResult>.Failure(
                    "ATTRIBUTE_PROGRESS_NOT_ALLOWED",
                    "Attribute progression can only be applied during CheckProgress.");
            }

            var definition = session.AttributeDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, attributeId, StringComparison.Ordinal));
            if (definition == null)
            {
                return OperationResult<AttributeProgressionResult>.Failure(
                    "ATTRIBUTE_DEFINITION_NOT_FOUND",
                    $"Attribute definition '{attributeId}' was not found in the session.");
            }
            if (session.CurrentPlayer == null ||
                !session.TryGetAttributeState(session.CurrentPlayer.Id, attributeId, out var state))
            {
                return OperationResult<AttributeProgressionResult>.Failure(
                    "PLAYER_ATTRIBUTE_STATE_NOT_FOUND",
                    "The current player does not have the requested attribute state.");
            }

            var result = _attributeProgression.Apply(definition, state, delta);
            if (result.Succeeded)
            {
                _actionRecorder?.Record(
                    session,
                    "attribute.changed",
                    session.CurrentPlayer.Id,
                    phaseBefore,
                    new[]
                    {
                        new DefinitionParameter("attributeId", attributeId),
                        new DefinitionParameter("delta", delta.ToString()),
                        new DefinitionParameter("before", result.Value.BeforeProgress.ToString()),
                        new DefinitionParameter("after", result.Value.AfterProgress.ToString()),
                        new DefinitionParameter("completedCycles", result.Value.CompletedCycles.ToString())
                    });
                _repository.Save(session);
            }
            return result;
        }

        public OperationResult<InnerBoardResolutionResult> ResolveInnerBoard(
            SessionId sessionId,
            int steps)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            if (session.Phase != GamePhase.CheckProgress)
            {
                return OperationResult<InnerBoardResolutionResult>.Failure(
                    "INNER_BOARD_RESOLUTION_NOT_ALLOWED",
                    "Inner board resolution can only run during CheckProgress.");
            }
            if (session.InnerBoard == null)
            {
                return OperationResult<InnerBoardResolutionResult>.Failure(
                    "INNER_BOARD_NOT_CONFIGURED",
                    "The session does not have an inner board definition.");
            }
            if (session.CurrentPlayer == null ||
                !session.TryGetInnerBoardState(session.CurrentPlayer.Id, out var currentState))
            {
                return OperationResult<InnerBoardResolutionResult>.Failure(
                    "PLAYER_INNER_BOARD_STATE_NOT_FOUND",
                    "The current player does not have an inner board state.");
            }

            var workingStates = session.GetInnerBoardStates()
                .Select(state => new InnerBoardState(state.PlayerId, state.Position, state.CheckpointIndex))
                .ToList();
            var workingCurrent = workingStates.Single(state => state.PlayerId == currentState.PlayerId);
            var move = _innerBoardProgression.Move(session.InnerBoard, workingCurrent, steps);
            if (!move.Succeeded)
                return Failure<InnerBoardResolutionResult>(move.ErrorCode, move.Message);

            var capture = _captureService.Resolve(session.InnerBoard, currentState.PlayerId, workingStates);
            if (!capture.Succeeded)
                return Failure<InnerBoardResolutionResult>(capture.ErrorCode, capture.Message);

            var victory = _victoryService.Evaluate(session.InnerBoard, currentState.PlayerId, workingStates);
            if (!victory.Succeeded)
                return Failure<InnerBoardResolutionResult>(victory.ErrorCode, victory.Message);

            foreach (var workingState in workingStates)
            {
                if (!session.TryGetInnerBoardState(workingState.PlayerId, out var committedState))
                    throw new InvalidOperationException("An inner board state disappeared during resolution.");
                committedState.Restore(workingState.Position, workingState.CheckpointIndex);
            }

            var transition = session.CompleteProgressCheck(victory.Value);
            if (!transition.Succeeded)
                throw new InvalidOperationException(transition.Message);

            _actionRecorder?.Record(
                session,
                "progress.inner.resolved",
                currentState.PlayerId,
                phaseBefore,
                new[]
                {
                    new DefinitionParameter("steps", steps.ToString()),
                    new DefinitionParameter("origin", move.Value.Origin.ToString()),
                    new DefinitionParameter("destination", move.Value.Destination.ToString()),
                    new DefinitionParameter("captureCount", capture.Value.Relocations.Count.ToString()),
                    new DefinitionParameter("gameEnded", victory.Value.GameEnded.ToString()),
                    new DefinitionParameter("reasonCode", victory.Value.ReasonCode ?? string.Empty)
                });
            _repository.Save(session);
            return OperationResult<InnerBoardResolutionResult>.Success(
                new InnerBoardResolutionResult(move.Value, capture.Value, victory.Value));
        }

        private static OperationResult<T> Failure<T>(string errorCode, string message) =>
            OperationResult<T>.Failure(errorCode, message);

        private GameSession RequireSession(SessionId sessionId) =>
            _repository.Get(sessionId) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }
}
