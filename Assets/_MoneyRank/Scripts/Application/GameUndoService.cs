using System;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class GameUndoService
    {
        private readonly IGameSessionRepository _repository;
        private readonly SessionUndoJournal _journal;
        private readonly SessionActionRecorder _actionRecorder;

        public GameUndoService(
            IGameSessionRepository repository,
            SessionUndoJournal journal,
            SessionActionRecorder actionRecorder = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _actionRecorder = actionRecorder;
        }

        public OperationResult CreateCheckpoint(
            SessionId sessionId,
            string checkpointId,
            string label,
            DateTimeOffset createdAt,
            string operatorId)
        {
            var session = RequireSession(sessionId);
            return _journal.AddCheckpoint(
                new SessionUndoCheckpoint(
                    checkpointId,
                    label,
                    session.CreateMemento(),
                    createdAt,
                    operatorId));
        }

        public OperationResult<SessionUndoRecord> Undo(
            SessionId sessionId,
            string checkpointId,
            string undoRecordId,
            DateTimeOffset createdAt,
            string operatorId)
        {
            var session = RequireSession(sessionId);
            if (_journal.Records.Any(record => string.Equals(record.Id, undoRecordId, StringComparison.Ordinal)))
            {
                return OperationResult<SessionUndoRecord>.Failure(
                    "DUPLICATE_UNDO_RECORD_ID",
                    $"Undo record '{undoRecordId}' already exists.");
            }

            var checkpoint = _journal.GetCheckpoint(checkpointId);
            if (checkpoint == null)
            {
                return OperationResult<SessionUndoRecord>.Failure(
                    "UNDO_CHECKPOINT_NOT_FOUND",
                    $"Undo checkpoint '{checkpointId}' was not found.");
            }
            if (checkpoint.Memento.SessionId != sessionId)
            {
                return OperationResult<SessionUndoRecord>.Failure(
                    "UNDO_CHECKPOINT_SESSION_MISMATCH",
                    "The undo checkpoint belongs to a different session.");
            }

            var beforeState = session.CreateMemento();
            var preservedEventHistory = session.EventHistory.Records.ToList();
            var preservedEconomyHistory = session.EconomyHistory.Records.ToList();
            var preservedActionHistory = session.ActionHistory.Records.ToList();
            var restore = session.Restore(checkpoint.Memento);
            if (!restore.Succeeded)
                return OperationResult<SessionUndoRecord>.Failure(restore.ErrorCode, restore.Message);

            session.EventHistory.Restore(preservedEventHistory);
            session.EconomyHistory.Restore(preservedEconomyHistory);
            session.ActionHistory.Restore(preservedActionHistory);
            _actionRecorder?.Record(
                session,
                "action.undone",
                session.CurrentPlayer?.Id,
                beforeState.Phase,
                new[]
                {
                    new DefinitionParameter("undoRecordId", undoRecordId),
                    new DefinitionParameter("checkpointId", checkpointId)
                });
            var afterState = session.CreateMemento();
            var record = new SessionUndoRecord(
                undoRecordId,
                checkpointId,
                beforeState,
                afterState,
                createdAt,
                operatorId);
            var journalResult = _journal.AddRecord(record);
            if (!journalResult.Succeeded)
                throw new InvalidOperationException(journalResult.Message);

            _repository.Save(session);
            return OperationResult<SessionUndoRecord>.Success(record);
        }

        private GameSession RequireSession(SessionId sessionId) =>
            _repository.Get(sessionId) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }
}
