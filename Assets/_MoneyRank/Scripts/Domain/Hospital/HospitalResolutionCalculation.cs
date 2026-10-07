using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Hospital
{
    public sealed class HospitalResolutionCalculation
    {
        private readonly ReadOnlyCollection<GameEffectDefinition> _effects;

        public HospitalResolutionCalculation(
            bool remainsHospitalized,
            int pendingMovementSkips,
            IEnumerable<GameEffectDefinition> effects)
        {
            if (pendingMovementSkips < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingMovementSkips));
            if (!remainsHospitalized && pendingMovementSkips != 0)
                throw new ArgumentException("A recovered player cannot keep pending movement skips.", nameof(pendingMovementSkips));
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));

            var effectList = effects.ToList();
            if (effectList.Any(effect => effect == null))
                throw new ArgumentException("Hospital effects cannot contain null entries.", nameof(effects));

            RemainsHospitalized = remainsHospitalized;
            PendingMovementSkips = pendingMovementSkips;
            _effects = effectList.AsReadOnly();
        }

        public bool RemainsHospitalized { get; }
        public int PendingMovementSkips { get; }
        public IReadOnlyList<GameEffectDefinition> Effects => _effects;
    }
}
