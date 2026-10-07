namespace MoneyRank.Domain
{
    public interface IInnerBoardMovementPolicy
    {
        string PolicyId { get; }

        OperationResult<InnerBoardMovementCalculation> Calculate(
            InnerBoardDefinition definition,
            InnerBoardState state,
            int steps);
    }
}
