using System;

namespace MoneyRank.Domain
{
    public sealed class ExternalBoardState
    {
        public ExternalBoardState(PlayerId playerId, BoardDefinition board)
        {
            PlayerId = playerId;
            BoardId = board?.Id ?? throw new ArgumentNullException(nameof(board));
            Position = board.StartIndex;
        }

        public PlayerId PlayerId { get; }
        public string BoardId { get; }
        public int Position { get; private set; }
        public int CompletedLaps { get; private set; }

        public BoardMoveResult Move(BoardDefinition board, int steps, BoardMovementCalculator calculator)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (calculator == null) throw new ArgumentNullException(nameof(calculator));
            if (!string.Equals(BoardId, board.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Board state cannot be moved using a different board definition.");

            var result = calculator.Move(board, Position, steps);
            Position = result.Destination;
            CompletedLaps += result.CompletedLaps;
            return result;
        }
    }
}
