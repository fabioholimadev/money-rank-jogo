using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardCaptureResult
    {
        private readonly ReadOnlyCollection<InnerBoardRelocation> _relocations;

        public InnerBoardCaptureResult(PlayerId movedPlayerId, IEnumerable<InnerBoardRelocation> relocations)
        {
            if (string.IsNullOrWhiteSpace(movedPlayerId.Value))
                throw new ArgumentException("Moved player ID cannot be empty.", nameof(movedPlayerId));
            if (relocations == null)
                throw new ArgumentNullException(nameof(relocations));

            MovedPlayerId = movedPlayerId;
            _relocations = relocations.ToList().AsReadOnly();
        }

        public PlayerId MovedPlayerId { get; }
        public IReadOnlyList<InnerBoardRelocation> Relocations => _relocations;
    }
}
