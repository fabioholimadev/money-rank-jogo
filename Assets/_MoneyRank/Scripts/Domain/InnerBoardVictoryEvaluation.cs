using System;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardVictoryEvaluation
    {
        private InnerBoardVictoryEvaluation(bool gameEnded, PlayerId winnerId, string reasonCode)
        {
            GameEnded = gameEnded;
            WinnerId = winnerId;
            ReasonCode = reasonCode;
        }

        public bool GameEnded { get; }
        public PlayerId WinnerId { get; }
        public string ReasonCode { get; }

        public static InnerBoardVictoryEvaluation Continue() =>
            new InnerBoardVictoryEvaluation(false, default, string.Empty);

        public static InnerBoardVictoryEvaluation End(PlayerId winnerId, string reasonCode)
        {
            if (string.IsNullOrWhiteSpace(winnerId.Value))
                throw new ArgumentException("Winner ID cannot be empty.", nameof(winnerId));
            if (string.IsNullOrWhiteSpace(reasonCode))
                throw new ArgumentException("Victory reason code cannot be empty.", nameof(reasonCode));

            return new InnerBoardVictoryEvaluation(true, winnerId, reasonCode);
        }
    }
}
