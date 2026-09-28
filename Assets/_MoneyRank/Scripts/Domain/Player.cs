using System;

namespace MoneyRank.Domain
{
    public sealed class Player
    {
        public Player(PlayerId id, string name, int turnOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Player name cannot be empty.", nameof(name));
            }

            if (turnOrder < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turnOrder));
            }

            Id = id;
            Name = name;
            TurnOrder = turnOrder;
        }

        public PlayerId Id { get; }
        public string Name { get; }
        public int TurnOrder { get; }
    }
}

