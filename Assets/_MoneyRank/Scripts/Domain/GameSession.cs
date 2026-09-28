using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class GameSession
    {
        private readonly List<Player> _players = new List<Player>();
        private readonly ReadOnlyCollection<Player> _readOnlyPlayers;
        private readonly GameFlowStateMachine _stateMachine;
        private int _currentPlayerIndex = -1;

        public GameSession(SessionId id, SessionRules rules, GameFlowStateMachine stateMachine)
        {
            Id = id;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _readOnlyPlayers = _players.AsReadOnly();
            Phase = GamePhase.Setup;
        }

        public SessionId Id { get; }
        public SessionRules Rules { get; }
        public GamePhase Phase { get; private set; }
        public IReadOnlyList<Player> Players => _readOnlyPlayers;
        public Player CurrentPlayer => _currentPlayerIndex < 0 ? null : _players[_currentPlayerIndex];

        public OperationResult AddPlayer(Player player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (Phase != GamePhase.Setup)
            {
                return OperationResult.Failure("SESSION_ALREADY_STARTED", "Players can only be added during setup.");
            }

            if (_players.Count >= Rules.MaximumPlayers)
            {
                return OperationResult.Failure("PLAYER_LIMIT_REACHED", "The session has reached its configured player limit.");
            }

            if (_players.Any(existing => existing.Id == player.Id))
            {
                return OperationResult.Failure("DUPLICATE_PLAYER_ID", "Player IDs must be unique within a session.");
            }

            if (_players.Any(existing => existing.TurnOrder == player.TurnOrder))
            {
                return OperationResult.Failure("DUPLICATE_TURN_ORDER", "Turn order values must be unique within a session.");
            }

            _players.Add(player);
            return OperationResult.Success();
        }

        public OperationResult Start()
        {
            if (_players.Count < Rules.MinimumPlayers)
            {
                return OperationResult.Failure("NOT_ENOUGH_PLAYERS", "The configured minimum number of players is required.");
            }

            var transition = TryTransition(GamePhase.StartTurn);
            if (!transition.Succeeded)
            {
                return transition;
            }

            _players.Sort((left, right) => left.TurnOrder.CompareTo(right.TurnOrder));
            _currentPlayerIndex = 0;
            return OperationResult.Success();
        }

        public OperationResult TryTransition(GamePhase nextPhase)
        {
            if (!_stateMachine.CanTransition(Phase, nextPhase))
            {
                return OperationResult.Failure(
                    "INVALID_PHASE_TRANSITION",
                    $"Transition from {Phase} to {nextPhase} is not allowed.");
            }

            Phase = nextPhase;
            return OperationResult.Success();
        }

        public OperationResult CompleteTurn()
        {
            if (Phase != GamePhase.EndTurn)
            {
                return OperationResult.Failure("TURN_NOT_READY_TO_COMPLETE", "A turn can only complete during EndTurn.");
            }

            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
            Phase = GamePhase.StartTurn;
            return OperationResult.Success();
        }
    }
}

