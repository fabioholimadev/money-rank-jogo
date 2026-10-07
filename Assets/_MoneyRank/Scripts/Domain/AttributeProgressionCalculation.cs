using System;

namespace MoneyRank.Domain
{
    public sealed class AttributeProgressionCalculation
    {
        public AttributeProgressionCalculation(int currentProgress, int completedCycles)
        {
            if (currentProgress < 0)
                throw new ArgumentOutOfRangeException(nameof(currentProgress));
            if (completedCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(completedCycles));

            CurrentProgress = currentProgress;
            CompletedCycles = completedCycles;
        }

        public int CurrentProgress { get; }
        public int CompletedCycles { get; }
    }
}
