using System;

namespace MoneyRank.Domain
{
    public sealed class SessionRules
    {
        public SessionRules(int minimumPlayers, int maximumPlayers)
        {
            if (minimumPlayers < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumPlayers));
            }

            if (maximumPlayers < minimumPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPlayers));
            }

            MinimumPlayers = minimumPlayers;
            MaximumPlayers = maximumPlayers;
        }

        public int MinimumPlayers { get; }
        public int MaximumPlayers { get; }
    }
}

