using System;
using MoneyRank.Domain;
using MoneyRank.Domain.Characters;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using MoneyRank.Domain.Hospital;

namespace MoneyRank.Application
{
    public sealed class GameSessionService
    {
        private readonly IGameSessionRepository _repository;
        private readonly SessionActionRecorder _actionRecorder;

        public GameSessionService(
            IGameSessionRepository repository,
            SessionActionRecorder actionRecorder = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _actionRecorder = actionRecorder;
        }

        public GameSession Create(SessionId sessionId, SessionRules rules)
        {
            return Create(sessionId, rules, null);
        }

        public GameSession Create(SessionId sessionId, SessionRules rules, BoardDefinition board)
        {
            return Create(sessionId, rules, board, null, null);
        }

        public GameSession Create(
            SessionId sessionId,
            SessionRules rules,
            BoardDefinition board,
            InnerBoardDefinition innerBoard,
            System.Collections.Generic.IEnumerable<AttributeDefinition> attributeDefinitions,
            AnnualCycleDefinition annualCycle = null,
            HospitalDefinition hospital = null,
            EconomyDefinition economy = null,
            System.Collections.Generic.IEnumerable<CharacterDefinition> characterDefinitions = null)
        {
            var session = new GameSession(
                sessionId,
                rules,
                new GameFlowStateMachine(),
                board,
                innerBoard,
                attributeDefinitions,
                annualCycle,
                hospital,
                economy,
                characterDefinitions);
            _actionRecorder?.Record(
                session,
                "session.created",
                null,
                GamePhase.Setup,
                new[]
                {
                    new DefinitionParameter("minimumPlayers", rules.MinimumPlayers.ToString()),
                    new DefinitionParameter("maximumPlayers", rules.MaximumPlayers.ToString())
                });
            _repository.Save(session);
            return session;
        }

        public OperationResult AddPlayer(SessionId sessionId, Player player)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var result = session.AddPlayer(player);
            if (result.Succeeded)
            {
                _actionRecorder?.Record(
                    session,
                    "player.added",
                    player.Id,
                    phaseBefore,
                    new[] { new DefinitionParameter("turnOrder", player.TurnOrder.ToString()) });
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult<BoardMoveResult> RegisterMove(SessionId sessionId, int steps)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var playerId = session.CurrentPlayer?.Id;
            var result = session.RegisterMove(steps);
            if (result.Succeeded)
            {
                _actionRecorder?.Record(
                    session,
                    "board.moved",
                    playerId,
                    phaseBefore,
                    new[]
                    {
                        new DefinitionParameter("steps", result.Value.Steps.ToString()),
                        new DefinitionParameter("origin", result.Value.Origin.ToString()),
                        new DefinitionParameter("destination", result.Value.Destination.ToString()),
                        new DefinitionParameter("completedLaps", result.Value.CompletedLaps.ToString()),
                        new DefinitionParameter("spaceId", result.Value.DestinationSpace.Id)
                    });
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult BeginTurn(SessionId sessionId)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var result = session.BeginTurn();
            if (result.Succeeded)
            {
                _actionRecorder?.Record(session, "turn.started", session.CurrentPlayer?.Id, phaseBefore);
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult CompleteTurn(SessionId sessionId)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var completedPlayerId = session.CurrentPlayer?.Id;
            var result = session.CompleteTurn();
            if (result.Succeeded)
            {
                _actionRecorder?.Record(session, "turn.completed", completedPlayerId, phaseBefore);
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult ShowResults(SessionId sessionId)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var result = session.ShowResults();
            if (result.Succeeded)
            {
                _actionRecorder?.Record(
                    session,
                    "results.shown",
                    session.Result?.WinnerId,
                    phaseBefore,
                    new[] { new DefinitionParameter("reasonCode", session.Result?.ReasonCode ?? string.Empty) });
                _repository.Save(session);
            }
            return result;
        }

        public OperationResult BeginEventChoice(
            SessionId sessionId,
            EventSelectionResult selection,
            string spaceId)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var result = session.BeginEventChoice(selection, spaceId);
            if (result.Succeeded)
            {
                _actionRecorder?.Record(
                    session,
                    "event.selected",
                    session.CurrentPlayer?.Id,
                    phaseBefore,
                    new[]
                    {
                        new DefinitionParameter("eventId", selection.Definition.Id),
                        new DefinitionParameter("eventVersion", selection.Definition.Version.ToString()),
                        new DefinitionParameter("spaceId", spaceId),
                        new DefinitionParameter("tableId", selection.TableId),
                        new DefinitionParameter("policyId", selection.PolicyId)
                    });
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult Start(SessionId sessionId)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var result = session.Start();
            if (result.Succeeded)
            {
                _actionRecorder?.Record(session, "session.started", session.CurrentPlayer?.Id, phaseBefore);
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult AssignCharacter(SessionId sessionId, PlayerId playerId, string characterId)
        {
            var session = RequireSession(sessionId);
            var phaseBefore = session.Phase;
            var result = session.AssignCharacter(playerId, characterId);
            if (result.Succeeded)
            {
                _actionRecorder?.Record(
                    session,
                    "character.assigned",
                    playerId,
                    phaseBefore,
                    new[] { new DefinitionParameter("characterId", characterId) });
                _repository.Save(session);
            }
            return result;
        }

        private GameSession RequireSession(SessionId sessionId) =>
            _repository.Get(sessionId) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }
}

