using System;

namespace MoneyRank.Domain
{
    public sealed class BoardSpaceDefinition
    {
        public BoardSpaceDefinition(string id, int index, string typeId, string categoryId = null, string eventTableId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Space ID cannot be empty.", nameof(id));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (string.IsNullOrWhiteSpace(typeId)) throw new ArgumentException("Space type ID cannot be empty.", nameof(typeId));

            Id = id;
            Index = index;
            TypeId = typeId;
            CategoryId = categoryId;
            EventTableId = eventTableId;
        }

        public string Id { get; }
        public int Index { get; }
        public string TypeId { get; }
        public string CategoryId { get; }
        public string EventTableId { get; }
    }
}
