namespace MoneyRank.Domain.Economy
{
    public sealed class AnnualCycleResolutionResult
    {
        public AnnualCycleResolutionResult(
            PlayerId playerId,
            int cycleNumber,
            int remainingPendingCycles,
            EconomyTransactionRecord transaction)
        {
            PlayerId = playerId;
            CycleNumber = cycleNumber;
            RemainingPendingCycles = remainingPendingCycles;
            Transaction = transaction;
        }

        public PlayerId PlayerId { get; }
        public int CycleNumber { get; }
        public int RemainingPendingCycles { get; }
        public EconomyTransactionRecord Transaction { get; }
    }
}
