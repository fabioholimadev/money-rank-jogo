using System;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;

namespace MoneyRank.Application
{
    public sealed class GameAnnualCycleService
    {
        private readonly IGameSessionRepository _repository;
        private readonly AnnualCycleService _annualCycleService;

        public GameAnnualCycleService(
            IGameSessionRepository repository,
            AnnualCycleService annualCycleService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _annualCycleService = annualCycleService ?? throw new ArgumentNullException(nameof(annualCycleService));
        }

        public OperationResult<AnnualCycleResolutionResult> ResolveCurrentPlayer(
            SessionId sessionId,
            string transactionId,
            DateTimeOffset createdAt,
            string operatorId)
        {
            var session = RequireSession(sessionId);
            if (session.Phase != GamePhase.CheckProgress)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_PHASE_INVALID",
                    "Annual cycles can only be resolved during CheckProgress.");
            }
            if (session.AnnualCycle == null || session.Economy == null)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_NOT_CONFIGURED",
                    "The session does not have annual cycle and economy definitions.");
            }
            if (session.CurrentPlayer == null ||
                !session.TryGetBoardState(session.CurrentPlayer.Id, out var boardState) ||
                !session.TryGetAnnualCycleState(session.CurrentPlayer.Id, out var annualState))
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_PLAYER_STATE_NOT_FOUND",
                    "The current player does not have the states required for an annual cycle.");
            }

            var result = _annualCycleService.ResolveNext(
                session.AnnualCycle,
                annualState,
                boardState.CompletedLaps,
                session.EconomyState,
                session.EconomyHistory,
                transactionId,
                createdAt,
                operatorId);
            if (result.Succeeded)
                _repository.Save(session);
            return result;
        }

        private GameSession RequireSession(SessionId sessionId) =>
            _repository.Get(sessionId) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }
}
