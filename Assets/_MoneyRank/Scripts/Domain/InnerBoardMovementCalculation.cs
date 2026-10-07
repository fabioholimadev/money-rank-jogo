using System;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardMovementCalculation
    {
        public InnerBoardMovementCalculation(int destination, int checkpointIndex)
        {
            if (destination < 0)
                throw new ArgumentOutOfRangeException(nameof(destination));
            if (checkpointIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex));

            Destination = destination;
            CheckpointIndex = checkpointIndex;
        }

        public int Destination { get; }
        public int CheckpointIndex { get; }
    }
}
