using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardCaptureCalculation
    {
        private readonly ReadOnlyCollection<InnerBoardRelocation> _relocations;

        public InnerBoardCaptureCalculation(IEnumerable<InnerBoardRelocation> relocations)
        {
            if (relocations == null)
                throw new ArgumentNullException(nameof(relocations));

            var list = relocations.ToList();
            if (list.Any(relocation => relocation == null))
                throw new ArgumentException("Capture relocations cannot contain null entries.", nameof(relocations));
            if (list.Select(relocation => relocation.PlayerId).Distinct().Count() != list.Count)
                throw new ArgumentException("A capture policy cannot relocate the same player more than once.", nameof(relocations));

            _relocations = list.AsReadOnly();
        }

        public IReadOnlyList<InnerBoardRelocation> Relocations => _relocations;
    }
}
