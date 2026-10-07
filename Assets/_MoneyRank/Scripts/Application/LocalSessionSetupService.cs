using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Characters;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Hospital;

namespace MoneyRank.Application
{
    public sealed class SessionSetupParticipant
    {
        public SessionSetupParticipant(
            PlayerId playerId,
            string displayName,
            int turnOrder,
            string characterId)
        {
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Setup player ID cannot be empty.", nameof(playerId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Setup display name cannot be empty.", nameof(displayName));
            if (turnOrder < 0)
                throw new ArgumentOutOfRangeException(nameof(turnOrder));
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("Setup character ID cannot be empty.", nameof(characterId));

            PlayerId = playerId;
            DisplayName = displayName;
            TurnOrder = turnOrder;
            CharacterId = characterId;
        }

        public PlayerId PlayerId { get; }
        public string DisplayName { get; }
        public int TurnOrder { get; }
        public string CharacterId { get; }
    }

    public sealed class LocalSessionSetupRequest
    {
        private readonly ReadOnlyCollection<SessionSetupParticipant> _participants;
        private readonly ReadOnlyCollection<AttributeDefinition> _attributes;
        private readonly ReadOnlyCollection<CharacterDefinition> _characters;

        public LocalSessionSetupRequest(
            SessionId sessionId,
            SessionRules rules,
            BoardDefinition board,
            IEnumerable<SessionSetupParticipant> participants,
            InnerBoardDefinition innerBoard = null,
            IEnumerable<AttributeDefinition> attributes = null,
            AnnualCycleDefinition annualCycle = null,
            HospitalDefinition hospital = null,
            EconomyDefinition economy = null,
            IEnumerable<CharacterDefinition> characters = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId.Value))
                throw new ArgumentException("Setup session ID cannot be empty.", nameof(sessionId));
            SessionId = sessionId;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            InnerBoard = innerBoard;
            AnnualCycle = annualCycle;
            Hospital = hospital;
            Economy = economy;
            _participants = Copy(participants, nameof(participants));
            _attributes = Copy(attributes ?? Array.Empty<AttributeDefinition>(), nameof(attributes));
            _characters = Copy(characters ?? Array.Empty<CharacterDefinition>(), nameof(characters));
        }

        public SessionId SessionId { get; }
        public SessionRules Rules { get; }
        public BoardDefinition Board { get; }
        public InnerBoardDefinition InnerBoard { get; }
        public AnnualCycleDefinition AnnualCycle { get; }
        public HospitalDefinition Hospital { get; }
        public EconomyDefinition Economy { get; }
        public IReadOnlyList<SessionSetupParticipant> Participants => _participants;
        public IReadOnlyList<AttributeDefinition> Attributes => _attributes;
        public IReadOnlyList<CharacterDefinition> Characters => _characters;

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            if (values == null) throw new ArgumentNullException(parameterName);
            var list = values.ToList();
            if (list.Any(value => value == null))
                throw new ArgumentException("Setup collections cannot contain null entries.", parameterName);
            return list.AsReadOnly();
        }
    }

    public sealed class LocalSessionSetupService
    {
        private readonly IGameSessionRepository _repository;
        private readonly GameSessionService _sessionService;

        public LocalSessionSetupService(
            IGameSessionRepository repository,
            SessionActionRecorder actionRecorder = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _sessionService = new GameSessionService(repository, actionRecorder);
        }

        public OperationResult<GameSession> CreateAndStart(LocalSessionSetupRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (_repository.Get(request.SessionId) != null)
                return Failure("SESSION_ALREADY_EXISTS", "A session with this ID already exists.");
            if (request.Participants.Count < request.Rules.MinimumPlayers ||
                request.Participants.Count > request.Rules.MaximumPlayers)
            {
                return Failure("SETUP_PLAYER_COUNT_INVALID", "The participant count is outside the configured session limits.");
            }
            if (request.Participants.Select(value => value.PlayerId).Distinct().Count() != request.Participants.Count)
                return Failure("SETUP_PLAYER_IDS_DUPLICATED", "Setup player IDs must be unique.");
            if (request.Participants.Select(value => value.TurnOrder).Distinct().Count() != request.Participants.Count)
                return Failure("SETUP_TURN_ORDER_DUPLICATED", "Setup turn order values must be unique.");
            var characterIds = new HashSet<string>(request.Characters.Select(value => value.Id), StringComparer.Ordinal);
            if (request.Participants.Any(value => !characterIds.Contains(value.CharacterId)))
                return Failure("SETUP_CHARACTER_NOT_FOUND", "Every participant must reference a configured character.");

            var session = _sessionService.Create(
                request.SessionId,
                request.Rules,
                request.Board,
                request.InnerBoard,
                request.Attributes,
                request.AnnualCycle,
                request.Hospital,
                request.Economy,
                request.Characters);
            foreach (var participant in request.Participants.OrderBy(value => value.TurnOrder))
            {
                RequireSuccess(
                    _sessionService.AddPlayer(
                        request.SessionId,
                        new Player(participant.PlayerId, participant.DisplayName, participant.TurnOrder)));
                RequireSuccess(
                    _sessionService.AssignCharacter(
                        request.SessionId,
                        participant.PlayerId,
                        participant.CharacterId));
            }
            RequireSuccess(_sessionService.Start(request.SessionId));
            RequireSuccess(_sessionService.BeginTurn(request.SessionId));
            return OperationResult<GameSession>.Success(session);
        }

        private static OperationResult<GameSession> Failure(string code, string message) =>
            OperationResult<GameSession>.Failure(code, message);

        private static void RequireSuccess(OperationResult result)
        {
            if (!result.Succeeded)
                throw new InvalidOperationException($"Validated session setup failed: {result.ErrorCode} - {result.Message}");
        }
    }
}
