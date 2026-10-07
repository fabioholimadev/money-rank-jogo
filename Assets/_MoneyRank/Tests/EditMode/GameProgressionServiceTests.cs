using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameProgressionServiceTests
    {
        [Test]
        public void AttributeProgressionIsRestrictedToCheckProgressAndPersistsSuccess()
        {
            var fixture = CreateFixture();

            var tooEarly = fixture.Service.ApplyAttributeProgress(fixture.SessionId, "test-attribute", 1);
            AdvanceToCheckProgress(fixture.Session);
            var savesBefore = fixture.Repository.SaveCount;
            var success = fixture.Service.ApplyAttributeProgress(fixture.SessionId, "test-attribute", 4);

            Assert.That(tooEarly.Succeeded, Is.False);
            Assert.That(tooEarly.ErrorCode, Is.EqualTo("ATTRIBUTE_PROGRESS_NOT_ALLOWED"));
            Assert.That(success.Succeeded, Is.True);
            Assert.That(success.Value.CompletedCycles, Is.EqualTo(1));
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBefore + 1));
        }

        [Test]
        public void InnerBoardResolutionCommitsCaptureAndEndsGameAtomically()
        {
            var fixture = CreateFixture();
            AdvanceToCheckProgress(fixture.Session);
            fixture.Session.TryGetInnerBoardState(new PlayerId("p2"), out var opponent);
            opponent.Restore(2, 0);
            var savesBefore = fixture.Repository.SaveCount;

            var result = fixture.Service.ResolveInnerBoard(fixture.SessionId, 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Move.ReachedGoal, Is.True);
            Assert.That(result.Value.Capture.Relocations, Has.Count.EqualTo(1));
            Assert.That(result.Value.Victory.GameEnded, Is.True);
            Assert.That(fixture.Session.Phase, Is.EqualTo(GamePhase.GameEnd));
            Assert.That(fixture.Session.Result, Is.Not.Null);
            Assert.That(fixture.Session.Result.WinnerId, Is.EqualTo(new PlayerId("p1")));
            Assert.That(fixture.Session.Result.ReasonCode, Is.EqualTo("TEST_GOAL"));
            fixture.Session.TryGetInnerBoardState(new PlayerId("p1"), out var current);
            Assert.That(current.Position, Is.EqualTo(2));
            Assert.That(opponent.Position, Is.Zero);
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBefore + 1));
        }

        [Test]
        public void FailedCaptureRollsBackMovementAndDoesNotPersist()
        {
            var fixture = CreateFixture(invalidCapture: true);
            AdvanceToCheckProgress(fixture.Session);
            var savesBefore = fixture.Repository.SaveCount;

            var result = fixture.Service.ResolveInnerBoard(fixture.SessionId, 1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INNER_BOARD_CAPTURE_DESTINATION_INVALID"));
            fixture.Session.TryGetInnerBoardState(new PlayerId("p1"), out var current);
            Assert.That(current.Position, Is.Zero);
            Assert.That(fixture.Session.Phase, Is.EqualTo(GamePhase.CheckProgress));
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBefore));
        }

        private static Fixture CreateFixture(bool invalidCapture = false)
        {
            var repository = new MemoryRepository();
            var sessionId = new SessionId("session");
            var attribute = new AttributeDefinition(
                "test-attribute",
                "Test attribute",
                "test-type",
                3,
                "carry",
                Array.Empty<GameEffectDefinition>());
            var innerBoard = new InnerBoardDefinition(
                "inner-board",
                new[]
                {
                    new InnerBoardSpaceDefinition("start", 0, "start"),
                    new InnerBoardSpaceDefinition("progress", 1, "progress"),
                    new InnerBoardSpaceDefinition("goal", 2, "goal", isGoal: true)
                },
                0,
                "move",
                "capture",
                "victory");
            var session = new GameSession(
                sessionId,
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                null,
                innerBoard,
                new[] { attribute });
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            session.Start();
            repository.Save(session);

            var service = new GameProgressionService(
                repository,
                new AttributeProgressionService(
                    new IAttributeOverflowHandler[] { new CarryOverflowPolicy() }),
                new InnerBoardProgressionService(
                    new IInnerBoardMovementPolicy[] { new MovePolicy() }),
                new InnerBoardCaptureService(
                    new IInnerBoardCapturePolicy[]
                    {
                        invalidCapture ? new InvalidCapturePolicy() : new CapturePolicy()
                    }),
                new InnerBoardVictoryService(
                    new IInnerBoardVictoryPolicy[] { new VictoryPolicy() }));
            return new Fixture(repository, service, sessionId, session);
        }

        private static void AdvanceToCheckProgress(GameSession session)
        {
            session.BeginTurn();
            session.TryTransition(GamePhase.ResolveSpace);
            session.TryTransition(GamePhase.ApplyEffects);
            session.TryTransition(GamePhase.CheckProgress);
        }

        private sealed class CarryOverflowPolicy : IAttributeOverflowHandler
        {
            public string BehaviorId => "carry";

            public OperationResult<AttributeProgressionCalculation> Calculate(
                int currentProgress,
                int delta,
                int threshold)
            {
                var total = currentProgress + delta;
                return OperationResult<AttributeProgressionCalculation>.Success(
                    new AttributeProgressionCalculation(total % threshold, total / threshold));
            }
        }

        private sealed class MovePolicy : IInnerBoardMovementPolicy
        {
            public string PolicyId => "move";

            public OperationResult<InnerBoardMovementCalculation> Calculate(
                InnerBoardDefinition definition,
                InnerBoardState state,
                int steps) =>
                OperationResult<InnerBoardMovementCalculation>.Success(
                    new InnerBoardMovementCalculation(
                        Math.Min(definition.SpaceCount - 1, state.Position + steps),
                        state.CheckpointIndex));
        }

        private sealed class CapturePolicy : IInnerBoardCapturePolicy
        {
            public string PolicyId => "capture";

            public OperationResult<InnerBoardCaptureCalculation> Calculate(
                InnerBoardDefinition definition,
                PlayerId movedPlayerId,
                IReadOnlyList<InnerBoardState> states)
            {
                var mover = states.Single(state => state.PlayerId == movedPlayerId);
                var relocations = states
                    .Where(state => state.PlayerId != movedPlayerId && state.Position == mover.Position)
                    .Select(state => new InnerBoardRelocation(state.PlayerId, 0, 0));
                return OperationResult<InnerBoardCaptureCalculation>.Success(
                    new InnerBoardCaptureCalculation(relocations));
            }
        }

        private sealed class InvalidCapturePolicy : IInnerBoardCapturePolicy
        {
            public string PolicyId => "capture";

            public OperationResult<InnerBoardCaptureCalculation> Calculate(
                InnerBoardDefinition definition,
                PlayerId movedPlayerId,
                IReadOnlyList<InnerBoardState> states) =>
                OperationResult<InnerBoardCaptureCalculation>.Success(
                    new InnerBoardCaptureCalculation(
                        new[] { new InnerBoardRelocation(states[1].PlayerId, 99, 0) }));
        }

        private sealed class VictoryPolicy : IInnerBoardVictoryPolicy
        {
            public string PolicyId => "victory";

            public InnerBoardVictoryEvaluation Evaluate(
                InnerBoardDefinition definition,
                PlayerId movedPlayerId,
                IReadOnlyList<InnerBoardState> states)
            {
                var mover = states.Single(state => state.PlayerId == movedPlayerId);
                return definition.GetSpace(mover.Position).IsGoal
                    ? InnerBoardVictoryEvaluation.End(movedPlayerId, "TEST_GOAL")
                    : InnerBoardVictoryEvaluation.Continue();
            }
        }

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions =
                new Dictionary<SessionId, GameSession>();

            public int SaveCount { get; private set; }
            public GameSession Get(SessionId sessionId) => _sessions[sessionId];

            public void Save(GameSession session)
            {
                _sessions[session.Id] = session;
                SaveCount++;
            }
        }

        private sealed class Fixture
        {
            public Fixture(
                MemoryRepository repository,
                GameProgressionService service,
                SessionId sessionId,
                GameSession session)
            {
                Repository = repository;
                Service = service;
                SessionId = sessionId;
                Session = session;
            }

            public MemoryRepository Repository { get; }
            public GameProgressionService Service { get; }
            public SessionId SessionId { get; }
            public GameSession Session { get; }
        }
    }
}
