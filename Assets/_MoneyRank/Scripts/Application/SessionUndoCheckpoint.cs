using System;
using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public sealed class SessionUndoCheckpoint
    {
        public SessionUndoCheckpoint(
            string id,
            string label,
            GameSessionMemento memento,
            DateTimeOffset createdAt,
            string operatorId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Undo checkpoint ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Undo checkpoint label cannot be empty.", nameof(label));
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Undo checkpoint operator ID cannot be empty.", nameof(operatorId));

            Id = id;
            Label = label;
            Memento = memento ?? throw new ArgumentNullException(nameof(memento));
            CreatedAt = createdAt;
            OperatorId = operatorId;
        }

        public string Id { get; }
        public string Label { get; }
        public GameSessionMemento Memento { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
    }
}
