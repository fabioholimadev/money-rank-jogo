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
        private readonly Dictionary<PlayerId, ExternalBoardState> _boardStates =
            new Dictionary<PlayerId, ExternalBoardState>();
        private readonly BoardMovementCalculator _movementCalculator = new BoardMovementCalculator();
        private int _currentPlayerIndex = -1;

        public GameSession(SessionId id, SessionRules rules, GameFlowStateMachine stateMachine)
            : this(id, rules, stateMachine, null)
        {
        }

        public GameSession(
            SessionId id,
            SessionRules rules,
            GameFlowStateMachine stateMachine,
            BoardDefinition board)
        {
            Id = id;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            Board = board;
            _readOnlyPlayers = _players.AsReadOnly();
            Phase = GamePhase.Setup;
        }

        public SessionId Id { get; }
        public SessionRules Rules { get; }
        public BoardDefinition Board { get; }
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
            if (Board != null)
            {
                _boardStates.Add(player.Id, new ExternalBoardState(player.Id, Board));
            }

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

        public bool TryGetBoardState(PlayerId playerId, out ExternalBoardState boardState) =>
            _boardStates.TryGetValue(playerId, out boardState);

        public OperationResult<BoardMoveResult> RegisterMove(int steps)
        {
            if (Phase != GamePhase.AwaitMove)
            {
                return OperationResult<BoardMoveResult>.Failure(
                    "MOVE_NOT_ALLOWED",
                    "A move can only be registered during AwaitMove.");
            }

            if (Board == null)
            {
                return OperationResult<BoardMoveResult>.Failure(
                    "BOARD_NOT_CONFIGURED",
                    "The session requires a board definition before registering moves.");
            }

            if (steps < 0)
            {
                return OperationResult<BoardMoveResult>.Failure(
                    "INVALID_MOVE_STEPS",
                    "Move steps cannot be negative.");
            }

            if (CurrentPlayer == null || !_boardStates.TryGetValue(CurrentPlayer.Id, out var boardState))
            {
                return OperationResult<BoardMoveResult>.Failure(
                    "PLAYER_BOARD_STATE_NOT_FOUND",
                    "The current player does not have an external board state.");
            }

            var transition = TryTransition(GamePhase.ResolveSpace);
            if (!transition.Succeeded)
            {
                return OperationResult<BoardMoveResult>.Failure(transition.ErrorCode, transition.Message);
            }

            var move = boardState.Move(Board, steps, _movementCalculator);
            return OperationResult<BoardMoveResult>.Success(move);
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

