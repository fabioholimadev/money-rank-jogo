namespace MoneyRank.Domain
{
    public interface IAttributeOverflowHandler
    {
        string BehaviorId { get; }

        OperationResult<AttributeProgressionCalculation> Calculate(
            int currentProgress,
            int delta,
            int threshold);
    }
}
