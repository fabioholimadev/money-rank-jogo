using System;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardState
    {
        public InnerBoardState(PlayerId playerId, int position, int checkpointIndex)
        {
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Inner board player ID cannot be empty.", nameof(playerId));
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));
            if (checkpointIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex));

            PlayerId = playerId;
            Position = position;
            CheckpointIndex = checkpointIndex;
        }

        public PlayerId PlayerId { get; }
        public int Position { get; private set; }
        public int CheckpointIndex { get; private set; }

        public void Apply(InnerBoardMovementCalculation calculation)
        {
            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));

            Position = calculation.Destination;
            CheckpointIndex = calculation.CheckpointIndex;
        }

        public void Restore(int position, int checkpointIndex)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));
            if (checkpointIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex));

            Position = position;
            CheckpointIndex = checkpointIndex;
        }
    }
}
