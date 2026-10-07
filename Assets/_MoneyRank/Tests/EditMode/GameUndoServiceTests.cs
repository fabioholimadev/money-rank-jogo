using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameUndoServiceTests
    {
        [Test]
        public void UndoRestoresStateButPreservesEventAuditHistory()
        {
            var repository = new MemoryRepository();
            var session = CreateSession();
            repository.Save(session);
            var journal = new SessionUndoJournal();
            var service = new GameUndoService(repository, journal);
            var now = new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero);
            service.CreateCheckpoint(session.Id, "before-move", "Before move", now, "facilitator");

            session.Start();
            session.BeginTurn();
            session.RegisterMove(2);
            session.TryGetEventState(new PlayerId("p1"), out var eventState);
            eventState.SetValue("test", "after");
            session.EventHistory.Append(CreateHistoryRecord());
            var savesBefore = repository.SaveCount;

            var result = service.Undo(
                session.Id,
                "before-move",
                "undo-1",
                now.AddMinutes(1),
                "facilitator");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Setup));
            Assert.That(session.CurrentPlayer, Is.Null);
            session.TryGetBoardState(new PlayerId("p1"), out var boardState);
            Assert.That(boardState.Position, Is.Zero);
            session.TryGetEventState(new PlayerId("p1"), out var restoredEventState);
            Assert.That(restoredEventState.TryGetValue("test", out _), Is.False);
            Assert.That(session.EventHistory.Records, Has.Count.EqualTo(1));
            Assert.That(journal.Records, Has.Count.EqualTo(1));
            Assert.That(result.Value.BeforeState.Phase, Is.EqualTo(GamePhase.ResolveSpace));
            Assert.That(result.Value.AfterState.Phase, Is.EqualTo(GamePhase.Setup));
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore + 1));
        }

        [Test]
        public void MissingCheckpointDoesNotMutateOrPersist()
        {
            var repository = new MemoryRepository();
            var session = CreateSession();
            repository.Save(session);
            var service = new GameUndoService(repository, new SessionUndoJournal());
            var savesBefore = repository.SaveCount;

            var result = service.Undo(
                session.Id,
                "missing",
                "undo-1",
                DateTimeOffset.UtcNow,
                "facilitator");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("UNDO_CHECKPOINT_NOT_FOUND"));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Setup));
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore));
        }

        private static GameSession CreateSession()
        {
            var board = new BoardDefinition(
                "board",
                new[]
                {
                    new BoardSpaceDefinition("start", 0, "start"),
                    new BoardSpaceDefinition("one", 1, "event"),
                    new BoardSpaceDefinition("two", 2, "event")
                },
                0);
            var session = new GameSession(
                new SessionId("session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                board);
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            return session;
        }

        private static EventResolutionRecord CreateHistoryRecord()
        {
            var before = new EventStateSnapshot(Array.Empty<DefinitionParameter>());
            var after = new EventStateSnapshot(new[] { new DefinitionParameter("test", "after") });
            return new EventResolutionRecord(
                "resolution-1",
                "test-event",
                1,
                new PlayerId("p1"),
                "two",
                "accept",
                before,
                after,
                "local-master",
                new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero),
                "facilitator");
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
    }
}
