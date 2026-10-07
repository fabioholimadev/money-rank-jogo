using System;

namespace MoneyRank.Domain.Events
{
    public sealed class ActiveEventSelection
    {
        public ActiveEventSelection(
            PlayerId playerId,
            string spaceId,
            string tableId,
            string policyId,
            string eventId,
            int eventVersion,
            int drawIndex)
        {
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Active event player ID cannot be empty.", nameof(playerId));
            if (string.IsNullOrWhiteSpace(spaceId))
                throw new ArgumentException("Active event space ID cannot be empty.", nameof(spaceId));
            if (string.IsNullOrWhiteSpace(tableId))
                throw new ArgumentException("Active event table ID cannot be empty.", nameof(tableId));
            if (string.IsNullOrWhiteSpace(policyId))
                throw new ArgumentException("Active event policy ID cannot be empty.", nameof(policyId));
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("Active event ID cannot be empty.", nameof(eventId));
            if (eventVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(eventVersion));
            if (drawIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(drawIndex));

            PlayerId = playerId;
            SpaceId = spaceId;
            TableId = tableId;
            PolicyId = policyId;
            EventId = eventId;
            EventVersion = eventVersion;
            DrawIndex = drawIndex;
        }

        public PlayerId PlayerId { get; }
        public string SpaceId { get; }
        public string TableId { get; }
        public string PolicyId { get; }
        public string EventId { get; }
        public int EventVersion { get; }
        public int DrawIndex { get; }
        public EventDefinitionKey EventKey => new EventDefinitionKey(EventId, EventVersion);

        public bool Matches(EventDefinition definition) =>
            definition != null &&
            string.Equals(EventId, definition.Id, StringComparison.Ordinal) &&
            EventVersion == definition.Version;
    }
}
