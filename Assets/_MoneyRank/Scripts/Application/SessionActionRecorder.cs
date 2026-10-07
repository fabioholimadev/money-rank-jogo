using System;
using System.Collections.Generic;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class SessionActionRecorder
    {
        public SessionActionRecorder(string sourceId, string operatorId)
        {
            SourceId = string.IsNullOrWhiteSpace(sourceId)
                ? throw new ArgumentException("Action source ID cannot be empty.", nameof(sourceId))
                : sourceId;
            OperatorId = string.IsNullOrWhiteSpace(operatorId)
                ? throw new ArgumentException("Action operator ID cannot be empty.", nameof(operatorId))
                : operatorId;
        }

        public string SourceId { get; }
        public string OperatorId { get; }

        public SessionActionRecord Record(
            GameSession session,
            string typeId,
            PlayerId? playerId,
            GamePhase phaseBefore,
            IEnumerable<DefinitionParameter> parameters = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var record = new SessionActionRecord(
                Guid.NewGuid().ToString("N"),
                typeId,
                playerId,
                phaseBefore,
                session.Phase,
                DateTimeOffset.UtcNow,
                OperatorId,
                SourceId,
                parameters ?? Array.Empty<DefinitionParameter>());
            var appended = session.ActionHistory.Append(record);
            if (!appended.Succeeded)
                throw new InvalidOperationException(appended.Message);
            return record;
        }
    }
}
