using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public sealed class PlaytestExportRequest
    {
        private readonly ReadOnlyCollection<PlaytestParticipant> _participants;
        private readonly ReadOnlyCollection<SessionUndoRecord> _undoRecords;

        public PlaytestExportRequest(
            GameSession session,
            SessionUndoJournal undoJournal,
            DateTimeOffset exportedAt,
            DateTimeOffset? startedAt = null,
            DateTimeOffset? endedAt = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (undoJournal == null)
                throw new ArgumentNullException(nameof(undoJournal));
            if (startedAt.HasValue && endedAt.HasValue && endedAt.Value < startedAt.Value)
                throw new ArgumentException("Playtest end cannot be earlier than its start.", nameof(endedAt));

            Session = session.CreateMemento();
            ExportedAt = exportedAt;
            StartedAt = startedAt;
            EndedAt = endedAt;
            _participants = session.Players
                .OrderBy(player => player.TurnOrder)
                .Select(player =>
                {
                    session.TryGetCharacter(player.Id, out var character);
                    return new PlaytestParticipant(player.Id, player.Name, player.TurnOrder, character?.Id);
                })
                .ToList()
                .AsReadOnly();
            _undoRecords = undoJournal.Records.ToList().AsReadOnly();
        }

        public GameSessionMemento Session { get; }
        public DateTimeOffset ExportedAt { get; }
        public DateTimeOffset? StartedAt { get; }
        public DateTimeOffset? EndedAt { get; }
        public IReadOnlyList<PlaytestParticipant> Participants => _participants;
        public IReadOnlyList<SessionUndoRecord> UndoRecords => _undoRecords;

        public PlaytestExportSummary CreateSummary()
        {
            var annualCycles = Session.Players
                .Where(player => player.AnnualCycle != null)
                .Sum(player => player.AnnualCycle.ProcessedCycles);
            var hospitalizedPlayers = Session.Players
                .Count(player => player.Hospital != null && player.Hospital.IsHospitalized);
            var duration = StartedAt.HasValue && EndedAt.HasValue
                ? EndedAt.Value - StartedAt.Value
                : (TimeSpan?)null;

            return new PlaytestExportSummary(
                Session.SessionId,
                Session.Phase,
                Participants.Count,
                Session.ActionHistory.Count,
                Session.EventHistory.Count,
                Session.EconomyHistory.Count,
                annualCycles,
                hospitalizedPlayers,
                UndoRecords.Count,
                duration,
                ExportedAt);
        }
    }

    public sealed class PlaytestParticipant
    {
        public PlaytestParticipant(PlayerId playerId, string displayName, int turnOrder, string characterId = null)
        {
            PlayerId = playerId;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            TurnOrder = turnOrder;
            CharacterId = characterId;
        }

        public PlayerId PlayerId { get; }
        public string DisplayName { get; }
        public int TurnOrder { get; }
        public string CharacterId { get; }
    }
}
