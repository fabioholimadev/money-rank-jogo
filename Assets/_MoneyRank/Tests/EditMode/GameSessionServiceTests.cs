using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameSessionServiceTests
    {
        [Test]
        public void SuccessfulCommandsPersistTheSession()
        {
            var repository = new MemoryRepository();
            var service = new GameSessionService(repository);
            var sessionId = new SessionId("application-test");
            service.Create(sessionId, new SessionRules(1, 4));

            var result = service.AddPlayer(sessionId, new Player(new PlayerId("p1"), "Player", 0));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void FailedCommandsDoNotPersistASecondTime()
        {
            var repository = new MemoryRepository();
            var service = new GameSessionService(repository);
            var sessionId = new SessionId("application-test");
            service.Create(sessionId, new SessionRules(2, 4));

            var result = service.Start(sessionId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void SuccessfulMoveIsPersistedOnce()
        {
            var repository = new MemoryRepository();
            var service = new GameSessionService(repository);
            var sessionId = new SessionId("move-test");
            var board = new BoardDefinition(
                "test-board",
                new[]
                {
                    new BoardSpaceDefinition("space-0", 0, "start"),
                    new BoardSpaceDefinition("space-1", 1, "event")
                },
                0);
            var session = service.Create(sessionId, new SessionRules(1, 4), board);
            service.AddPlayer(sessionId, new Player(new PlayerId("p1"), "Player", 0));
            service.Start(sessionId);
            service.BeginTurn(sessionId);
            var savesBeforeMove = repository.SaveCount;

            var result = service.RegisterMove(sessionId, 1);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Destination, Is.EqualTo(1));
            Assert.That(repository.SaveCount, Is.EqualTo(savesBeforeMove + 1));
        }

        [Test]
        public void SuccessfulTurnLifecycleCommandsPersistOnceEach()
        {
            var repository = new MemoryRepository();
            var service = new GameSessionService(repository);
            var sessionId = new SessionId("turn-lifecycle-test");
            var session = service.Create(sessionId, new SessionRules(1, 4));
            service.AddPlayer(sessionId, new Player(new PlayerId("p1"), "Player", 0));
            service.Start(sessionId);

            var savesBeforeBegin = repository.SaveCount;
            var beginResult = service.BeginTurn(sessionId);
            session.TryTransition(GamePhase.ResolveSpace);
            session.TryTransition(GamePhase.ApplyEffects);
            session.TryTransition(GamePhase.CheckProgress);
            session.TryTransition(GamePhase.EndTurn);
            var savesBeforeComplete = repository.SaveCount;
            var completeResult = service.CompleteTurn(sessionId);

            Assert.That(beginResult.Succeeded, Is.True);
            Assert.That(completeResult.Succeeded, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(savesBeforeComplete + 1));
            Assert.That(savesBeforeComplete, Is.EqualTo(savesBeforeBegin + 1));
        }

        [Test]
        public void FailedTurnLifecycleCommandsDoNotPersist()
        {
            var repository = new MemoryRepository();
            var service = new GameSessionService(repository);
            var sessionId = new SessionId("invalid-turn-lifecycle-test");
            service.Create(sessionId, new SessionRules(1, 4));
            service.AddPlayer(sessionId, new Player(new PlayerId("p1"), "Player", 0));
            service.Start(sessionId);
            service.BeginTurn(sessionId);
            var savesBeforeFailures = repository.SaveCount;

            var beginResult = service.BeginTurn(sessionId);
            var completeResult = service.CompleteTurn(sessionId);

            Assert.That(beginResult.Succeeded, Is.False);
            Assert.That(completeResult.Succeeded, Is.False);
            Assert.That(repository.SaveCount, Is.EqualTo(savesBeforeFailures));
        }

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions = new Dictionary<SessionId, GameSession>();

            public int SaveCount { get; private set; }

            public GameSession Get(SessionId sessionId) =>
                _sessions.TryGetValue(sessionId, out var session) ? session : null;

            public void Save(GameSession session)
            {
                _sessions[session.Id] = session;
                SaveCount++;
            }
        }
    }
}
