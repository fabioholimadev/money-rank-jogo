using System;

namespace MoneyRank.Domain
{
    public sealed class BoardMovementCalculator
    {
        public BoardMoveResult Move(BoardDefinition board, int currentPosition, int steps)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (currentPosition < 0 || currentPosition >= board.SpaceCount)
                throw new ArgumentOutOfRangeException(nameof(currentPosition));
            if (steps < 0) throw new ArgumentOutOfRangeException(nameof(steps));

            var offsetFromStart = PositiveModulo(currentPosition - board.StartIndex, board.SpaceCount);
            var totalOffset = offsetFromStart + steps;
            var completedLaps = totalOffset / board.SpaceCount;
            var destination = PositiveModulo(board.StartIndex + totalOffset, board.SpaceCount);

            return new BoardMoveResult(currentPosition, destination, steps, completedLaps, board.GetSpace(destination));
        }

        private static int PositiveModulo(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    public readonly struct BoardMoveResult
    {
        public BoardMoveResult(int origin, int destination, int steps, int completedLaps, BoardSpaceDefinition destinationSpace)
        {
            Origin = origin;
            Destination = destination;
            Steps = steps;
            CompletedLaps = completedLaps;
            DestinationSpace = destinationSpace;
        }

        public int Origin { get; }
        public int Destination { get; }
        public int Steps { get; }
        public int CompletedLaps { get; }
        public BoardSpaceDefinition DestinationSpace { get; }
    }
}
