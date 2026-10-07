using System;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardMoveResult
    {
        public InnerBoardMoveResult(
            PlayerId playerId,
            int origin,
            int destination,
            int checkpointIndex,
            bool reachedGoal)
        {
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Inner board player ID cannot be empty.", nameof(playerId));

            PlayerId = playerId;
            Origin = origin;
            Destination = destination;
            CheckpointIndex = checkpointIndex;
            ReachedGoal = reachedGoal;
        }

        public PlayerId PlayerId { get; }
        public int Origin { get; }
        public int Destination { get; }
        public int CheckpointIndex { get; }
        public bool ReachedGoal { get; }
    }
}
