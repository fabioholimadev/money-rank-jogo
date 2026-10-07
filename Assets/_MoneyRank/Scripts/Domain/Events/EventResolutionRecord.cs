using System;

namespace MoneyRank.Domain.Events
{
    public sealed class EventResolutionRecord
    {
        public EventResolutionRecord(
            string id,
            string eventId,
            int eventVersion,
            PlayerId playerId,
            string spaceId,
            string choiceId,
            EventStateSnapshot beforeState,
            EventStateSnapshot afterState,
            string sourceId,
            DateTimeOffset createdAt,
            string operatorId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Resolution ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("Resolution event ID cannot be empty.", nameof(eventId));
            if (eventVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(eventVersion));
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Resolution player ID cannot be empty.", nameof(playerId));
            if (string.IsNullOrWhiteSpace(spaceId))
                throw new ArgumentException("Resolution space ID cannot be empty.", nameof(spaceId));
            if (string.IsNullOrWhiteSpace(choiceId))
                throw new ArgumentException("Resolution choice ID cannot be empty.", nameof(choiceId));
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Resolution source ID cannot be empty.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Resolution operator ID cannot be empty.", nameof(operatorId));

            Id = id;
            EventId = eventId;
            EventVersion = eventVersion;
            PlayerId = playerId;
            SpaceId = spaceId;
            ChoiceId = choiceId;
            BeforeState = beforeState ?? throw new ArgumentNullException(nameof(beforeState));
            AfterState = afterState ?? throw new ArgumentNullException(nameof(afterState));
            SourceId = sourceId;
            CreatedAt = createdAt;
            OperatorId = operatorId;
        }

        public string Id { get; }
        public string EventId { get; }
        public int EventVersion { get; }
        public PlayerId PlayerId { get; }
        public string SpaceId { get; }
        public string ChoiceId { get; }
        public EventStateSnapshot BeforeState { get; }
        public EventStateSnapshot AfterState { get; }
        public string SourceId { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
    }
}
