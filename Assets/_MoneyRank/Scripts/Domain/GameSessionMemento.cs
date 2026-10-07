using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain
{
    public sealed class GameSessionMemento
    {
        private readonly ReadOnlyCollection<PlayerStateMemento> _players;
        private readonly ReadOnlyCollection<EventResolutionRecord> _eventHistory;
        private readonly ReadOnlyCollection<EconomyTransactionRecord> _economyHistory;
        private readonly ReadOnlyCollection<SessionActionRecord> _actionHistory;

        public GameSessionMemento(
            int schemaVersion,
            SessionId sessionId,
            GamePhase phase,
            PlayerId? currentPlayerId,
            IEnumerable<PlayerStateMemento> players,
            IEnumerable<EventResolutionRecord> eventHistory,
            EconomyStateMemento economy = null,
            IEnumerable<EconomyTransactionRecord> economyHistory = null,
            ActiveEventSelection activeEvent = null,
            GameResult result = null,
            IEnumerable<SessionActionRecord> actionHistory = null)
        {
            if (schemaVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (players == null)
                throw new ArgumentNullException(nameof(players));
            if (eventHistory == null)
                throw new ArgumentNullException(nameof(eventHistory));

            var playerList = players.ToList();
            var historyList = eventHistory.ToList();
            var economyHistoryList = (economyHistory ?? Array.Empty<EconomyTransactionRecord>()).ToList();
            var actionHistoryList = (actionHistory ?? Array.Empty<SessionActionRecord>()).ToList();
            if (playerList.Any(player => player == null))
                throw new ArgumentException("Session memento players cannot contain null entries.", nameof(players));
            if (playerList.Select(player => player.PlayerId).Distinct().Count() != playerList.Count)
                throw new ArgumentException("Session memento player IDs must be unique.", nameof(players));
            if (economyHistoryList.Any(record => record == null))
                throw new ArgumentException("Economy history cannot contain null records.", nameof(economyHistory));
            if (actionHistoryList.Any(record => record == null))
                throw new ArgumentException("Action history cannot contain null records.", nameof(actionHistory));

            SchemaVersion = schemaVersion;
            SessionId = sessionId;
            Phase = phase;
            CurrentPlayerId = currentPlayerId;
            _players = playerList.AsReadOnly();
            _eventHistory = historyList.AsReadOnly();
            Economy = economy;
            _economyHistory = economyHistoryList.AsReadOnly();
            ActiveEvent = activeEvent;
            Result = result;
            _actionHistory = actionHistoryList.AsReadOnly();
        }

        public int SchemaVersion { get; }
        public SessionId SessionId { get; }
        public GamePhase Phase { get; }
        public PlayerId? CurrentPlayerId { get; }
        public IReadOnlyList<PlayerStateMemento> Players => _players;
        public IReadOnlyList<EventResolutionRecord> EventHistory => _eventHistory;
        public EconomyStateMemento Economy { get; }
        public IReadOnlyList<EconomyTransactionRecord> EconomyHistory => _economyHistory;
        public ActiveEventSelection ActiveEvent { get; }
        public GameResult Result { get; }
        public IReadOnlyList<SessionActionRecord> ActionHistory => _actionHistory;
    }

    public sealed class PlayerStateMemento
    {
        private readonly ReadOnlyCollection<AttributeStateMemento> _attributes;

        public PlayerStateMemento(
            PlayerId playerId,
            ExternalBoardStateMemento externalBoard,
            EventStateSnapshot eventState,
            IEnumerable<AttributeStateMemento> attributes,
            InnerBoardStateMemento innerBoard,
            AnnualCycleStateMemento annualCycle = null,
            HospitalStateMemento hospital = null,
            string characterId = null)
        {
            if (attributes == null)
                throw new ArgumentNullException(nameof(attributes));

            var attributeList = attributes.ToList();
            if (attributeList.Any(attribute => attribute == null))
                throw new ArgumentException("Player attributes cannot contain null entries.", nameof(attributes));
            if (attributeList.Select(attribute => attribute.AttributeId).Distinct(StringComparer.Ordinal).Count() != attributeList.Count)
                throw new ArgumentException("Player attribute IDs must be unique.", nameof(attributes));

            PlayerId = playerId;
            ExternalBoard = externalBoard;
            EventState = eventState ?? throw new ArgumentNullException(nameof(eventState));
            InnerBoard = innerBoard;
            AnnualCycle = annualCycle;
            Hospital = hospital;
            CharacterId = characterId;
            _attributes = attributeList.AsReadOnly();
        }

        public PlayerId PlayerId { get; }
        public ExternalBoardStateMemento ExternalBoard { get; }
        public EventStateSnapshot EventState { get; }
        public IReadOnlyList<AttributeStateMemento> Attributes => _attributes;
        public InnerBoardStateMemento InnerBoard { get; }
        public AnnualCycleStateMemento AnnualCycle { get; }
        public HospitalStateMemento Hospital { get; }
        public string CharacterId { get; }
    }

    public sealed class ExternalBoardStateMemento
    {
        public ExternalBoardStateMemento(int position, int completedLaps)
        {
            Position = position;
            CompletedLaps = completedLaps;
        }

        public int Position { get; }
        public int CompletedLaps { get; }
    }

    public sealed class AttributeStateMemento
    {
        public AttributeStateMemento(string attributeId, int currentProgress, int completedCycles)
        {
            AttributeId = attributeId;
            CurrentProgress = currentProgress;
            CompletedCycles = completedCycles;
        }

        public string AttributeId { get; }
        public int CurrentProgress { get; }
        public int CompletedCycles { get; }
    }

    public sealed class InnerBoardStateMemento
    {
        public InnerBoardStateMemento(int position, int checkpointIndex)
        {
            Position = position;
            CheckpointIndex = checkpointIndex;
        }

        public int Position { get; }
        public int CheckpointIndex { get; }
    }

    public sealed class AnnualCycleStateMemento
    {
        public AnnualCycleStateMemento(int processedCycles)
        {
            ProcessedCycles = processedCycles;
        }

        public int ProcessedCycles { get; }
    }

    public sealed class HospitalStateMemento
    {
        public HospitalStateMemento(bool isHospitalized, int pendingMovementSkips, string reasonId)
        {
            IsHospitalized = isHospitalized;
            PendingMovementSkips = pendingMovementSkips;
            ReasonId = reasonId;
        }

        public bool IsHospitalized { get; }
        public int PendingMovementSkips { get; }
        public string ReasonId { get; }
    }

    public sealed class EconomyStateMemento
    {
        private readonly ReadOnlyCollection<EconomyBalanceDefinition> _balances;

        public EconomyStateMemento(IEnumerable<EconomyBalanceDefinition> balances)
        {
            if (balances == null)
                throw new ArgumentNullException(nameof(balances));

            var values = balances.ToList();
            if (values.Any(balance => balance == null))
                throw new ArgumentException("Economy balances cannot contain null entries.", nameof(balances));
            if (values.Select(balance => balance.AccountId).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Economy balance account IDs must be unique.", nameof(balances));
            _balances = values.AsReadOnly();
        }

        public IReadOnlyList<EconomyBalanceDefinition> Balances => _balances;

        public EconomySnapshot ToSnapshot() => new EconomySnapshot(_balances);
    }
}
