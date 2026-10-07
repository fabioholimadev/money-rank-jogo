using System;

namespace MoneyRank.Domain.Economy
{
    public sealed class AnnualCycleState
    {
        public AnnualCycleState(PlayerId playerId, int processedCycles = 0)
        {
            if (processedCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(processedCycles));

            PlayerId = playerId;
            ProcessedCycles = processedCycles;
        }

        public PlayerId PlayerId { get; }
        public int ProcessedCycles { get; private set; }

        public void MarkProcessed(int cycleNumber)
        {
            if (cycleNumber != ProcessedCycles + 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cycleNumber),
                    "Annual cycles must be processed once and in sequence.");
            }

            ProcessedCycles = cycleNumber;
        }

        public void Restore(int processedCycles)
        {
            if (processedCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(processedCycles));

            ProcessedCycles = processedCycles;
        }
    }
}
