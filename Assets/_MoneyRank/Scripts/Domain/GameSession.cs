using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Characters;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using MoneyRank.Domain.Hospital;

namespace MoneyRank.Domain
{
    public sealed class GameSession
    {
        public const int CurrentSaveSchemaVersion = 7;
        public const int MinimumSupportedSaveSchemaVersion = 4;

        private readonly List<Player> _players = new List<Player>();
        private readonly ReadOnlyCollection<Player> _readOnlyPlayers;
        private readonly GameFlowStateMachine _stateMachine;
        private readonly Dictionary<PlayerId, ExternalBoardState> _boardStates =
            new Dictionary<PlayerId, ExternalBoardState>();
        private readonly Dictionary<PlayerId, EventState> _eventStates =
            new Dictionary<PlayerId, EventState>();
        private readonly Dictionary<PlayerId, Dictionary<string, AttributeState>> _attributeStates =
            new Dictionary<PlayerId, Dictionary<string, AttributeState>>();
        private readonly Dictionary<PlayerId, InnerBoardState> _innerBoardStates =
            new Dictionary<PlayerId, InnerBoardState>();
        private readonly Dictionary<PlayerId, AnnualCycleState> _annualCycleStates =
            new Dictionary<PlayerId, AnnualCycleState>();
        private readonly Dictionary<PlayerId, HospitalState> _hospitalStates =
            new Dictionary<PlayerId, HospitalState>();
        private readonly ReadOnlyCollection<AttributeDefinition> _attributeDefinitions;
        private readonly Dictionary<string, CharacterDefinition> _characterDefinitions;
        private readonly ReadOnlyCollection<CharacterDefinition> _readOnlyCharacterDefinitions;
        private readonly Dictionary<PlayerId, string> _characterAssignments =
            new Dictionary<PlayerId, string>();
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
            BoardDefinition board,
            InnerBoardDefinition innerBoard = null,
            IEnumerable<AttributeDefinition> attributeDefinitions = null,
            AnnualCycleDefinition annualCycle = null,
            HospitalDefinition hospital = null,
            EconomyDefinition economy = null,
            IEnumerable<CharacterDefinition> characterDefinitions = null)
        {
            Id = id;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            Board = board;
            InnerBoard = innerBoard;
            AnnualCycle = annualCycle;
            Hospital = hospital;
            Economy = economy;
            EconomyState = Economy?.CreateState();
            EconomyHistory = new EconomyTransactionHistory();
            if (AnnualCycle != null && Board == null)
                throw new ArgumentException("An annual cycle requires an external board definition.", nameof(annualCycle));
            var definitions = (attributeDefinitions ?? Array.Empty<AttributeDefinition>()).ToList();
            if (definitions.Any(definition => definition == null))
                throw new ArgumentException("Attribute definitions cannot contain null entries.", nameof(attributeDefinitions));
            if (definitions.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count() != definitions.Count)
                throw new ArgumentException("Attribute definition IDs must be unique within a session.", nameof(attributeDefinitions));
            _attributeDefinitions = definitions.AsReadOnly();
            var characters = (characterDefinitions ?? Array.Empty<CharacterDefinition>()).ToList();
            if (characters.Any(character => character == null))
                throw new ArgumentException("Character definitions cannot contain null entries.", nameof(characterDefinitions));
            if (characters.Select(character => character.Id).Distinct(StringComparer.Ordinal).Count() != characters.Count)
                throw new ArgumentException("Character definition IDs must be unique within a session.", nameof(characterDefinitions));
            _characterDefinitions = characters.ToDictionary(character => character.Id, StringComparer.Ordinal);
            _readOnlyCharacterDefinitions = characters.AsReadOnly();
            EventHistory = new EventResolutionHistory();
            ActionHistory = new SessionActionHistory();
            _readOnlyPlayers = _players.AsReadOnly();
            Phase = GamePhase.Setup;
        }

        public SessionId Id { get; }
        public SessionRules Rules { get; }
        public BoardDefinition Board { get; }
        public InnerBoardDefinition InnerBoard { get; }
        public AnnualCycleDefinition AnnualCycle { get; }
        public HospitalDefinition Hospital { get; }
        public EconomyDefinition Economy { get; }
        public EconomyState EconomyState { get; }
        public EconomyTransactionHistory EconomyHistory { get; }
        public IReadOnlyList<AttributeDefinition> AttributeDefinitions => _attributeDefinitions;
        public IReadOnlyList<CharacterDefinition> CharacterDefinitions => _readOnlyCharacterDefinitions;
        public GamePhase Phase { get; private set; }
        public IReadOnlyList<Player> Players => _readOnlyPlayers;
        public Player CurrentPlayer => _currentPlayerIndex < 0 ? null : _players[_currentPlayerIndex];
        public EventResolutionHistory EventHistory { get; }
        public ActiveEventSelection ActiveEvent { get; private set; }
        public GameResult Result { get; private set; }
        public SessionActionHistory ActionHistory { get; }

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
            _eventStates.Add(player.Id, new EventState(Array.Empty<DefinitionParameter>()));
            _attributeStates.Add(
                player.Id,
                _attributeDefinitions.ToDictionary(
                    definition => definition.Id,
                    definition => new AttributeState(definition.Id),
                    StringComparer.Ordinal));
            if (Board != null)
            {
                _boardStates.Add(player.Id, new ExternalBoardState(player.Id, Board));
            }
            if (InnerBoard != null)
            {
                _innerBoardStates.Add(
                    player.Id,
                    new InnerBoardState(player.Id, InnerBoard.StartIndex, InnerBoard.StartIndex));
            }
            if (AnnualCycle != null)
            {
                _annualCycleStates.Add(player.Id, new AnnualCycleState(player.Id));
            }
            if (Hospital != null)
            {
                _hospitalStates.Add(player.Id, new HospitalState(player.Id));
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

        public OperationResult AssignCharacter(PlayerId playerId, string characterId)
        {
            if (Phase != GamePhase.Setup)
                return OperationResult.Failure("CHARACTER_ASSIGNMENT_CLOSED", "Characters can only be assigned during setup.");
            if (_players.All(player => player.Id != playerId))
                return OperationResult.Failure("CHARACTER_PLAYER_NOT_FOUND", "The character target player was not found.");
            if (string.IsNullOrWhiteSpace(characterId) || !_characterDefinitions.ContainsKey(characterId))
                return OperationResult.Failure("CHARACTER_NOT_FOUND", "The configured character was not found.");

            _characterAssignments[playerId] = characterId;
            return OperationResult.Success();
        }

        public bool TryGetCharacter(PlayerId playerId, out CharacterDefinition character)
        {
            character = null;
            return _characterAssignments.TryGetValue(playerId, out var characterId) &&
                   _characterDefinitions.TryGetValue(characterId, out character);
        }

        public bool TryGetBoardState(PlayerId playerId, out ExternalBoardState boardState) =>
            _boardStates.TryGetValue(playerId, out boardState);

        public bool TryGetEventState(PlayerId playerId, out EventState eventState) =>
            _eventStates.TryGetValue(playerId, out eventState);

        public bool TryGetAttributeState(
            PlayerId playerId,
            string attributeId,
            out AttributeState attributeState)
        {
            attributeState = null;
            return !string.IsNullOrWhiteSpace(attributeId) &&
                   _attributeStates.TryGetValue(playerId, out var states) &&
                   states.TryGetValue(attributeId, out attributeState);
        }

        public bool TryGetInnerBoardState(PlayerId playerId, out InnerBoardState innerBoardState) =>
            _innerBoardStates.TryGetValue(playerId, out innerBoardState);

        public bool TryGetAnnualCycleState(PlayerId playerId, out AnnualCycleState annualCycleState) =>
            _annualCycleStates.TryGetValue(playerId, out annualCycleState);

        public bool TryGetHospitalState(PlayerId playerId, out HospitalState hospitalState) =>
            _hospitalStates.TryGetValue(playerId, out hospitalState);

        public IReadOnlyList<InnerBoardState> GetInnerBoardStates() =>
            _players
                .Where(player => _innerBoardStates.ContainsKey(player.Id))
                .Select(player => _innerBoardStates[player.Id])
                .ToList()
                .AsReadOnly();

        public GameSessionMemento CreateMemento()
        {
            var playerStates = _players.Select(player =>
            {
                var externalBoard = _boardStates.TryGetValue(player.Id, out var boardState)
                    ? new ExternalBoardStateMemento(boardState.Position, boardState.CompletedLaps)
                    : null;
                var attributes = _attributeStates[player.Id]
                    .Values
                    .OrderBy(state => state.AttributeId, StringComparer.Ordinal)
                    .Select(state => new AttributeStateMemento(
                        state.AttributeId,
                        state.CurrentProgress,
                        state.CompletedCycles));
                var innerBoard = _innerBoardStates.TryGetValue(player.Id, out var innerState)
                    ? new InnerBoardStateMemento(innerState.Position, innerState.CheckpointIndex)
                    : null;
                var annualCycle = _annualCycleStates.TryGetValue(player.Id, out var annualCycleState)
                    ? new AnnualCycleStateMemento(annualCycleState.ProcessedCycles)
                    : null;
                var hospital = _hospitalStates.TryGetValue(player.Id, out var hospitalState)
                    ? new HospitalStateMemento(
                        hospitalState.IsHospitalized,
                        hospitalState.PendingMovementSkips,
                        hospitalState.ReasonId)
                    : null;
                return new PlayerStateMemento(
                    player.Id,
                    externalBoard,
                    _eventStates[player.Id].CreateSnapshot(),
                    attributes,
                    innerBoard,
                    annualCycle,
                    hospital,
                    _characterAssignments.TryGetValue(player.Id, out var characterId) ? characterId : null);
            });

            return new GameSessionMemento(
                CurrentSaveSchemaVersion,
                Id,
                Phase,
                CurrentPlayer?.Id,
                playerStates,
                EventHistory.Records,
                EconomyState == null
                    ? null
                    : new EconomyStateMemento(
                        EconomyState.CreateSnapshot().Balances
                            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => new EconomyBalanceDefinition(pair.Key, pair.Value))),
                EconomyHistory.Records,
                ActiveEvent,
                Result,
                ActionHistory.Records);
        }

        public OperationResult Restore(GameSessionMemento memento)
        {
            if (memento == null)
                throw new ArgumentNullException(nameof(memento));
            if (memento.SchemaVersion < MinimumSupportedSaveSchemaVersion ||
                memento.SchemaVersion > CurrentSaveSchemaVersion)
            {
                return OperationResult.Failure(
                    "SAVE_SCHEMA_UNSUPPORTED",
                    $"Save schema {memento.SchemaVersion} is not supported.");
            }
            if (memento.SessionId != Id)
                return OperationResult.Failure("SAVE_SESSION_MISMATCH", "The save belongs to a different session.");
            if (!Enum.IsDefined(typeof(GamePhase), memento.Phase))
                return OperationResult.Failure("SAVE_PHASE_INVALID", "The save contains an invalid game phase.");
            if (memento.Players.Count != _players.Count ||
                memento.Players.Any(saved => _players.All(player => player.Id != saved.PlayerId)))
            {
                return OperationResult.Failure(
                    "SAVE_PLAYERS_MISMATCH",
                    "The save player set does not match the session.");
            }
            if (memento.Phase != GamePhase.Setup && !memento.CurrentPlayerId.HasValue)
                return OperationResult.Failure("SAVE_CURRENT_PLAYER_MISSING", "A started save requires a current player.");
            if (memento.CurrentPlayerId.HasValue &&
                _players.All(player => player.Id != memento.CurrentPlayerId.Value))
            {
                return OperationResult.Failure("SAVE_CURRENT_PLAYER_INVALID", "The saved current player was not found.");
            }
            if (memento.Phase == GamePhase.AwaitChoice && memento.ActiveEvent == null)
                return OperationResult.Failure("SAVE_ACTIVE_EVENT_MISSING", "AwaitChoice requires an active event selection.");
            if (memento.Phase != GamePhase.AwaitChoice && memento.ActiveEvent != null)
                return OperationResult.Failure("SAVE_ACTIVE_EVENT_UNEXPECTED", "An active event is only valid during AwaitChoice.");
            if (memento.ActiveEvent != null)
            {
                if (!memento.CurrentPlayerId.HasValue || memento.ActiveEvent.PlayerId != memento.CurrentPlayerId.Value)
                    return OperationResult.Failure("SAVE_ACTIVE_EVENT_PLAYER_INVALID", "The active event player is not current.");
                var activePlayerState = memento.Players.FirstOrDefault(
                    player => player.PlayerId == memento.ActiveEvent.PlayerId);
                if (Board == null || activePlayerState?.ExternalBoard == null ||
                    activePlayerState.ExternalBoard.Position < 0 ||
                    activePlayerState.ExternalBoard.Position >= Board.SpaceCount)
                    return OperationResult.Failure("SAVE_ACTIVE_EVENT_BOARD_INVALID", "The active event requires a board state.");

                var activeSpace = Board.GetSpace(activePlayerState.ExternalBoard.Position);
                if (!string.Equals(activeSpace.Id, memento.ActiveEvent.SpaceId, StringComparison.Ordinal) ||
                    !string.Equals(activeSpace.EventTableId, memento.ActiveEvent.TableId, StringComparison.Ordinal))
                {
                    return OperationResult.Failure(
                        "SAVE_ACTIVE_EVENT_SPACE_INVALID",
                        "The active event does not match the current board space.");
                }
            }
            var resultPhase = memento.Phase == GamePhase.GameEnd || memento.Phase == GamePhase.Results;
            if (resultPhase && memento.Result == null)
                return OperationResult.Failure("SAVE_RESULT_MISSING", "A finished session requires a game result.");
            if (!resultPhase && memento.Result != null)
                return OperationResult.Failure("SAVE_RESULT_UNEXPECTED", "A game result is only valid after the game ends.");
            if (memento.Result != null && _players.All(player => player.Id != memento.Result.WinnerId))
                return OperationResult.Failure("SAVE_RESULT_WINNER_INVALID", "The saved winner was not found in the session.");

            foreach (var savedPlayer in memento.Players)
            {
                if ((Board == null) != (savedPlayer.ExternalBoard == null))
                    return OperationResult.Failure("SAVE_EXTERNAL_BOARD_MISMATCH", "External board state does not match the session definition.");
                if (savedPlayer.ExternalBoard != null &&
                    (savedPlayer.ExternalBoard.Position < 0 ||
                     savedPlayer.ExternalBoard.Position >= Board.SpaceCount ||
                     savedPlayer.ExternalBoard.CompletedLaps < 0))
                {
                    return OperationResult.Failure("SAVE_EXTERNAL_BOARD_INVALID", "External board state is invalid.");
                }

                var configuredAttributes = _attributeStates[savedPlayer.PlayerId];
                if (savedPlayer.Attributes.Count != configuredAttributes.Count ||
                    savedPlayer.Attributes.Any(attribute =>
                        !configuredAttributes.ContainsKey(attribute.AttributeId) ||
                        attribute.CurrentProgress < 0 ||
                        attribute.CompletedCycles < 0))
                {
                    return OperationResult.Failure("SAVE_ATTRIBUTES_INVALID", "Saved attributes do not match the session definition.");
                }

                if ((InnerBoard == null) != (savedPlayer.InnerBoard == null))
                    return OperationResult.Failure("SAVE_INNER_BOARD_MISMATCH", "Inner board state does not match the session definition.");
                if (savedPlayer.InnerBoard != null)
                {
                    var checkpoint = InnerBoard.GetSpace(savedPlayer.InnerBoard.CheckpointIndex);
                    if (InnerBoard.GetSpace(savedPlayer.InnerBoard.Position) == null ||
                        checkpoint == null ||
                        (savedPlayer.InnerBoard.CheckpointIndex != InnerBoard.StartIndex && !checkpoint.IsCheckpoint))
                    {
                        return OperationResult.Failure("SAVE_INNER_BOARD_INVALID", "Saved inner board state is invalid.");
                    }
                }

                if ((AnnualCycle == null) != (savedPlayer.AnnualCycle == null))
                    return OperationResult.Failure("SAVE_ANNUAL_CYCLE_MISMATCH", "Annual cycle state does not match the session definition.");
                if (savedPlayer.AnnualCycle != null &&
                    (savedPlayer.AnnualCycle.ProcessedCycles < 0 ||
                     savedPlayer.AnnualCycle.ProcessedCycles >
                     savedPlayer.ExternalBoard.CompletedLaps / AnnualCycle.CompletedLapsPerCycle))
                {
                    return OperationResult.Failure("SAVE_ANNUAL_CYCLE_INVALID", "Saved annual cycle state is invalid.");
                }

                if ((Hospital == null) != (savedPlayer.Hospital == null))
                    return OperationResult.Failure("SAVE_HOSPITAL_MISMATCH", "Hospital state does not match the session definition.");
                if (savedPlayer.Hospital != null &&
                    (savedPlayer.Hospital.PendingMovementSkips < 0 ||
                     (savedPlayer.Hospital.IsHospitalized && string.IsNullOrWhiteSpace(savedPlayer.Hospital.ReasonId)) ||
                     (!savedPlayer.Hospital.IsHospitalized &&
                      (savedPlayer.Hospital.PendingMovementSkips != 0 || !string.IsNullOrEmpty(savedPlayer.Hospital.ReasonId)))))
                {
                    return OperationResult.Failure("SAVE_HOSPITAL_INVALID", "Saved hospital state is invalid.");
                }
                if (!string.IsNullOrEmpty(savedPlayer.CharacterId) &&
                    !_characterDefinitions.ContainsKey(savedPlayer.CharacterId))
                {
                    return OperationResult.Failure("SAVE_CHARACTER_INVALID", "Saved character does not match the session definitions.");
                }
            }

            try
            {
                var historyValidation = new EventResolutionHistory();
                historyValidation.Restore(memento.EventHistory);
                var actionHistoryValidation = new SessionActionHistory();
                actionHistoryValidation.Restore(memento.ActionHistory);
            }
            catch (ArgumentException exception)
            {
                return OperationResult.Failure("SAVE_EVENT_HISTORY_INVALID", exception.Message);
            }

            if ((Economy == null) != (memento.Economy == null))
                return OperationResult.Failure("SAVE_ECONOMY_MISMATCH", "Economy state does not match the session definition.");
            if (Economy == null && memento.EconomyHistory.Count != 0)
                return OperationResult.Failure("SAVE_ECONOMY_HISTORY_MISMATCH", "A session without economy cannot restore economy history.");
            if (Economy != null)
            {
                try
                {
                    MoneyRank.Domain.Economy.EconomyState.FromSnapshot(Economy.Accounts, memento.Economy.ToSnapshot());
                    foreach (var record in memento.EconomyHistory)
                    {
                        MoneyRank.Domain.Economy.EconomyState.FromSnapshot(Economy.Accounts, record.BeforeState);
                        MoneyRank.Domain.Economy.EconomyState.FromSnapshot(Economy.Accounts, record.AfterState);
                        if (record.Entries.Any(entry => Economy.Accounts.All(account => account.Id != entry.AccountId)))
                            throw new ArgumentException("Economy history references an unknown account.");
                    }
                    var historyValidation = new EconomyTransactionHistory();
                    historyValidation.Restore(memento.EconomyHistory);
                }
                catch (ArgumentException exception)
                {
                    return OperationResult.Failure("SAVE_ECONOMY_INVALID", exception.Message);
                }
            }

            foreach (var savedPlayer in memento.Players)
            {
                if (savedPlayer.ExternalBoard != null)
                {
                    _boardStates[savedPlayer.PlayerId].Restore(
                        Board,
                        savedPlayer.ExternalBoard.Position,
                        savedPlayer.ExternalBoard.CompletedLaps);
                }
                _eventStates[savedPlayer.PlayerId].Restore(savedPlayer.EventState);
                foreach (var attribute in savedPlayer.Attributes)
                {
                    _attributeStates[savedPlayer.PlayerId][attribute.AttributeId].Restore(
                        attribute.CurrentProgress,
                        attribute.CompletedCycles);
                }
                if (savedPlayer.InnerBoard != null)
                {
                    _innerBoardStates[savedPlayer.PlayerId].Restore(
                        savedPlayer.InnerBoard.Position,
                        savedPlayer.InnerBoard.CheckpointIndex);
                }
                if (savedPlayer.AnnualCycle != null)
                {
                    _annualCycleStates[savedPlayer.PlayerId].Restore(
                        savedPlayer.AnnualCycle.ProcessedCycles);
                }
                if (savedPlayer.Hospital != null)
                {
                    _hospitalStates[savedPlayer.PlayerId].Restore(
                        savedPlayer.Hospital.IsHospitalized,
                        savedPlayer.Hospital.PendingMovementSkips,
                        savedPlayer.Hospital.ReasonId);
                }
                if (string.IsNullOrEmpty(savedPlayer.CharacterId))
                    _characterAssignments.Remove(savedPlayer.PlayerId);
                else
                    _characterAssignments[savedPlayer.PlayerId] = savedPlayer.CharacterId;
            }

            EventHistory.Restore(memento.EventHistory);
            ActionHistory.Restore(memento.ActionHistory);
            if (EconomyState != null)
            {
                EconomyState.Restore(memento.Economy.ToSnapshot());
                EconomyHistory.Restore(memento.EconomyHistory);
            }
            Phase = memento.Phase;
            ActiveEvent = memento.ActiveEvent;
            Result = memento.Result;
            _currentPlayerIndex = memento.CurrentPlayerId.HasValue
                ? _players.FindIndex(player => player.Id == memento.CurrentPlayerId.Value)
                : -1;
            return OperationResult.Success();
        }

        public OperationResult BeginEventChoice(EventSelectionResult selection, string spaceId)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));
            if (Phase != GamePhase.ResolveSpace)
            {
                return OperationResult.Failure(
                    "EVENT_CHOICE_NOT_READY",
                    "An event choice can only begin during ResolveSpace.");
            }

            if (CurrentPlayer == null || Board == null ||
                !_boardStates.TryGetValue(CurrentPlayer.Id, out var boardState))
            {
                return OperationResult.Failure(
                    "EVENT_CHOICE_BOARD_STATE_NOT_FOUND",
                    "The current player does not have a board state for event selection.");
            }

            var currentSpace = Board.GetSpace(boardState.Position);
            if (!string.Equals(currentSpace.Id, spaceId, StringComparison.Ordinal))
                return OperationResult.Failure("EVENT_CHOICE_SPACE_MISMATCH", "The event space is not the current board space.");
            if (string.IsNullOrWhiteSpace(currentSpace.EventTableId) ||
                !string.Equals(currentSpace.EventTableId, selection.TableId, StringComparison.Ordinal))
            {
                return OperationResult.Failure(
                    "EVENT_CHOICE_TABLE_MISMATCH",
                    "The selected event table does not match the current board space.");
            }

            ActiveEvent = new ActiveEventSelection(
                CurrentPlayer.Id,
                currentSpace.Id,
                selection.TableId,
                selection.PolicyId,
                selection.Definition.Id,
                selection.Definition.Version,
                selection.DrawIndex);

            var transition = TryTransition(GamePhase.AwaitChoice);
            if (!transition.Succeeded)
                ActiveEvent = null;
            return transition;
        }

        public OperationResult CompleteEventChoice(EventDefinition definition, string spaceId)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (Phase != GamePhase.AwaitChoice || ActiveEvent == null)
                return OperationResult.Failure("EVENT_CHOICE_NOT_ACTIVE", "There is no active event choice to complete.");
            if (!ActiveEvent.Matches(definition))
                return OperationResult.Failure("EVENT_CHOICE_DEFINITION_MISMATCH", "The confirmed event is not the active selection.");
            if (!string.Equals(ActiveEvent.SpaceId, spaceId, StringComparison.Ordinal))
                return OperationResult.Failure("EVENT_CHOICE_SPACE_MISMATCH", "The confirmed event space is not the active selection.");

            if (!_stateMachine.CanTransition(Phase, GamePhase.ApplyEffects))
                return OperationResult.Failure("INVALID_PHASE_TRANSITION", $"Transition from {Phase} to {GamePhase.ApplyEffects} is not allowed.");

            Phase = GamePhase.ApplyEffects;
            ActiveEvent = null;
            return OperationResult.Success();
        }

        public OperationResult CompleteProgressCheck(InnerBoardVictoryEvaluation evaluation)
        {
            if (evaluation == null)
                throw new ArgumentNullException(nameof(evaluation));
            if (Phase != GamePhase.CheckProgress)
                return OperationResult.Failure("PROGRESS_CHECK_NOT_ACTIVE", "Progress can only complete during CheckProgress.");

            if (!evaluation.GameEnded)
                return TryTransition(GamePhase.EndTurn);
            if (_players.All(player => player.Id != evaluation.WinnerId))
                return OperationResult.Failure("GAME_RESULT_WINNER_INVALID", "The winning player was not found in the session.");

            if (!_stateMachine.CanTransition(Phase, GamePhase.GameEnd))
                return OperationResult.Failure("INVALID_PHASE_TRANSITION", $"Transition from {Phase} to {GamePhase.GameEnd} is not allowed.");
            Result = new GameResult(evaluation.WinnerId, evaluation.ReasonCode);
            Phase = GamePhase.GameEnd;
            return OperationResult.Success();
        }

        public OperationResult ShowResults()
        {
            if (Phase != GamePhase.GameEnd || Result == null)
                return OperationResult.Failure("GAME_RESULTS_NOT_READY", "Results require a completed game result.");
            return TryTransition(GamePhase.Results);
        }

        public OperationResult BeginTurn()
        {
            if (Phase != GamePhase.StartTurn)
            {
                return OperationResult.Failure(
                    "TURN_NOT_READY_TO_BEGIN",
                    "A turn can only begin during StartTurn.");
            }

            return TryTransition(GamePhase.AwaitMove);
        }

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
            if (nextPhase == GamePhase.GameEnd)
            {
                return OperationResult.Failure(
                    "GAME_RESULT_REQUIRED",
                    "GameEnd requires a validated game result.");
            }
            if (nextPhase == GamePhase.Results && Result == null)
            {
                return OperationResult.Failure(
                    "GAME_RESULT_REQUIRED",
                    "Results require a validated game result.");
            }
            if (nextPhase == GamePhase.AwaitChoice && ActiveEvent == null)
            {
                return OperationResult.Failure(
                    "ACTIVE_EVENT_REQUIRED",
                    "AwaitChoice requires an active event selection.");
            }
            if (Phase == GamePhase.AwaitChoice)
            {
                return OperationResult.Failure(
                    "EVENT_CHOICE_COMPLETION_REQUIRED",
                    "An active event choice must be completed through the event flow.");
            }

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

