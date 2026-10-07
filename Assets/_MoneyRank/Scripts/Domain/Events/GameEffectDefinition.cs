using System;

namespace MoneyRank.Domain.Events
{
    public sealed class GameEffectDefinition
    {
        public GameEffectDefinition(
            string targetId,
            string operationId,
            string value,
            string durationId,
            string sourceId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("Effect target ID cannot be empty.", nameof(targetId));
            if (string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentException("Effect operation ID cannot be empty.", nameof(operationId));
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Effect source ID cannot be empty.", nameof(sourceId));

            TargetId = targetId;
            OperationId = operationId;
            Value = value;
            DurationId = durationId;
            SourceId = sourceId;
        }

        public string TargetId { get; }
        public string OperationId { get; }
        public string Value { get; }
        public string DurationId { get; }
        public string SourceId { get; }
    }
}
