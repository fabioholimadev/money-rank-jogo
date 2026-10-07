using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Hospital
{
    public sealed class HospitalResolutionResult
    {
        private readonly ReadOnlyCollection<GameEffectDefinition> _effects;

        public HospitalResolutionResult(
            PlayerId playerId,
            bool wasHospitalized,
            bool isHospitalized,
            int pendingMovementSkips,
            IEnumerable<GameEffectDefinition> effects)
        {
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));

            PlayerId = playerId;
            WasHospitalized = wasHospitalized;
            IsHospitalized = isHospitalized;
            PendingMovementSkips = pendingMovementSkips;
            _effects = effects.ToList().AsReadOnly();
        }

        public PlayerId PlayerId { get; }
        public bool WasHospitalized { get; }
        public bool IsHospitalized { get; }
        public int PendingMovementSkips { get; }
        public IReadOnlyList<GameEffectDefinition> Effects => _effects;
    }
}
