using System;

namespace MoneyRank.Domain.Economy
{
    public sealed class AnnualCycleContext
    {
        public AnnualCycleContext(
            PlayerId playerId,
            int cycleNumber,
            EconomySnapshot economy,
            string transactionId,
            DateTimeOffset createdAt,
            string operatorId)
        {
            if (cycleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(cycleNumber));
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Annual cycle transaction ID cannot be empty.", nameof(transactionId));
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Annual cycle operator ID cannot be empty.", nameof(operatorId));

            PlayerId = playerId;
            CycleNumber = cycleNumber;
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            TransactionId = transactionId;
            CreatedAt = createdAt;
            OperatorId = operatorId;
        }

        public PlayerId PlayerId { get; }
        public int CycleNumber { get; }
        public EconomySnapshot Economy { get; }
        public string TransactionId { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
    }
}
