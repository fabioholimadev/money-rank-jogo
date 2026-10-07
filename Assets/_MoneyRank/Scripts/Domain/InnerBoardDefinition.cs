using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardDefinition
    {
        private readonly ReadOnlyCollection<InnerBoardSpaceDefinition> _spaces;

        public InnerBoardDefinition(
            string id,
            IEnumerable<InnerBoardSpaceDefinition> spaces,
            int startIndex,
            string movementPolicyId,
            string capturePolicyId,
            string victoryPolicyId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Inner board ID cannot be empty.", nameof(id));
            if (spaces == null)
                throw new ArgumentNullException(nameof(spaces));
            if (string.IsNullOrWhiteSpace(movementPolicyId))
                throw new ArgumentException("Inner board movement policy ID cannot be empty.", nameof(movementPolicyId));
            if (string.IsNullOrWhiteSpace(capturePolicyId))
                throw new ArgumentException("Inner board capture policy ID cannot be empty.", nameof(capturePolicyId));
            if (string.IsNullOrWhiteSpace(victoryPolicyId))
                throw new ArgumentException("Inner board victory policy ID cannot be empty.", nameof(victoryPolicyId));

            var orderedSpaces = spaces.OrderBy(space => space?.Index ?? -1).ToList();
            if (orderedSpaces.Count == 0)
                throw new ArgumentException("Inner board requires at least one space.", nameof(spaces));
            if (orderedSpaces.Any(space => space == null))
                throw new ArgumentException("Inner board spaces cannot contain null entries.", nameof(spaces));
            if (orderedSpaces.Select(space => space.Id).Distinct(StringComparer.Ordinal).Count() != orderedSpaces.Count)
                throw new ArgumentException("Inner board space IDs must be unique.", nameof(spaces));
            for (var index = 0; index < orderedSpaces.Count; index++)
            {
                if (orderedSpaces[index].Index != index)
                    throw new ArgumentException("Inner board space indexes must be contiguous and start at zero.", nameof(spaces));
            }
            if (startIndex < 0 || startIndex >= orderedSpaces.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            Id = id;
            StartIndex = startIndex;
            MovementPolicyId = movementPolicyId;
            CapturePolicyId = capturePolicyId;
            VictoryPolicyId = victoryPolicyId;
            _spaces = orderedSpaces.AsReadOnly();
        }

        public string Id { get; }
        public int StartIndex { get; }
        public string MovementPolicyId { get; }
        public string CapturePolicyId { get; }
        public string VictoryPolicyId { get; }
        public int SpaceCount => _spaces.Count;
        public IReadOnlyList<InnerBoardSpaceDefinition> Spaces => _spaces;

        public InnerBoardSpaceDefinition GetSpace(int index) =>
            index >= 0 && index < _spaces.Count ? _spaces[index] : null;
    }
}
