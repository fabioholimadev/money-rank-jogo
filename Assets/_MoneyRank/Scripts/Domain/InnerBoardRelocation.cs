using System;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardRelocation
    {
        public InnerBoardRelocation(PlayerId playerId, int destination, int checkpointIndex)
        {
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Relocation player ID cannot be empty.", nameof(playerId));
            if (destination < 0)
                throw new ArgumentOutOfRangeException(nameof(destination));
            if (checkpointIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex));

            PlayerId = playerId;
            Destination = destination;
            CheckpointIndex = checkpointIndex;
        }

        public PlayerId PlayerId { get; }
        public int Destination { get; }
        public int CheckpointIndex { get; }
    }
}
