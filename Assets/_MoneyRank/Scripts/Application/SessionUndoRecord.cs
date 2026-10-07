using System;
using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public sealed class SessionUndoRecord
    {
        public SessionUndoRecord(
            string id,
            string checkpointId,
            GameSessionMemento beforeState,
            GameSessionMemento afterState,
            DateTimeOffset createdAt,
            string operatorId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Undo record ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(checkpointId))
                throw new ArgumentException("Undo checkpoint ID cannot be empty.", nameof(checkpointId));
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Undo operator ID cannot be empty.", nameof(operatorId));

            Id = id;
            CheckpointId = checkpointId;
            BeforeState = beforeState ?? throw new ArgumentNullException(nameof(beforeState));
            AfterState = afterState ?? throw new ArgumentNullException(nameof(afterState));
            CreatedAt = createdAt;
            OperatorId = operatorId;
        }

        public string Id { get; }
        public string CheckpointId { get; }
        public GameSessionMemento BeforeState { get; }
        public GameSessionMemento AfterState { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
    }
}
