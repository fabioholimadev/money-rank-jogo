using System;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;

namespace MoneyRank.Application
{
    public sealed class AdministrativeEconomyAdjustmentService
    {
        public const string OperationId = "administration:manual-economy-adjustment";

        private readonly IGameSessionRepository _repository;
        private readonly IAdministrationAuthorizationPolicy _authorization;

        public AdministrativeEconomyAdjustmentService(
            IGameSessionRepository repository,
            IAdministrationAuthorizationPolicy authorization)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        }

        public OperationResult<EconomyTransactionRecord> Apply(
            SessionId sessionId,
            ManualEconomyAdjustmentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var session = _repository.Get(sessionId) ??
                throw new InvalidOperationException($"Session '{sessionId}' was not found.");
            if (session.Economy == null)
            {
                return OperationResult<EconomyTransactionRecord>.Failure(
                    "ECONOMY_NOT_CONFIGURED",
                    "The session does not have an economy definition.");
            }

            var authorization = _authorization.Authorize(
                sessionId,
                request.OperatorId,
                OperationId);
            if (!authorization.Succeeded)
            {
                return OperationResult<EconomyTransactionRecord>.Failure(
                    authorization.ErrorCode,
                    authorization.Message);
            }

            var transaction = new EconomyTransactionRequest(
                request.Id,
                request.Entries,
                OperationId,
                request.CreatedAt,
                request.OperatorId,
                request.Reason);
            var result = session.Economy.CreateTransactionService().Apply(
                session.EconomyState,
                session.EconomyHistory,
                transaction);
            if (result.Succeeded)
                _repository.Save(session);
            return result;
        }
    }
}
