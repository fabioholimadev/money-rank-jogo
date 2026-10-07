namespace MoneyRank.Domain.Economy
{
    public sealed class EconomicResourceTransactionResult
    {
        public EconomicResourceTransactionResult(
            string resourceId,
            PlayerId playerId,
            decimal previousQuantity,
            decimal currentQuantity,
            EconomyTransactionRecord transaction)
        {
            ResourceId = resourceId;
            PlayerId = playerId;
            PreviousQuantity = previousQuantity;
            CurrentQuantity = currentQuantity;
            Transaction = transaction;
        }

        public string ResourceId { get; }
        public PlayerId PlayerId { get; }
        public decimal PreviousQuantity { get; }
        public decimal CurrentQuantity { get; }
        public EconomyTransactionRecord Transaction { get; }
    }
}
