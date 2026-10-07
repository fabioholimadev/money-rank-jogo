using System;

namespace MoneyRank.Domain
{
    public sealed class AttributeState
    {
        public AttributeState(string attributeId, int currentProgress = 0, int completedCycles = 0)
        {
            if (string.IsNullOrWhiteSpace(attributeId))
                throw new ArgumentException("Attribute ID cannot be empty.", nameof(attributeId));
            if (currentProgress < 0)
                throw new ArgumentOutOfRangeException(nameof(currentProgress));
            if (completedCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(completedCycles));

            AttributeId = attributeId;
            CurrentProgress = currentProgress;
            CompletedCycles = completedCycles;
        }

        public string AttributeId { get; }
        public int CurrentProgress { get; private set; }
        public int CompletedCycles { get; private set; }

        public void Apply(AttributeProgressionCalculation calculation)
        {
            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));

            CurrentProgress = calculation.CurrentProgress;
            checked
            {
                CompletedCycles += calculation.CompletedCycles;
            }
        }

        public void Restore(int currentProgress, int completedCycles)
        {
            if (currentProgress < 0)
                throw new ArgumentOutOfRangeException(nameof(currentProgress));
            if (completedCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(completedCycles));

            CurrentProgress = currentProgress;
            CompletedCycles = completedCycles;
        }
    }
}
