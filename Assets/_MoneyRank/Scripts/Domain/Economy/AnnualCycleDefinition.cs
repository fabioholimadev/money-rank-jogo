using System;

namespace MoneyRank.Domain.Economy
{
    public sealed class AnnualCycleDefinition
    {
        public AnnualCycleDefinition(string id, int completedLapsPerCycle, string policyId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Annual cycle ID cannot be empty.", nameof(id));
            if (completedLapsPerCycle <= 0)
                throw new ArgumentOutOfRangeException(nameof(completedLapsPerCycle), "Completed laps per cycle must be positive.");
            if (string.IsNullOrWhiteSpace(policyId))
                throw new ArgumentException("Annual cycle policy ID cannot be empty.", nameof(policyId));

            Id = id;
            CompletedLapsPerCycle = completedLapsPerCycle;
            PolicyId = policyId;
        }

        public string Id { get; }
        public int CompletedLapsPerCycle { get; }
        public string PolicyId { get; }
    }
}
