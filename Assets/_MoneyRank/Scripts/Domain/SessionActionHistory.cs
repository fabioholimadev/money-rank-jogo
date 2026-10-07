using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain
{
    public sealed class SessionActionRecord
    {
        private readonly ReadOnlyCollection<DefinitionParameter> _parameters;

        public SessionActionRecord(
            string id,
            string typeId,
            PlayerId? playerId,
            GamePhase phaseBefore,
            GamePhase phaseAfter,
            DateTimeOffset createdAt,
            string operatorId,
            string sourceId,
            IEnumerable<DefinitionParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Action record ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(typeId))
                throw new ArgumentException("Action type ID cannot be empty.", nameof(typeId));
            if (playerId.HasValue && string.IsNullOrWhiteSpace(playerId.Value.Value))
                throw new ArgumentException("Action player ID cannot be empty.", nameof(playerId));
            if (!Enum.IsDefined(typeof(GamePhase), phaseBefore) || !Enum.IsDefined(typeof(GamePhase), phaseAfter))
                throw new ArgumentException("Action phases must be valid.");
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Action operator ID cannot be empty.", nameof(operatorId));
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Action source ID cannot be empty.", nameof(sourceId));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var values = parameters.ToList();
            if (values.Any(value => value == null))
                throw new ArgumentException("Action parameters cannot contain null entries.", nameof(parameters));
            if (values.Select(value => value.Key).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Action parameter keys must be unique.", nameof(parameters));

            Id = id;
            TypeId = typeId;
            PlayerId = playerId;
            PhaseBefore = phaseBefore;
            PhaseAfter = phaseAfter;
            CreatedAt = createdAt;
            OperatorId = operatorId;
            SourceId = sourceId;
            _parameters = values.AsReadOnly();
        }

        public string Id { get; }
        public string TypeId { get; }
        public PlayerId? PlayerId { get; }
        public GamePhase PhaseBefore { get; }
        public GamePhase PhaseAfter { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
        public string SourceId { get; }
        public IReadOnlyList<DefinitionParameter> Parameters => _parameters;
    }

    public sealed class SessionActionHistory
    {
        private readonly List<SessionActionRecord> _records = new List<SessionActionRecord>();
        private readonly ReadOnlyCollection<SessionActionRecord> _readOnlyRecords;

        public SessionActionHistory()
        {
            _readOnlyRecords = _records.AsReadOnly();
        }

        public IReadOnlyList<SessionActionRecord> Records => _readOnlyRecords;

        public OperationResult Append(SessionActionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (_records.Any(existing => string.Equals(existing.Id, record.Id, StringComparison.Ordinal)))
                return OperationResult.Failure("DUPLICATE_ACTION_ID", $"Action '{record.Id}' already exists.");

            _records.Add(record);
            return OperationResult.Success();
        }

        public void Restore(IEnumerable<SessionActionRecord> records)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            var values = records.ToList();
            if (values.Any(record => record == null))
                throw new ArgumentException("Action history cannot contain null records.", nameof(records));
            if (values.Select(record => record.Id).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Action history IDs must be unique.", nameof(records));

            _records.Clear();
            _records.AddRange(values);
        }
    }
}
