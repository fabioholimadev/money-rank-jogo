using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class BoardDefinition
    {
        private readonly ReadOnlyCollection<BoardSpaceDefinition> _spaces;

        public BoardDefinition(string id, IEnumerable<BoardSpaceDefinition> spaces, int startIndex)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Board ID cannot be empty.", nameof(id));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));

            var orderedSpaces = spaces.OrderBy(space => space.Index).ToList();
            if (orderedSpaces.Count == 0) throw new ArgumentException("A board requires at least one space.", nameof(spaces));
            if (startIndex < 0 || startIndex >= orderedSpaces.Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (orderedSpaces.Select(space => space.Id).Distinct(StringComparer.Ordinal).Count() != orderedSpaces.Count)
                throw new ArgumentException("Board space IDs must be unique.", nameof(spaces));

            for (var index = 0; index < orderedSpaces.Count; index++)
            {
                if (orderedSpaces[index].Index != index)
                    throw new ArgumentException("Board space indexes must be contiguous and start at zero.", nameof(spaces));
            }

            Id = id;
            StartIndex = startIndex;
            _spaces = orderedSpaces.AsReadOnly();
        }

        public string Id { get; }
        public int StartIndex { get; }
        public int SpaceCount => _spaces.Count;
        public IReadOnlyList<BoardSpaceDefinition> Spaces => _spaces;

        public BoardSpaceDefinition GetSpace(int index)
        {
            if (index < 0 || index >= SpaceCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _spaces[index];
        }
    }
}
