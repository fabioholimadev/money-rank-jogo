using System;
using System.Globalization;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Infrastructure.Persistence
{
    public sealed class JsonGameSessionMementoSerializer
    {
        public string Serialize(GameSessionMemento memento, bool prettyPrint = true)
        {
            if (memento == null)
                throw new ArgumentNullException(nameof(memento));

            return JsonUtility.ToJson(SessionDto.FromDomain(memento), prettyPrint);
        }

        public GameSessionMemento Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Session JSON cannot be empty.", nameof(json));

            var dto = JsonUtility.FromJson<SessionDto>(json);
            if (dto == null)
                throw new FormatException("Session JSON did not contain an object.");
            return dto.ToDomain();
        }

        [Serializable]
        private sealed class SessionDto
        {
            public int schemaVersion;
            public string sessionId;
            public int phase;
            public bool hasCurrentPlayer;
            public string currentPlayerId;
            public PlayerDto[] players;
            public EventRecordDto[] eventHistory;
            public SessionActionDto[] actionHistory;
            public bool hasActiveEvent;
            public ActiveEventDto activeEvent;
            public bool hasResult;
            public GameResultDto result;
            public bool hasEconomy;
            public EconomyDto economy;
            public EconomyRecordDto[] economyHistory;

            public static SessionDto FromDomain(GameSessionMemento source) =>
                new SessionDto
                {
                    schemaVersion = source.SchemaVersion,
                    sessionId = source.SessionId.Value,
                    phase = (int)source.Phase,
                    hasCurrentPlayer = source.CurrentPlayerId.HasValue,
                    currentPlayerId = source.CurrentPlayerId?.Value,
                    players = source.Players.Select(PlayerDto.FromDomain).ToArray(),
                    eventHistory = source.EventHistory.Select(EventRecordDto.FromDomain).ToArray(),
                    actionHistory = source.ActionHistory.Select(SessionActionDto.FromDomain).ToArray(),
                    hasActiveEvent = source.ActiveEvent != null,
                    activeEvent = ActiveEventDto.FromDomain(source.ActiveEvent),
                    hasResult = source.Result != null,
                    result = GameResultDto.FromDomain(source.Result),
                    hasEconomy = source.Economy != null,
                    economy = EconomyDto.FromDomain(source.Economy),
                    economyHistory = source.EconomyHistory.Select(EconomyRecordDto.FromDomain).ToArray()
                };

            public GameSessionMemento ToDomain() =>
                new GameSessionMemento(
                    schemaVersion,
                    new SessionId(sessionId),
                    (GamePhase)phase,
                    hasCurrentPlayer ? new PlayerId(currentPlayerId) : (PlayerId?)null,
                    (players ?? Array.Empty<PlayerDto>()).Select(player => player.ToDomain()),
                    (eventHistory ?? Array.Empty<EventRecordDto>()).Select(record => record.ToDomain()),
                    hasEconomy ? Require(economy, "Economy").ToDomain() : null,
                    (economyHistory ?? Array.Empty<EconomyRecordDto>()).Select(record => record.ToDomain()),
                    hasActiveEvent ? Require(activeEvent, "Active event").ToDomain() : null,
                    hasResult ? Require(result, "Game result").ToDomain() : null,
                    (actionHistory ?? Array.Empty<SessionActionDto>()).Select(record => record.ToDomain()));
        }

        [Serializable]
        private sealed class SessionActionDto
        {
            public string id;
            public string typeId;
            public bool hasPlayer;
            public string playerId;
            public int phaseBefore;
            public int phaseAfter;
            public string createdAt;
            public string operatorId;
            public string sourceId;
            public KeyValueDto[] parameters;

            public static SessionActionDto FromDomain(SessionActionRecord source) =>
                new SessionActionDto
                {
                    id = source.Id,
                    typeId = source.TypeId,
                    hasPlayer = source.PlayerId.HasValue,
                    playerId = source.PlayerId?.Value,
                    phaseBefore = (int)source.PhaseBefore,
                    phaseAfter = (int)source.PhaseAfter,
                    createdAt = source.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                    operatorId = source.OperatorId,
                    sourceId = source.SourceId,
                    parameters = source.Parameters
                        .Select(parameter => new KeyValueDto { key = parameter.Key, value = parameter.Value })
                        .ToArray()
                };

            public SessionActionRecord ToDomain() =>
                new SessionActionRecord(
                    id,
                    typeId,
                    hasPlayer ? new PlayerId(playerId) : (PlayerId?)null,
                    (GamePhase)phaseBefore,
                    (GamePhase)phaseAfter,
                    DateTimeOffset.Parse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    operatorId,
                    sourceId,
                    (parameters ?? Array.Empty<KeyValueDto>())
                    .Select(parameter => new DefinitionParameter(parameter.key, parameter.value)));
        }

        [Serializable]
        private sealed class GameResultDto
        {
            public string winnerId;
            public string reasonCode;

            public static GameResultDto FromDomain(GameResult source) =>
                source == null ? null : new GameResultDto
                {
                    winnerId = source.WinnerId.Value,
                    reasonCode = source.ReasonCode
                };

            public GameResult ToDomain() => new GameResult(new PlayerId(winnerId), reasonCode);
        }

        [Serializable]
        private sealed class ActiveEventDto
        {
            public string playerId;
            public string spaceId;
            public string tableId;
            public string policyId;
            public string eventId;
            public int eventVersion;
            public int drawIndex;

            public static ActiveEventDto FromDomain(ActiveEventSelection source) =>
                source == null ? null : new ActiveEventDto
                {
                    playerId = source.PlayerId.Value,
                    spaceId = source.SpaceId,
                    tableId = source.TableId,
                    policyId = source.PolicyId,
                    eventId = source.EventId,
                    eventVersion = source.EventVersion,
                    drawIndex = source.DrawIndex
                };

            public ActiveEventSelection ToDomain() =>
                new ActiveEventSelection(
                    new PlayerId(playerId),
                    spaceId,
                    tableId,
                    policyId,
                    eventId,
                    eventVersion,
                    drawIndex);
        }

        [Serializable]
        private sealed class PlayerDto
        {
            public string playerId;
            public bool hasExternalBoard;
            public ExternalBoardDto externalBoard;
            public KeyValueDto[] eventState;
            public AttributeDto[] attributes;
            public bool hasInnerBoard;
            public InnerBoardDto innerBoard;
            public bool hasAnnualCycle;
            public AnnualCycleDto annualCycle;
            public bool hasHospital;
            public HospitalDto hospital;
            public string characterId;

            public static PlayerDto FromDomain(PlayerStateMemento source) =>
                new PlayerDto
                {
                    playerId = source.PlayerId.Value,
                    hasExternalBoard = source.ExternalBoard != null,
                    externalBoard = ExternalBoardDto.FromDomain(source.ExternalBoard),
                    eventState = source.EventState.Values
                        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => new KeyValueDto { key = pair.Key, value = pair.Value })
                        .ToArray(),
                    attributes = source.Attributes.Select(AttributeDto.FromDomain).ToArray(),
                    hasInnerBoard = source.InnerBoard != null,
                    innerBoard = InnerBoardDto.FromDomain(source.InnerBoard),
                    hasAnnualCycle = source.AnnualCycle != null,
                    annualCycle = AnnualCycleDto.FromDomain(source.AnnualCycle),
                    hasHospital = source.Hospital != null,
                    hospital = HospitalDto.FromDomain(source.Hospital),
                    characterId = source.CharacterId
                };

            public PlayerStateMemento ToDomain() =>
                new PlayerStateMemento(
                    new PlayerId(playerId),
                    hasExternalBoard
                        ? Require(externalBoard, "External board").ToDomain()
                        : null,
                    new EventStateSnapshot(
                        (eventState ?? Array.Empty<KeyValueDto>())
                        .Select(value => new DefinitionParameter(value.key, value.value))),
                    (attributes ?? Array.Empty<AttributeDto>()).Select(attribute => attribute.ToDomain()),
                    hasInnerBoard ? Require(innerBoard, "Inner board").ToDomain() : null,
                    hasAnnualCycle ? Require(annualCycle, "Annual cycle").ToDomain() : null,
                    hasHospital ? Require(hospital, "Hospital").ToDomain() : null,
                    characterId);
        }

        [Serializable]
        private sealed class ExternalBoardDto
        {
            public int position;
            public int completedLaps;

            public static ExternalBoardDto FromDomain(ExternalBoardStateMemento source) =>
                source == null ? null : new ExternalBoardDto
                {
                    position = source.Position,
                    completedLaps = source.CompletedLaps
                };

            public ExternalBoardStateMemento ToDomain() =>
                new ExternalBoardStateMemento(position, completedLaps);
        }

        [Serializable]
        private sealed class AttributeDto
        {
            public string attributeId;
            public int currentProgress;
            public int completedCycles;

            public static AttributeDto FromDomain(AttributeStateMemento source) =>
                new AttributeDto
                {
                    attributeId = source.AttributeId,
                    currentProgress = source.CurrentProgress,
                    completedCycles = source.CompletedCycles
                };

            public AttributeStateMemento ToDomain() =>
                new AttributeStateMemento(attributeId, currentProgress, completedCycles);
        }

        [Serializable]
        private sealed class InnerBoardDto
        {
            public int position;
            public int checkpointIndex;

            public static InnerBoardDto FromDomain(InnerBoardStateMemento source) =>
                source == null ? null : new InnerBoardDto
                {
                    position = source.Position,
                    checkpointIndex = source.CheckpointIndex
                };

            public InnerBoardStateMemento ToDomain() =>
                new InnerBoardStateMemento(position, checkpointIndex);
        }

        [Serializable]
        private sealed class AnnualCycleDto
        {
            public int processedCycles;

            public static AnnualCycleDto FromDomain(AnnualCycleStateMemento source) =>
                source == null ? null : new AnnualCycleDto { processedCycles = source.ProcessedCycles };

            public AnnualCycleStateMemento ToDomain() =>
                new AnnualCycleStateMemento(processedCycles);
        }

        [Serializable]
        private sealed class HospitalDto
        {
            public bool isHospitalized;
            public int pendingMovementSkips;
            public string reasonId;

            public static HospitalDto FromDomain(HospitalStateMemento source) =>
                source == null ? null : new HospitalDto
                {
                    isHospitalized = source.IsHospitalized,
                    pendingMovementSkips = source.PendingMovementSkips,
                    reasonId = source.ReasonId
                };

            public HospitalStateMemento ToDomain() =>
                new HospitalStateMemento(isHospitalized, pendingMovementSkips, reasonId);
        }

        [Serializable]
        private sealed class EventRecordDto
        {
            public string id;
            public string eventId;
            public int eventVersion;
            public string playerId;
            public string spaceId;
            public string choiceId;
            public KeyValueDto[] beforeState;
            public KeyValueDto[] afterState;
            public string sourceId;
            public string createdAt;
            public string operatorId;

            public static EventRecordDto FromDomain(EventResolutionRecord source) =>
                new EventRecordDto
                {
                    id = source.Id,
                    eventId = source.EventId,
                    eventVersion = source.EventVersion,
                    playerId = source.PlayerId.Value,
                    spaceId = source.SpaceId,
                    choiceId = source.ChoiceId,
                    beforeState = ToValues(source.BeforeState),
                    afterState = ToValues(source.AfterState),
                    sourceId = source.SourceId,
                    createdAt = source.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                    operatorId = source.OperatorId
                };

            public EventResolutionRecord ToDomain() =>
                new EventResolutionRecord(
                    id,
                    eventId,
                    eventVersion,
                    new PlayerId(playerId),
                    spaceId,
                    choiceId,
                    ToSnapshot(beforeState),
                    ToSnapshot(afterState),
                    sourceId,
                    DateTimeOffset.Parse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    operatorId);

            private static KeyValueDto[] ToValues(EventStateSnapshot snapshot) =>
                snapshot.Values
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new KeyValueDto { key = pair.Key, value = pair.Value })
                    .ToArray();

            private static EventStateSnapshot ToSnapshot(KeyValueDto[] values) =>
                new EventStateSnapshot(
                    (values ?? Array.Empty<KeyValueDto>())
                    .Select(value => new DefinitionParameter(value.key, value.value)));
        }

        [Serializable]
        private sealed class EconomyDto
        {
            public EconomyBalanceDto[] balances;

            public static EconomyDto FromDomain(EconomyStateMemento source) =>
                source == null ? null : new EconomyDto
                {
                    balances = source.Balances.Select(EconomyBalanceDto.FromDomain).ToArray()
                };

            public EconomyStateMemento ToDomain() =>
                new EconomyStateMemento(
                    (balances ?? Array.Empty<EconomyBalanceDto>()).Select(balance => balance.ToDomain()));
        }

        [Serializable]
        private sealed class EconomyBalanceDto
        {
            public string accountId;
            public string balance;

            public static EconomyBalanceDto FromDomain(EconomyBalanceDefinition source) =>
                new EconomyBalanceDto
                {
                    accountId = source.AccountId,
                    balance = source.Balance.ToString(CultureInfo.InvariantCulture)
                };

            public EconomyBalanceDefinition ToDomain() =>
                new EconomyBalanceDefinition(
                    accountId,
                    decimal.Parse(balance, NumberStyles.Number, CultureInfo.InvariantCulture));
        }

        [Serializable]
        private sealed class EconomyEntryDto
        {
            public string accountId;
            public string amount;

            public static EconomyEntryDto FromDomain(EconomyTransactionEntry source) =>
                new EconomyEntryDto
                {
                    accountId = source.AccountId,
                    amount = source.Amount.ToString(CultureInfo.InvariantCulture)
                };

            public EconomyTransactionEntry ToDomain() =>
                new EconomyTransactionEntry(
                    accountId,
                    decimal.Parse(amount, NumberStyles.Number, CultureInfo.InvariantCulture));
        }

        [Serializable]
        private sealed class EconomyRecordDto
        {
            public string id;
            public string sourceId;
            public string createdAt;
            public string operatorId;
            public string description;
            public EconomyEntryDto[] entries;
            public EconomyBalanceDto[] beforeState;
            public EconomyBalanceDto[] afterState;

            public static EconomyRecordDto FromDomain(EconomyTransactionRecord source) =>
                new EconomyRecordDto
                {
                    id = source.Id,
                    sourceId = source.SourceId,
                    createdAt = source.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                    operatorId = source.OperatorId,
                    description = source.Description,
                    entries = source.Entries.Select(EconomyEntryDto.FromDomain).ToArray(),
                    beforeState = ToBalances(source.BeforeState),
                    afterState = ToBalances(source.AfterState)
                };

            public EconomyTransactionRecord ToDomain()
            {
                var request = new EconomyTransactionRequest(
                    id,
                    (entries ?? Array.Empty<EconomyEntryDto>()).Select(entry => entry.ToDomain()),
                    sourceId,
                    DateTimeOffset.Parse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    operatorId,
                    description);
                return new EconomyTransactionRecord(
                    request,
                    ToSnapshot(beforeState),
                    ToSnapshot(afterState));
            }

            private static EconomyBalanceDto[] ToBalances(EconomySnapshot snapshot) =>
                snapshot.Balances
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => EconomyBalanceDto.FromDomain(
                        new EconomyBalanceDefinition(pair.Key, pair.Value)))
                    .ToArray();

            private static EconomySnapshot ToSnapshot(EconomyBalanceDto[] balances) =>
                new EconomySnapshot(
                    (balances ?? Array.Empty<EconomyBalanceDto>()).Select(balance => balance.ToDomain()));
        }

        [Serializable]
        private sealed class KeyValueDto
        {
            public string key;
            public string value;
        }

        private static T Require<T>(T value, string label) where T : class =>
            value ?? throw new FormatException($"{label} data is missing from the session JSON.");
    }
}
