using System;

namespace MoneyRank.Domain.Hospital
{
    public sealed class HospitalState
    {
        public HospitalState(PlayerId playerId)
        {
            PlayerId = playerId;
            ReasonId = string.Empty;
        }

        public PlayerId PlayerId { get; }
        public bool IsHospitalized { get; private set; }
        public int PendingMovementSkips { get; private set; }
        public string ReasonId { get; private set; }

        internal void Admit(string reasonId, int pendingMovementSkips)
        {
            if (string.IsNullOrWhiteSpace(reasonId))
                throw new ArgumentException("Hospital admission reason ID cannot be empty.", nameof(reasonId));
            if (pendingMovementSkips < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingMovementSkips));

            IsHospitalized = true;
            PendingMovementSkips = pendingMovementSkips;
            ReasonId = reasonId;
        }

        internal void Apply(HospitalResolutionCalculation calculation)
        {
            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));

            IsHospitalized = calculation.RemainsHospitalized;
            PendingMovementSkips = calculation.PendingMovementSkips;
            if (!IsHospitalized)
                ReasonId = string.Empty;
        }

        internal void ConsumeMovementSkip()
        {
            if (PendingMovementSkips <= 0)
                throw new InvalidOperationException("There is no pending hospital movement skip.");

            PendingMovementSkips--;
        }

        public void Restore(bool isHospitalized, int pendingMovementSkips, string reasonId)
        {
            if (pendingMovementSkips < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingMovementSkips));
            if (isHospitalized && string.IsNullOrWhiteSpace(reasonId))
                throw new ArgumentException("A hospitalized player requires a reason ID.", nameof(reasonId));
            if (!isHospitalized && pendingMovementSkips != 0)
                throw new ArgumentException("A player outside the hospital cannot have pending movement skips.", nameof(pendingMovementSkips));

            IsHospitalized = isHospitalized;
            PendingMovementSkips = pendingMovementSkips;
            ReasonId = isHospitalized ? reasonId : string.Empty;
        }
    }
}
