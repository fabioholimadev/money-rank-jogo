using System;
using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public sealed class InnerBoardResolutionResult
    {
        public InnerBoardResolutionResult(
            InnerBoardMoveResult move,
            InnerBoardCaptureResult capture,
            InnerBoardVictoryEvaluation victory)
        {
            Move = move ?? throw new ArgumentNullException(nameof(move));
            Capture = capture ?? throw new ArgumentNullException(nameof(capture));
            Victory = victory ?? throw new ArgumentNullException(nameof(victory));
        }

        public InnerBoardMoveResult Move { get; }
        public InnerBoardCaptureResult Capture { get; }
        public InnerBoardVictoryEvaluation Victory { get; }
    }
}
