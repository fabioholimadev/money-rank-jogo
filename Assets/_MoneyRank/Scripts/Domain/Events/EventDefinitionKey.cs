using System;

namespace MoneyRank.Domain.Events
{
    public readonly struct EventDefinitionKey : IEquatable<EventDefinitionKey>
    {
        public EventDefinitionKey(string eventId, int version)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
            if (version <= 0)
                throw new ArgumentOutOfRangeException(nameof(version), "Event version must be positive.");

            EventId = eventId;
            Version = version;
        }

        public string EventId { get; }
        public int Version { get; }

        public bool Equals(EventDefinitionKey other) =>
            string.Equals(EventId, other.EventId, StringComparison.Ordinal) && Version == other.Version;

        public override bool Equals(object obj) => obj is EventDefinitionKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((EventId != null ? StringComparer.Ordinal.GetHashCode(EventId) : 0) * 397) ^ Version;
            }
        }

        public override string ToString() => $"{EventId}@{Version}";

        public static bool operator ==(EventDefinitionKey left, EventDefinitionKey right) => left.Equals(right);
        public static bool operator !=(EventDefinitionKey left, EventDefinitionKey right) => !left.Equals(right);
    }
}
