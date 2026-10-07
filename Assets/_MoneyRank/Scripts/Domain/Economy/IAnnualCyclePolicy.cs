namespace MoneyRank.Domain.Economy
{
    public interface IAnnualCyclePolicy
    {
        string PolicyId { get; }

        OperationResult<EconomyTransactionRequest> CreateTransaction(
            AnnualCycleDefinition definition,
            AnnualCycleContext context);
    }
}
