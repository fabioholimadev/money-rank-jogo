namespace MoneyRank.Domain.Economy
{
    public interface IEconomicResourceTransactionPolicy
    {
        string PolicyId { get; }

        OperationResult<EconomyTransactionRequest> CreateTransaction(
            EconomicResourceDefinition definition,
            EconomicResourceCommand command,
            EconomySnapshot economy);
    }
}
