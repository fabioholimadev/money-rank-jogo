using System;

namespace MoneyRank.Domain
{
    public sealed class GameResult
    {
        public GameResult(PlayerId winnerId, string reasonCode)
        {
            if (string.IsNullOrWhiteSpace(winnerId.Value))
                throw new ArgumentException("Winner ID cannot be empty.", nameof(winnerId));
            if (string.IsNullOrWhiteSpace(reasonCode))
                throw new ArgumentException("Result reason code cannot be empty.", nameof(reasonCode));

            WinnerId = winnerId;
            ReasonCode = reasonCode;
        }

        public PlayerId WinnerId { get; }
        public string ReasonCode { get; }
    }
}
