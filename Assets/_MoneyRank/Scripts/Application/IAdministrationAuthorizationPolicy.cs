using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public interface IAdministrationAuthorizationPolicy
    {
        OperationResult Authorize(
            SessionId sessionId,
            string operatorId,
            string operationId);
    }
}
