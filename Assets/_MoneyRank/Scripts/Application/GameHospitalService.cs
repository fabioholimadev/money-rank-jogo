using System;
using MoneyRank.Domain;
using MoneyRank.Domain.Hospital;

namespace MoneyRank.Application
{
    public sealed class GameHospitalService
    {
        private readonly IGameSessionRepository _repository;
        private readonly HospitalService _hospitalService;

        public GameHospitalService(
            IGameSessionRepository repository,
            HospitalService hospitalService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _hospitalService = hospitalService ?? throw new ArgumentNullException(nameof(hospitalService));
        }

        public OperationResult Admit(
            SessionId sessionId,
            PlayerId playerId,
            string reasonId,
            int pendingMovementSkips)
        {
            var state = RequireState(sessionId, playerId, out var session, out var failure);
            if (state == null) return failure;

            var result = _hospitalService.Admit(state, reasonId, pendingMovementSkips);
            if (result.Succeeded)
                _repository.Save(session);
            return result;
        }

        public OperationResult<bool> TryConsumeMovementSkip(SessionId sessionId, PlayerId playerId)
        {
            var state = RequireState(sessionId, playerId, out var session, out var failure);
            if (state == null)
                return OperationResult<bool>.Failure(failure.ErrorCode, failure.Message);

            var result = _hospitalService.TryConsumeMovementSkip(state);
            if (result.Succeeded && result.Value)
                _repository.Save(session);
            return result;
        }

        public OperationResult<HospitalResolutionResult> Resolve(
            SessionId sessionId,
            PlayerId playerId,
            HospitalResolutionCommand command)
        {
            var state = RequireState(sessionId, playerId, out var session, out var failure);
            if (state == null)
                return OperationResult<HospitalResolutionResult>.Failure(failure.ErrorCode, failure.Message);

            var result = _hospitalService.Resolve(session.Hospital, state, command);
            if (result.Succeeded)
                _repository.Save(session);
            return result;
        }

        private HospitalState RequireState(
            SessionId sessionId,
            PlayerId playerId,
            out GameSession session,
            out OperationResult failure)
        {
            session = _repository.Get(sessionId) ??
                throw new InvalidOperationException($"Session '{sessionId}' was not found.");
            if (session.Hospital == null || !session.TryGetHospitalState(playerId, out var state))
            {
                failure = OperationResult.Failure(
                    "HOSPITAL_NOT_CONFIGURED",
                    "The session or player does not have Hospital state.");
                return null;
            }

            failure = OperationResult.Success();
            return state;
        }
    }
}
