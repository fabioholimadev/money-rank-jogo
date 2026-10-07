using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public sealed class SessionUndoJournal
    {
        private readonly List<SessionUndoCheckpoint> _checkpoints = new List<SessionUndoCheckpoint>();
        private readonly List<SessionUndoRecord> _records = new List<SessionUndoRecord>();
        private readonly ReadOnlyCollection<SessionUndoCheckpoint> _readOnlyCheckpoints;
        private readonly ReadOnlyCollection<SessionUndoRecord> _readOnlyRecords;

        public SessionUndoJournal()
        {
            _readOnlyCheckpoints = _checkpoints.AsReadOnly();
            _readOnlyRecords = _records.AsReadOnly();
        }

        public IReadOnlyList<SessionUndoCheckpoint> Checkpoints => _readOnlyCheckpoints;
        public IReadOnlyList<SessionUndoRecord> Records => _readOnlyRecords;

        public OperationResult AddCheckpoint(SessionUndoCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            if (_checkpoints.Any(existing => string.Equals(existing.Id, checkpoint.Id, StringComparison.Ordinal)))
                return OperationResult.Failure("DUPLICATE_UNDO_CHECKPOINT_ID", $"Undo checkpoint '{checkpoint.Id}' already exists.");
            _checkpoints.Add(checkpoint);
            return OperationResult.Success();
        }

        public SessionUndoCheckpoint GetCheckpoint(string id) =>
            _checkpoints.LastOrDefault(checkpoint => string.Equals(checkpoint.Id, id, StringComparison.Ordinal));

        public OperationResult AddRecord(SessionUndoRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (_records.Any(existing => string.Equals(existing.Id, record.Id, StringComparison.Ordinal)))
                return OperationResult.Failure("DUPLICATE_UNDO_RECORD_ID", $"Undo record '{record.Id}' already exists.");
            _records.Add(record);
            return OperationResult.Success();
        }
    }
}
