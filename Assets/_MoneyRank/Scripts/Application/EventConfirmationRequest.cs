using System;

namespace MoneyRank.Application
{
    public sealed class EventConfirmationRequest
    {
        public EventConfirmationRequest(
            string resolutionId,
            string spaceId,
            string sourceId,
            DateTimeOffset createdAt,
            string operatorId)
        {
            if (string.IsNullOrWhiteSpace(resolutionId))
                throw new ArgumentException("Resolution ID cannot be empty.", nameof(resolutionId));
            if (string.IsNullOrWhiteSpace(spaceId))
                throw new ArgumentException("Space ID cannot be empty.", nameof(spaceId));
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Source ID cannot be empty.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(operatorId))
                throw new ArgumentException("Operator ID cannot be empty.", nameof(operatorId));

            ResolutionId = resolutionId;
            SpaceId = spaceId;
            SourceId = sourceId;
            CreatedAt = createdAt;
            OperatorId = operatorId;
        }

        public string ResolutionId { get; }
        public string SpaceId { get; }
        public string SourceId { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
    }
}
